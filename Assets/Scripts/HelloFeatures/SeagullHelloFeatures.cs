using System;
using System.Threading.Tasks;
using UnityEngine;
using PM.horizOn.Cloud.Core;

namespace SeagullStorm
{
    /// <summary>Standalone current-feature Hello. Connection, identity and every feature action are explicit.</summary>
    public sealed class SeagullHelloFeatures : MonoBehaviour
    {
        private string _status = "Import your own project config, then connect. No startup network action has run.", _gift = "";
        private bool _busy;
        private Vector2 _scroll;
        private void Awake() { if (HorizonManager.Instance == null) new GameObject("Existing session facade").AddComponent<HorizonManager>(); }
        private void OnGUI()
        {
            if (SeagullFeatureLab.Instance?.Visible == true) return;
            GUI.backgroundColor = new Color(.95f, .37f, .12f);
            GUILayout.BeginArea(new Rect(40, 35, Mathf.Min(Screen.width - 80, 1100), Screen.height - 70));
            _scroll = GUILayout.BeginScrollView(_scroll);
            GUILayout.Label("horizOn / HELLO CURRENT FEATURES");
            GUILayout.Label("One existing identity; explicit reads and durable originals. Practice game coins are unrelated to server-owned rewards.");
            GUI.enabled = !_busy;
            if (GUILayout.Button("Connect (no signup)", GUILayout.Height(38))) Run(async () =>
            {
                var h = HorizonManager.Instance;
                h.Configured = HorizonApp.Initialize() && await new HorizonServer().Connect();
                _status = h.Configured ? "Connected. Restore an existing identity or create one explicitly." : "Connection unavailable; original identity retained.";
            });
            if (GUILayout.Button("Restore existing identity", GUILayout.Height(38))) Run(async () => _status = await HorizonManager.Instance.RestoreSession() ? "Existing player restored." : "Restore failed; no replacement identity was created.");
            if (GUILayout.Button("Create guest explicitly (only without a saved identity)", GUILayout.Height(38))) Run(async () => _status = await HorizonManager.Instance.SignUpGuest("SeagullHello") ? "Player connected." : "Creation unresolved/refused; recover existing identity first.");
            if (GUILayout.Button("Read authoritative player values", GUILayout.Height(38))) Run(async () =>
            { var r = await HorizonManager.Instance.GetPlayerValues(); _status = r?.State == null ? "Read refused or unavailable." : JsonUtility.ToJson(r.State, true); });
            if (GUILayout.Button("Read statistics", GUILayout.Height(38))) Run(async () =>
            { var r = await HorizonManager.Instance.GetStatistics(); _status = r == null ? "Read refused or unavailable." : JsonUtility.ToJson(r, true); });
            if (GUILayout.Button("Open current feature lab: Achievements / Friends / Groups / Inbox / Items / Market / Matches / Shared / Chat", GUILayout.Height(45))) SeagullFeatureLab.Open();
            _gift = GUILayout.TextField(_gift);
            if (GUILayout.Button("Prepare and save V2 gift (no redemption)", GUILayout.Height(38))) Run(async () =>
            { var r = await HorizonManager.Instance.PrepareGiftCode(_gift); _status = r == null ? HorizonManager.Instance.LastGiftError : "Saved original campaign " + r.campaignId + " / expected " + r.expectedCount + " / revision " + r.configRevision + ". Confirm separately."; });
            if (GUILayout.Button("Confirm / retry the saved V2 gift", GUILayout.Height(38))) Run(async () =>
            { var r = await HorizonManager.Instance.RedeemPendingGift(); _status = r == null ? HorizonManager.Instance.LastGiftError + ". Original retained." : "Server confirmed count " + r.count + ". Read authoritative holdings explicitly."; });
            if (GUILayout.Button("Sign out", GUILayout.Height(38))) { HorizonManager.Instance.SignOut(); _status = "Signed out. Pending originals remain scoped to their original owner."; }
            GUI.enabled = true;
            GUILayout.Label(HorizonManager.Instance.ConnectionStatus);
            if (HorizonManager.Instance.ReadPendingGift(out var pending) && pending != null) GUILayout.Label("Saved gift: " + pending.code + " / " + pending.expectedCount + " / " + pending.configRevision);
            GUILayout.TextArea(_status);
            GUILayout.EndScrollView(); GUILayout.EndArea();
        }
        private async void Run(Func<Task> action)
        {
            if (_busy) return; _busy = true;
            try { await action(); } catch (Exception e) { _status = e.GetType().Name + ": original identity and intents retained."; }
            finally { _busy = false; }
        }
    }
}
