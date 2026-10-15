using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using PM.horizOn.Cloud.Core;
using PM.horizOn.Cloud.Manager;
using PM.horizOn.Cloud.Objects.Network.Responses;
using UnityEngine;

namespace SeagullStorm
{
    /// <summary>Standalone Hello scene. Network calls happen only through its explicit buttons.</summary>
    public sealed class StatisticsHello : MonoBehaviour
    {
        private bool _connected, _busy;
        private StatisticsSnapshot _snapshot;
        private StatisticDefinition[] _definitions;
        private string _scope, _host;
        private GUIStyle _button, _label, _heading;
        private string _status = "Import your own horizOn config. Connect/restore preserves a cached identity after failures.";
        private StatisticsIntentStore Store() => new StatisticsIntentStore(HorizonApp.Network.GetActiveHost(),
            HorizonApp.Network.GetApiKey(), UserManager.Instance.CurrentUser?.UserId, "horizon.statistics.hello.", HorizonConfig.Load()?.Hosts);
        private bool SignedIn => _connected && UserManager.Instance.IsSignedIn;
        private string GuestPendingKey()
        {
            const string prefix = "horizon.statistics.hello.guest-pending.";
            string key = DurableIntentScope.Key(HorizonApp.Network.GetApiKey(), "", prefix);
            if (!DurableIntentScope.Migrate(key, HorizonApp.Network.GetActiveHost(), HorizonApp.Network.GetApiKey(), "", prefix, HorizonConfig.Load()?.Hosts))
            { PlayerPrefs.SetString(key, "Unresolved legacy guest creation"); PlayerPrefs.Save(); }
            return key;
        }

        private IEnumerator Start()
        {
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-statistics-hello-smoke") < 0) yield break;
            Application.runInBackground = true;
            StatisticsIntentSmoke.Check();
            yield return new WaitForSecondsRealtime(2);
            if (_connected || _busy) { Debug.LogError("[STATISTICS HELLO SMOKE] Unexpected automatic connection"); Application.Quit(1); yield break; }
            string directory = Path.Combine(Environment.CurrentDirectory, "tmp"); Directory.CreateDirectory(directory);
            ScreenCapture.CaptureScreenshot(Path.Combine(directory, "statistics-hello-smoke.png"));
            yield return new WaitForSecondsRealtime(.5f);
            Debug.Log("[STATISTICS HELLO SMOKE] PASS standalone controls rendered; no automatic backend connection");
            Application.Quit(0);
        }

