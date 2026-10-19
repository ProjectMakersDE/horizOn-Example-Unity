using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PM.horizOn.Cloud.Manager;
using PM.horizOn.Cloud.Objects.Network.Responses;
using TMPro;
using UnityEngine;
using static SeagullStorm.SeagullInterface;

namespace SeagullStorm
{
    internal sealed class SeagullStatisticsDemo
    {
        private readonly GameObject _panel;
        private readonly TMP_Text _text;
        private StatisticsIntentStore _store;
        private StatisticsSnapshot _snapshot;
        private StatisticDefinition[] _definitions;
        private string _scope, _host;
        private int _epoch;
        private bool _busy;
        public bool IsVisible => _panel != null && _panel.activeSelf;

        public SeagullStatisticsDemo(Transform canvas, Action close)
        {
            _panel = Panel(canvas, "Statistics demo", new Vector2(1210, 630), Vector2.zero, GameColors.DarkBG);
            var bounds = _panel.GetComponent<RectTransform>();
            bounds.anchorMin = Vector2.zero; bounds.anchorMax = Vector2.one; bounds.sizeDelta = Vector2.zero;
            Text(_panel.transform, "SESSION STATISTICS", new Vector2(850, 45), new Vector2(-110, 265), 28, GameColors.Orange);
            Button(_panel.transform, "BACK", new Vector2(125, 40), new Vector2(505, 265), () => { Hide(); close(); });
            Button(_panel.transform, "Read snapshot", new Vector2(220, 44), new Vector2(-380, 195), () => Run(Read));
            Button(_panel.transform, "Report completed runs", new Vector2(310, 44), new Vector2(-70, 195), () => Run(Begin));
            Button(_panel.transform, "Retry original batch", new Vector2(280, 44), new Vector2(280, 195), () => Run(Send));
            _text = Text(_panel.transform, "", new Vector2(1090, 410), new Vector2(0, -65), 20, GameColors.LightText);
            _text.alignment = TextAlignmentOptions.TopLeft; _text.richText = false;
            SeagullMenuScope.Bind(_panel, () => { Hide(); close(); }, _panel.transform.Find("Read snapshot").GetComponent<UnityEngine.UI.Button>());
        }

        public void Show()
        {
            _epoch++; _panel.SetActive(true); _snapshot = null; _definitions = null;
            _store = HorizonManager.Instance.Online ? StatisticsIntentStore.Current() : null; _scope = _store?.StorageKey;
            _host = PM.horizOn.Cloud.Core.HorizonApp.Network.GetActiveHost();
            Render("Complete an online run, then explicitly read and report. No request runs per kill or on opening this panel.");
            FocusControl(_panel.transform.Find("Read snapshot")?.GetComponent<UnityEngine.UI.Button>());
        }
        public void Hide() { _epoch++; if (_panel != null) _panel.SetActive(false); }
        private bool IsCurrent(int generation, string user, int epoch) => IsVisible && _epoch == epoch &&
            HorizonManager.Instance.IsCurrent(generation, user) && StatisticsIntentStore.Current().StorageKey == _scope &&
            PM.horizOn.Cloud.Core.HorizonApp.Network.GetActiveHost() == _host;
        private async void Run(Func<Task> action)
        {
            if (_busy) return;
            if (!HorizonManager.Instance.Online) { Render("Sign in with your own project. Offline practice never reports statistics."); return; }
            if (_store == null || StatisticsIntentStore.Current().StorageKey != _scope || PM.horizOn.Cloud.Core.HorizonApp.Network.GetActiveHost() != _host) { Render("Project or regional host changed. Close and reopen this panel."); return; }
            int epoch = _epoch; _busy = true;
            try { await action(); }
            catch (Exception) { if (IsVisible && _epoch == epoch) Render("Request or persistence failed. The saved original batch remains protected."); }
            finally { _busy = false; }
        }
        private async Task Read()
        {
            var manager = HorizonManager.Instance; int generation = manager.Generation, epoch = _epoch; string user = manager.GetUserId();
            var result = await manager.GetStatistics();
            if (!IsCurrent(generation, user, epoch)) return;
            if (result != null) { _snapshot = result; _definitions = StatisticsManager.Instance.CurrentDefinitions; }
            Render(result == null ? StatisticsManager.Instance.LastErrorCode : "Latest server snapshot loaded.");
        }
        private async Task Begin()
        {
            if (!_store.TryRead(out var state) || state.pending != null)
            { Render("An unresolved original batch exists. Use Retry original batch; never replace its ID or payload."); return; }
            if (state.rounds < 1) { Render("No completed online run is queued. Play an online run first."); return; }
            if (_snapshot == null) { Render("Read the server snapshot first to obtain its clock and definitions."); return; }
            string invalid = StatisticsIntentStore.ReportError(state, _definitions);
            if (invalid != null) { Render(invalid); return; }
            _store.Reserve(StatisticsManager.Instance.CreateOperationId());
            await Send();
        }
        private async Task Send()
        {
            if (!_store.TryRead(out var state) || state.pending == null) { Render("No readable pending batch. Unreadable storage is protected until reconciliation."); return; }
            var request = state.pending;
            var manager = HorizonManager.Instance; int generation = manager.Generation, epoch = _epoch; string user = manager.GetUserId();
            var result = await manager.UpdateStatistics(request.updates, request.operationId);
            if (!IsCurrent(generation, user, epoch)) return;
            if (result != null) { _snapshot = result; _definitions = StatisticsManager.Instance.CurrentDefinitions; _store.Confirm(request.operationId); }
            Render(result != null ? "Server confirmed this original batch. Later completed runs remain queued." :
                (StatisticsManager.Instance.LastErrorCode ?? "Uncertain outcome") + ". Original batch retained. OPERATION_EXPIRED requires reconciliation; never automatically generate another ID.");
        }
        private void Render(string message)
        {
            var text = new StringBuilder("One batch contains completed rounds, total kills and the best score.\nStatistics are independent of spendable values and local upgrade coins.\n");
            if (_store != null)
            {
                if (!_store.TryRead(out var state)) text.AppendLine("Unreadable saved intent protected; reconcile it before reporting.");
                else
                {
                    text.AppendLine($"Queued: {state.rounds:N0} rounds · {state.kills:N0} kills · best {state.bestScore:N0}");
                    if (state.pending != null) text.AppendLine("Original pending ID and exact batch are saved across restarts.");
                }
            }
            if (_snapshot != null)
            {
                text.AppendLine($"\nServer revision {_snapshot.revision} · catalog {_snapshot.definitionVersion}");
                foreach (var value in _snapshot.values.Take(8)) text.AppendLine(
                    $"{value.key}: {(value.hasValue ? value.value.ToString("N0") : "not reported")} · {_definitions?.FirstOrDefault(d => d.key == value.key)?.mode ?? "unknown mode"} · {value.period}");
                if (_snapshot.values.Length > 8) text.AppendLine($"Showing 8 of {_snapshot.values.Length} configured counters.");
            }
            text.AppendLine("\n" + message); _text.text = text.ToString();
        }
    }
}
