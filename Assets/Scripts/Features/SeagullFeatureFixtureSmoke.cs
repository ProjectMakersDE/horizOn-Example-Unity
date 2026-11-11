#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using PM.horizOn.Cloud.Core;
using PM.horizOn.Cloud.Manager;
using PM.horizOn.Cloud.Service;

namespace SeagullStorm
{
    /// <summary>Opt-in native consumer proof. Exact loopback only, and only in an isolated QA product.</summary>
    public sealed class SeagullFeatureFixtureSmoke : MonoBehaviour
    {
        public static bool Enabled => Environment.GetCommandLineArgs().Any(x => x.StartsWith("-seagull-feature-fixture=", StringComparison.Ordinal));
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (!Enabled) return;
            var go = new GameObject("Opt-in current-feature consumer proof"); DontDestroyOnLoad(go); go.AddComponent<SeagullFeatureFixtureSmoke>();
        }
        private async void Start()
        {
            try
            {
                if (!Application.productName.StartsWith("Seagull QA ", StringComparison.Ordinal)) throw new Exception("Isolated QA product required before touching preferences");
                var option = Environment.GetCommandLineArgs().Single(x => x.StartsWith("-seagull-feature-fixture=", StringComparison.Ordinal)).Substring("-seagull-feature-fixture=".Length);
                if (!Uri.TryCreate(option, UriKind.Absolute, out var uri) || uri.Scheme != "http" || uri.Host != "127.0.0.1" || option != uri.GetLeftPart(UriPartial.Authority)) throw new Exception("Exact loopback fixture required");
                Application.runInBackground = true;
                while (HorizonManager.Instance == null) await Task.Yield();
                var config = ScriptableObject.CreateInstance<HorizonConfig>(); config.SetApiKey("feature-consumer-fixture"); config.SetHosts(new[] { option });
                NetworkService.Instance.Initialize(config); NetworkService.Instance.SetActiveHost(option);
                LogService.Instance.Initialize(config); LogService.Instance.EnableUnityLogging = false;
                HorizonManager.Instance.Configured = true;
                if (!await HorizonManager.Instance.SignInEmail("lab@example.invalid", "synthetic-password")) throw new Exception("Fixture sign-in failed");
                SeagullFeatureLab.Open(); SeagullFeatureLab.Instance.Configure("10000000-0000-4000-8000-000000001016");
                async Task<string> Act(string feature, string action, SeagullFeatureLab.Arguments args = null)
                    => await SeagullFeatureLab.Instance.Execute(feature, action, args ?? new SeagullFeatureLab.Arguments());
                void Need(bool ok, string detail) { if (!ok) throw new Exception(detail); }
                bool resume = Environment.GetCommandLineArgs().Contains("-seagull-feature-resume");
                if (!resume)
                {
                Need((await Act("Achievements", "Read")).Contains("definitions"), "Achievements read");
                Need((await Act("Friends", "Read")).Contains("revision"), "Friends read");
                var friend = new SeagullFeatureLab.Arguments { target = "00000000-0000-4000-8000-000000000002", revision = "9007199254740000" };
                Need((await Act("Friends", "request", friend)).Contains("UNAVAILABLE"), "Uncertain Friends request");
                string before = await Act("Friends", "Inspect original");
                Destroy(SeagullFeatureLab.Instance.gameObject); await Task.Delay(100); Need(SeagullFeatureLab.Instance == null, "Native component was destroyed"); SeagullFeatureLab.Open(); SeagullFeatureLab.Instance.Configure("10000000-0000-4000-8000-000000001016");
                Need(await Act("Friends", "Inspect original") == before, "Friends original survives actual component recreation");
                Need((await Act("Friends", "Read")).Contains("revision"), "Friends authoritative reconciliation");
                Need((await Act("Friends", "Acknowledge newer revision")).Contains("acknowledged"), "New revision fences old original");
                var inbox = await Act("Inbox", "Read"); Need(inbox.Contains("message:"), "Inbox read");
                var entry = new SeagullFeatureLab.Arguments { id = "message:aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa" };
                Need((await Act("Inbox", "Claim selected original", entry)).Contains("UNAVAILABLE"), "Uncertain Inbox claim");
                before = await Act("Inbox", "Inspect original");
                Destroy(SeagullFeatureLab.Instance.gameObject); await Task.Delay(100); Need(SeagullFeatureLab.Instance == null, "Native component was destroyed"); SeagullFeatureLab.Open(); SeagullFeatureLab.Instance.Configure("10000000-0000-4000-8000-000000001016");
                Need(await Act("Inbox", "Inspect original") == before, "Inbox original survives actual component recreation");
                    if (Environment.GetCommandLineArgs().Contains("-seagull-feature-stop-before-restart"))
                    {
                        using (var pending = new FeatureOriginalStore(HorizonApp.Network.GetApiKey(), HorizonManager.Instance.GetUserId(), "Inbox"))
                        using (var hash = System.Security.Cryptography.SHA256.Create())
                            File.WriteAllText(Path.Combine(Application.persistentDataPath, "fixture-inbox-original.sha256"), Convert.ToBase64String(hash.ComputeHash(System.Text.Encoding.UTF8.GetBytes(pending.Read()))));
                        Debug.Log("[CURRENT FEATURE NATIVE] RESTART_READY exact uncertain Inbox original saved before process exit");
                        Application.Quit(0); return;
                    }
                }
                else
                {
                    using (var pending = new FeatureOriginalStore(HorizonApp.Network.GetApiKey(), HorizonManager.Instance.GetUserId(), "Inbox"))
                    using (var hash = System.Security.Cryptography.SHA256.Create())
                        Need(File.ReadAllText(Path.Combine(Application.persistentDataPath, "fixture-inbox-original.sha256")) == Convert.ToBase64String(hash.ComputeHash(System.Text.Encoding.UTF8.GetBytes(pending.Read()))), "Real restarted process kept byte-identical original payload");
                    Need((await Act("Inbox", "Inspect original")).Contains("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa"), "Decoded original identity is displayed without replacing its stored tokens");
                }
                Need((await Act("Inbox", "Retry original")).Contains("instanceId"), "Original Inbox retry returns current reference");
                Need((await Act("Items", "Read")).Contains("instances"), "Authoritative inventory, no synthesized historical properties");
                var prepared = await HorizonManager.Instance.PrepareGiftCode("GIFT"); Need(prepared?.expectedCount == "9007199254740993", "Exact Int64 prepared count");
                Need(await HorizonManager.Instance.RedeemPendingGift() == null, "Uncertain Gift response preserves original");
                Need(HorizonManager.Instance.ReadPendingGift(out var saved) && saved.expectedCount == prepared.expectedCount, "Gift original retained");
                var redeemed = await HorizonManager.Instance.RedeemPendingGift(); Need(redeemed?.alreadyApplied == true && redeemed.count == "9007199254740994", "Gift original replay confirmed");
                Need(HorizonManager.Instance.ReadPendingGift(out saved) && saved == null, "Confirmed Gift ACK");
                string capture = Environment.GetCommandLineArgs().FirstOrDefault(x => x.StartsWith("-seagull-capture=", StringComparison.Ordinal))?.Substring("-seagull-capture=".Length);
                if (capture != null) { Directory.CreateDirectory(Path.GetDirectoryName(capture)); ScreenCapture.CaptureScreenshot(capture); await Task.Delay(700); }
                Debug.Log("[CURRENT FEATURE NATIVE] PASS actual game/Hello facade, recreated consumers, unchanged uncertain originals, exact Inbox item reference/current inventory, V2 Gift Int64 retry and no local reward synthesis");
                Application.Quit(0);
            }
            catch (Exception error) { Debug.LogError("[CURRENT FEATURE NATIVE] FAIL " + error.Message); Application.Quit(1); }
        }
    }
}
#endif