        private void OnGUI()
        {
            if (_button == null)
            {
                _label = new GUIStyle(GUI.skin.label) { fontSize = 18, wordWrap = true };
                _label.normal.textColor = new Color(.93f,.92f,.88f);
                _heading = new GUIStyle(_label) { fontSize = 25, fontStyle = FontStyle.Bold };
                _heading.normal.textColor = new Color(.95f,.55f,.3f);
                _button = new GUIStyle(GUI.skin.button) { fontSize = 18, fixedHeight = 42,
                    margin = new RectOffset(0,0,5,5) };
                foreach (var state in new[] { _button.normal, _button.hover, _button.active, _button.focused })
                { state.background = Texture2D.whiteTexture; state.textColor = new Color(.08f,.07f,.06f); }
            }
            if (SignedIn && (_scope != Store().StorageKey || _host != HorizonApp.Network.GetActiveHost()))
            { _scope = Store().StorageKey; _host = HorizonApp.Network.GetActiveHost(); _snapshot = null; _definitions = null; }
            GUI.backgroundColor = new Color(.95f, .37f, .12f);
            GUILayout.BeginArea(new Rect(40, 35, Mathf.Min(Screen.width - 80, 1050), Screen.height - 70));
            GUILayout.Label("horizOn / Statistics Hello", _heading);
            GUILayout.Label("One explicit session batch. Original IDs survive restarts.", _label);
            GUILayout.Space(12);
            GUI.enabled = !_busy;
            if (GUILayout.Button("Connect and restore existing identity", _button)) Run(async () =>
            {
                HorizonApp.Initialize(); _connected = await new HorizonServer().Connect();
                if (!_connected) { _status = "Connection failed; existing identity retained."; return; }
                if (!UserManager.Instance.IsSignedIn && UserManager.Instance.HasCachedAnonymousToken())
                {
                    if (!await GuestIdentityGuard.Run(HorizonApp.Network.GetApiKey(), true, () => UserManager.Instance.RestoreAnonymousSession()))
                    { _status = "Restore failed. Reconcile the original identity; no replacement player is created."; return; }
                }
                if (SignedIn) { PlayerPrefs.DeleteKey(GuestPendingKey()); PlayerPrefs.Save(); }
                _status = SignedIn ? "Signed in. Read the snapshot explicitly." : "No existing player. Create a guest explicitly if intended.";
            });
            GUI.enabled = !_busy && _connected && !UserManager.Instance.IsSignedIn && !UserManager.Instance.HasCachedAnonymousToken() &&
                !PlayerPrefs.HasKey(GuestPendingKey()) && !GuestIdentityGuard.Blocked(HorizonApp.Network.GetApiKey());
            if (GUILayout.Button("Create a new guest explicitly", _button)) Run(async () =>
            {
                string pending = GuestPendingKey(); PlayerPrefs.SetString(pending, "StatisticsHello"); PlayerPrefs.Save();
                bool created = await GuestIdentityGuard.Run(HorizonApp.Network.GetApiKey(), false,
                    () => UserManager.Instance.RestoreAnonymousSession(), () => UserManager.Instance.SignUpAnonymous("StatisticsHello"));
                if (created && SignedIn && GuestPendingKey() == pending) { PlayerPrefs.DeleteKey(pending); PlayerPrefs.Save(); }
                _status = created ? "Guest created. Read snapshot." : "Guest creation unresolved; reconcile before creating another identity.";
            });
            GUI.enabled = !_busy && SignedIn;
            if (GUILayout.Button("Read snapshot", _button)) Run(async () =>
            {
                string scope = Store().StorageKey, host = HorizonApp.Network.GetActiveHost();
                var result = await StatisticsManager.Instance.GetStatistics();
                if (!SignedIn || Store().StorageKey != scope || HorizonApp.Network.GetActiveHost() != host) return;
                if (result != null) { _snapshot = result; _definitions = StatisticsManager.Instance.CurrentDefinitions; }
                _status = result == null ? StatisticsManager.Instance.LastErrorCode : "Loaded server snapshot and operation window.";
            });
            if (GUILayout.Button("Queue a simulated completed round: 3 kills, score 250", _button)) Run(() =>
            { Store().CompleteRun(3, 250); _status = "Session facts saved locally; no network call made."; return Task.CompletedTask; });
            if (GUILayout.Button("Send queued sessions once", _button)) Run(async () =>
            {
                var store = Store();
                if (!store.TryRead(out var state) || state.pending != null) { _status = "Original pending batch protected. Retry it unchanged."; return; }
                string invalid = StatisticsIntentStore.ReportError(state, _snapshot == null ? null : _definitions);
                if (invalid != null) { _status = invalid; return; }
                store.Reserve(StatisticsManager.Instance.CreateOperationId());
                await Send(store);
            });
            if (GUILayout.Button("Retry the exact original batch", _button)) Run(() => Send(Store()));
            GUI.enabled = true;
            GUILayout.Space(15); GUILayout.Label(_status, _label);
            if (SignedIn && Store().TryRead(out var saved)) GUILayout.Label("Queued rounds: " + saved.rounds + "; pending original batch: " + (saved.pending != null), _label);
            if (_snapshot != null) foreach (var value in _snapshot.values) GUILayout.Label(
                value.key + ": " + (value.hasValue ? value.value.ToString() : "not reported") + " / " +
                (_definitions?.FirstOrDefault(d => d.key == value.key)?.mode ?? "unknown mode") + " / " + value.period, _label);
            GUILayout.EndArea();
        }
        private async Task Send(StatisticsIntentStore store)
        {
            if (!store.TryRead(out var state) || state.pending == null) { _status = "No readable original batch; unreadable storage remains protected."; return; }
            string host = HorizonApp.Network.GetActiveHost();
            var result = await StatisticsManager.Instance.UpdateStatistics(state.pending.updates, state.pending.operationId);
            if (!SignedIn || Store().StorageKey != store.StorageKey || HorizonApp.Network.GetActiveHost() != host) return;
            if (result != null) { _snapshot = result; _definitions = StatisticsManager.Instance.CurrentDefinitions; store.Confirm(state.pending.operationId); }
            _status = result != null ? "Confirmed server snapshot; replayed=" + result.replayed :
                StatisticsManager.Instance.LastErrorCode + ". Original ID/payload retained. Expired IDs require reconciliation, never replacement.";
        }
        private async void Run(Func<Task> action)
        {
            if (_busy) return; _busy = true;
            try { await action(); } catch (Exception) { _status = "Operation failed; existing identity and original pending batch retained."; }
            finally { _busy = false; }
        }
    }
}
