#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Globalization;
using System.Threading.Tasks;
using UnityEngine;
using PM.horizOn.Cloud.Core;
using PM.horizOn.Cloud.Enums;
using PM.horizOn.Cloud.Manager;
using PM.horizOn.Cloud.Service;
using PM.horizOn.Cloud.Objects.Network.Responses;

namespace SeagullStorm
{
    /// <summary>Opt-in native Develop QA. Private files, fixed actions, no automatic command replay.</summary>
    public sealed class SeagullDevelopAcceptance : MonoBehaviour
    {
        private const string Flag = "-seagull-develop-acceptance=";
        public const string DevelopHost = "http://127.0.0.1:3170";
        public static bool Enabled => Environment.GetCommandLineArgs().Any(x => x.StartsWith(Flag, StringComparison.Ordinal));
        [Serializable] public sealed class Manifest
        {
            public string stage, runId, baseUrl, projectId, appApiKey, serverCommit, dashboardCommit, controlDirectory;
            public bool rootApproved, allowWrites;
        }
        [Serializable] public sealed class Command
        {
            public string id, action, feature, operation, argumentsJson = "{}", dataBase64 = "", expectedRevision, displayName, channelId, listingId;
            public bool confirm;
        }
        [Serializable] public sealed class Result
        {
            public string id, action, startedAtUtc, completedAtUtc, revision, userId, dataBase64, output, error;
            // All result files are private. Never copy this field to public evidence.
            public string privateAnonymousToken;
            public bool success, conflict, found, invoked;
            public long statusCode, lastStatusCode;
            public int observedResponses;
            public double secondsToHttpResponse;
        }
        private Manifest _manifest;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (!Enabled) return;
            var go = new GameObject("Explicit Develop acceptance controller"); DontDestroyOnLoad(go); go.AddComponent<SeagullDevelopAcceptance>();
        }
        public static void Validate(Manifest manifest)
        {
            if (manifest == null || manifest.stage != "develop" || !manifest.rootApproved || manifest.baseUrl != DevelopHost
                || !Guid.TryParseExact(manifest.projectId, "D", out _) || !Guid.TryParseExact(manifest.runId, "D", out _)
                || string.IsNullOrWhiteSpace(manifest.appApiKey) || string.IsNullOrWhiteSpace(manifest.controlDirectory)
                || !Path.IsPathRooted(manifest.controlDirectory) || !IsCommit(manifest.serverCommit) || !IsCommit(manifest.dashboardCommit))
                throw new InvalidOperationException("Approved exact Develop manifest required before configuring any SDK service");
        }
        private static bool IsCommit(string value) => value != null && value.Length == 40 && value.All(c => c >= '0' && c <= '9' || c >= 'a' && c <= 'f');
        private async void Start()
        {
            try
            {
                if (!Application.productName.StartsWith("Seagull QA ", StringComparison.Ordinal)) throw new InvalidOperationException("Isolated QA product required");
                string path = Environment.GetCommandLineArgs().Single(x => x.StartsWith(Flag, StringComparison.Ordinal)).Substring(Flag.Length);
                _manifest = JsonUtility.FromJson<Manifest>(File.ReadAllText(path, new UTF8Encoding(false, true))); Validate(_manifest);
                if (!Directory.Exists(_manifest.controlDirectory)) throw new IOException("Existing private control directory required");
                Directory.CreateDirectory(Path.Combine(_manifest.controlDirectory, "results"));
                while (HorizonManager.Instance == null) await Task.Yield();
                Application.runInBackground = true;
                var config = ScriptableObject.CreateInstance<HorizonConfig>(); config.SetApiKey(_manifest.appApiKey); config.SetHosts(new[] { DevelopHost });
                LogService.Instance.Initialize(config); LogService.Instance.EnableUnityLogging = false;
                NetworkService.Instance.Initialize(config); NetworkService.Instance.SetActiveHost(DevelopHost); HorizonManager.Instance.Configured = true;
                // Configuration itself does not start a business action or create a player.
                Debug.Log("[DEVELOP ACCEPTANCE] READY: private commands only; no action was inferred");
                while (true)
                {
                    foreach (string input in Directory.GetFiles(_manifest.controlDirectory, "*.json").OrderBy(x => x, StringComparer.Ordinal))
                    {
                        string id = Path.GetFileNameWithoutExtension(input);
                        if (!Guid.TryParseExact(id, "D", out var parsed) || id != parsed.ToString("D")) continue;
                        string destination = Path.Combine(_manifest.controlDirectory, "results", id + ".json");
                        if (File.Exists(destination)) continue;
                        var command = JsonUtility.FromJson<Command>(File.ReadAllText(input, new UTF8Encoding(false, true)));
                        if (command == null || command.id != id) throw new IOException("Command filename/identity mismatch");
                        var result = new Result { id = id, action = command.action, startedAtUtc = DateTimeOffset.UtcNow.ToString("O") };
                        string marker = input + ".started";
                        if (File.Exists(marker)) result.error = "UNKNOWN_OUTCOME_ORIGINAL_RETAINED";
                        else
                        {
                            DurableNew(marker, Encoding.UTF8.GetBytes(_manifest.runId));
                            try { await Execute(command, result); }
                            catch (Exception error) { result.error = error.GetType().Name + ": original and command marker retained"; }
                        }
                        result.completedAtUtc = DateTimeOffset.UtcNow.ToString("O");
                        DurableNew(destination + ".next", new UTF8Encoding(false, true).GetBytes(JsonUtility.ToJson(result, true)));
                        File.Move(destination + ".next", destination);
                        if (command.action == "quit") { Application.Quit(0); return; }
                    }
                    await Task.Delay(150); // Local control files only. Never polls an API.
                }
            }
            catch (Exception error) { Debug.LogError("[DEVELOP ACCEPTANCE] STOP: " + error.GetType().Name + "; private files/originals preserved"); Application.Quit(1); }
        }
        private static void DurableNew(string path, byte[] bytes)
        {
            using var file = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None); file.Write(bytes, 0, bytes.Length); file.Flush(true);
        }
        private async Task Execute(Command command, Result result)
        {
            var h = HorizonManager.Instance;
            bool read = command.action == "loadJson" || command.action == "loadBinary" || command.action == "guide" || command.action == "capture" || command.action == "quit" || command.action == "restoreSaved";
            bool guideWrites = command.action == "guide" && (command.feature == "statistics" || command.feature == "achievements");
            if ((!read || guideWrites) && (!_manifest.allowWrites || !command.confirm)) { result.error = "EXPLICIT_DEVELOP_CONFIRMATION_REQUIRED"; return; }
            if (command.action == "createGuest")
            {
                if (UserManager.Instance.IsSignedIn || UserManager.Instance.HasCachedAnonymousToken()) { result.error = "EXISTING_IDENTITY_REQUIRES_RESTORE"; return; }
                result.success = await h.SignUpGuest(command.displayName); result.userId = h.GetUserId();
                if (result.success) result.privateAnonymousToken = UserManager.Instance.CurrentUser.AnonymousToken;
                return;
            }
            if (command.action == "restoreSaved") { result.success = await h.RestoreSession(); result.userId = h.GetUserId(); return; }
            if (command.action == "quit") { result.success = true; return; }
            if (!h.Online) { result.error = "SESSION_REQUIRED"; return; }
            result.userId = h.GetUserId();
            switch (command.action)
            {
                case "feature":
                    SeagullFeatureLab.Open(); SeagullFeatureLab.Instance.Configure(_manifest.projectId);
                    result.output = await SeagullFeatureLab.Instance.Execute(command.feature, command.operation, JsonUtility.FromJson<SeagullFeatureLab.Arguments>(command.argumentsJson));
                    result.invoked = true; // Completion/invocation is not a committed business outcome.
                    break;
                case "loadJson":
                    var text = await CloudSaveManager.Instance.LoadSnapshot(); result.success = text.IsSuccess; result.found = text.Found; result.revision = text.Revision?.ToString(CultureInfo.InvariantCulture); result.statusCode = text.StatusCode;
                    if (text.Data != null) result.dataBase64 = Convert.ToBase64String(new UTF8Encoding(false, true).GetBytes(text.Data)); break;
                case "loadBinary":
                    var bytes = await CloudSaveManager.Instance.LoadBytesSnapshot(); result.success = bytes.IsSuccess; result.found = bytes.Found; result.revision = bytes.Revision?.ToString(CultureInfo.InvariantCulture); result.statusCode = bytes.StatusCode;
                    if (bytes.Data != null) result.dataBase64 = Convert.ToBase64String(bytes.Data); break;
                case "saveJson": case "saveBinary":
                    if (!long.TryParse(command.expectedRevision, NumberStyles.None, CultureInfo.InvariantCulture, out var revision) || revision < 0 || revision.ToString(CultureInfo.InvariantCulture) != command.expectedRevision) { result.error = "ORIGINAL_REVISION_REQUIRED"; return; }
                    var payload = Convert.FromBase64String(command.dataBase64);
                    var write = command.action == "saveJson" ? await CloudSaveManager.Instance.Save(new UTF8Encoding(false, true).GetString(payload), revision) : await CloudSaveManager.Instance.SaveBytes(payload, revision);
                    result.success = write.IsSuccess; result.conflict = write.IsConflict; result.revision = write.Revision?.ToString(CultureInfo.InvariantCulture); result.statusCode = write.StatusCode; break;
                case "guide": await ReadGuide(command, result); break;
                case "capture":
                    string image = Path.Combine(_manifest.controlDirectory, "results", command.id + ".png"); ScreenCapture.CaptureScreenshot(image); await Task.Delay(1000); result.success = File.Exists(image); break;
                default: result.error = "UNKNOWN_ACTION"; break;
            }
        }
        private async Task ReadGuide(Command command, Result result)
        {
            string feature = command.feature;
            string route = feature == "groups" ? "/groups" : feature == "items" ? "/items" : feature == "matches" ? "/matches" : feature == "shared-objects" ? "/shared-objects/"
                : feature == "player-values" ? "/player-values" : feature == "statistics" ? "/statistics" : feature == "achievements" ? "/achievements"
                : feature == "inbox" ? "/inbox" : feature == "friends" ? "/friends" : feature == "chat" ? "/chat/channels/" : feature == "marketplace" ? "/marketplace/" : throw new InvalidOperationException("Unknown complete guide");
            int expectedResponses = feature == "statistics" || feature == "achievements" ? 2 : 1;
            if (feature == "marketplace" && (!Guid.TryParseExact(command.listingId, "D", out var listing) || command.listingId != listing.ToString("D"))) throw new InvalidOperationException("Original canonical listing ID required");
            if (feature == "chat" && (!Guid.TryParseExact(command.channelId, "D", out var channel) || command.channelId != channel.ToString("D"))) throw new InvalidOperationException("Original canonical channel ID required");
            bool invoked = false; var response = new TaskCompletionSource<long>(); var began = DateTimeOffset.UtcNow;
            bool Match(string url) => Uri.TryCreate(url, UriKind.Absolute, out var uri) && uri.GetLeftPart(UriPartial.Authority) == DevelopHost && uri.AbsolutePath.StartsWith("/api/v1/app" + route, StringComparison.Ordinal);
            void Observe(string url, long status)
            {
                if (!invoked || !Match(url)) return;
                if (++result.observedResponses == 1) { result.statusCode = status; result.secondsToHttpResponse = (DateTimeOffset.UtcNow - began).TotalSeconds; }
                result.lastStatusCode = status;
                if (status < 200 || status >= 300 || result.observedResponses >= expectedResponses) response.TrySetResult(status);
            }
            void Success(NetworkSuccessData value) => Observe(value.Url, value.StatusCode);
            void Failure(NetworkErrorData value) => Observe(value.Url, value.StatusCode);
            EventService.Instance.Subscribe<NetworkSuccessData>(EventKeys.NetworkRequestSuccess, Success);
            EventService.Instance.Subscribe<NetworkErrorData>(EventKeys.NetworkRequestFailed, Failure);
            try
            {
                invoked = true; // Cached event replay from Subscribe above cannot satisfy this read.
                switch (feature)
                {
                    case "groups": gameObject.AddComponent<GroupsDemo>().ReadMyGroups(); break;
                    case "items": gameObject.AddComponent<ItemsDemo>().ReadInventory(); break;
                    case "matches": gameObject.AddComponent<MatchesDemo>().ReadMyMatches(); break;
                    case "shared-objects": gameObject.AddComponent<SharedObjectsDemo>().ReadWorldMap(); break;
                    case "player-values": gameObject.AddComponent<PlayerValuesDemo>().RunReviewedAction(); break;
                    case "statistics": gameObject.AddComponent<StatisticsDemo>().RunReviewedAction(); break;
                    case "achievements": gameObject.AddComponent<AchievementsDemo>().RunReviewedAction(); break;
                    case "inbox": gameObject.AddComponent<InboxDemo>().RunReviewedAction(); break;
                    case "friends": gameObject.AddComponent<FriendsDemo>().RunReviewedAction(); break;
                    case "chat": gameObject.AddComponent<ChatDemo>().ReadChannelSnapshot(command.channelId); break;
                    case "marketplace":
                        var screen = gameObject.AddComponent<PM.horizOn.Cloud.Examples.Features.MarketplaceExample>();
                        JsonUtility.FromJsonOverwrite("{\"projectId\":\"" + _manifest.projectId + "\"}", screen);
                        var marketGuide = gameObject.AddComponent<MarketplaceDemo>(); marketGuide.screen = screen;
                        _ = marketGuide.ReadListing(command.listingId); break;
                }
                if (await Task.WhenAny(response.Task, Task.Delay(TimeSpan.FromMinutes(5))) != response.Task) { result.error = "NO_HTTP_RESPONSE_WITHIN_FIVE_MINUTES"; return; }
                result.lastStatusCode = await response.Task;
                result.success = result.observedResponses >= expectedResponses && result.statusCode >= 200 && result.statusCode < 300 && result.lastStatusCode >= 200 && result.lastStatusCode < 300; result.invoked = true;
                result.output = "Complete guide invoked in an existing native session. HTTP timing is separate from DTO/business acceptance.";
            }
            finally { EventService.Instance.Unsubscribe<NetworkSuccessData>(EventKeys.NetworkRequestSuccess, Success); EventService.Instance.Unsubscribe<NetworkErrorData>(EventKeys.NetworkRequestFailed, Failure); }
        }
        public static void CheckFileGates()
        {
            var manifest = new Manifest { stage = "develop", rootApproved = true, baseUrl = DevelopHost, projectId = Guid.NewGuid().ToString("D"), runId = Guid.NewGuid().ToString("D"), appApiKey = "synthetic-not-a-server-key", controlDirectory = Path.GetTempPath(), serverCommit = new string('a', 40), dashboardCommit = new string('b', 40) };
            Validate(manifest); manifest.baseUrl = "http://127.0.0.1:3171"; bool rejected = false; try { Validate(manifest); } catch (InvalidOperationException) { rejected = true; } if (!rejected) throw new Exception("Develop target escaped");
            manifest.baseUrl = DevelopHost; manifest.rootApproved = false; rejected = false; try { Validate(manifest); } catch (InvalidOperationException) { rejected = true; } if (!rejected) throw new Exception("Missing Root Go accepted");
            string file = Path.Combine(Path.GetTempPath(), "seagull-develop-started-" + Guid.NewGuid().ToString("N"));
            try { DurableNew(file, new byte[] { 0, 255, 1 }); rejected = false; try { DurableNew(file, new byte[] { 2 }); } catch (IOException) { rejected = true; } if (!rejected || !File.ReadAllBytes(file).SequenceEqual(new byte[] { 0, 255, 1 })) throw new Exception("Started marker was replaced"); }
            finally { File.Delete(file); }
        }
    }
}
#endif
