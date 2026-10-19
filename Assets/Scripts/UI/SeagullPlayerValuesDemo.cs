using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PM.horizOn.Cloud.Core;
using PM.horizOn.Cloud.Manager;
using PM.horizOn.Cloud.Enums;
using PM.horizOn.Cloud.Service;
using PM.horizOn.Cloud.Objects.Network.Responses;
using TMPro;
using UnityEngine;
using static SeagullStorm.SeagullInterface;

namespace SeagullStorm
{
    /// <summary>Explicit SDK requests in the existing uGUI hub; practice progress is independent.</summary>
    internal sealed class SeagullPlayerValuesDemo
    {
        private readonly GameObject _panel;
        private readonly TMP_Text _text;
        private PlayerValueIntentStore _store;
        private PlayerValueOffer[] _offers = Array.Empty<PlayerValueOffer>();
        private PlayerValuesState _state;
        private string _scope, _host;
        private int _offerIndex, _valueIndex, _epoch;
        private long _offerDefinition = -1;
        private bool _busy;
        public bool IsVisible => _panel != null && _panel.activeSelf;

        public SeagullPlayerValuesDemo(Transform canvas, Action close)
        {
            _panel = Panel(canvas, "Player values demo", new Vector2(1210,630), Vector2.zero, GameColors.DarkBG);
            var bounds = _panel.GetComponent<RectTransform>();
            bounds.anchorMin = Vector2.zero; bounds.anchorMax = Vector2.one; bounds.sizeDelta = Vector2.zero;
            Text(_panel.transform, "BACKEND BALANCES", new Vector2(850,45), new Vector2(-110,265), 28, GameColors.Orange);
            Button(_panel.transform, "BACK", new Vector2(125,40), new Vector2(505,265), () => { Hide(); close(); });
            Button(_panel.transform, "Read balances", new Vector2(170,44), new Vector2(-455,195), () => Run(Read));
            Button(_panel.transform, "Next value", new Vector2(160,44), new Vector2(-270,195), () => { if (!_busy) { _valueIndex++; Render(); } });
            Button(_panel.transform, "Next offer", new Vector2(160,44), new Vector2(-95,195), () => { if (!_busy) { _offerIndex++; Render(); } });
            Button(_panel.transform, "Buy offer", new Vector2(160,44), new Vector2(80,195), () => Run(() => Begin(false)));
            Button(_panel.transform, "Adjust +1", new Vector2(160,44), new Vector2(255,195), () => Run(() => Begin(true)));
            Button(_panel.transform, "Retry pending", new Vector2(190,44), new Vector2(445,195), () => Run(Send));
            _text = Text(_panel.transform, "", new Vector2(1090,410), new Vector2(0,-65), 20, GameColors.LightText);
            _text.alignment = TextAlignmentOptions.TopLeft; _text.richText = false;
            SeagullMenuScope.Bind(_panel, () => { Hide(); close(); }, _panel.transform.Find("Read balances").GetComponent<UnityEngine.UI.Button>());
        }

        private static PlayerValueIntentStore CurrentStore() => new PlayerValueIntentStore(
            HorizonApp.Network.GetActiveHost(), HorizonApp.Network.GetApiKey(), HorizonManager.Instance.GetUserId(), HorizonConfig.Load()?.Hosts);

        public void Show()
        {
            _epoch++; _panel.SetActive(true);
            _store = HorizonManager.Instance.Online ? CurrentStore() : null; _scope = _store?.StorageKey;
            _host = HorizonApp.Network.GetActiveHost();
            _state = null; _offers = Array.Empty<PlayerValueOffer>(); _offerDefinition = -1;
            Render("Read balances and offers before a new action. All requests are explicit.");
            FocusControl(_panel.transform.Find("Read balances")?.GetComponent<UnityEngine.UI.Button>());
        }
        public void Hide() { _epoch++; if (_panel != null) _panel.SetActive(false); }

        private bool IsCurrent(int generation, string user, int epoch) => IsVisible && _epoch == epoch &&
            HorizonManager.Instance.IsCurrent(generation, user) && CurrentStore().StorageKey == _scope && HorizonApp.Network.GetActiveHost() == _host;

        private async void Run(Func<Task> action)
        {
            if (_busy) return;
            if (!HorizonManager.Instance.Online) { Render("Sign in with your own project. Practice coins remain local."); return; }
            if (_store == null || CurrentStore().StorageKey != _scope || HorizonApp.Network.GetActiveHost() != _host) { Render("Project or regional host changed. Close and reopen this panel before another request."); return; }
            int epoch = _epoch;
            _busy = true;
            try { await action(); }
            catch (Exception) { if (IsVisible && _epoch == epoch) Render("Request or local persistence failed. Saved pending actions are retained; read and retry them."); }
            finally { _busy = false; }
        }

        private async Task Read()
        {
            var manager = HorizonManager.Instance; int generation = manager.Generation, epoch = _epoch; string user = manager.GetUserId();
            var state = await manager.GetPlayerValues();
            if (!IsCurrent(generation, user, epoch)) return;
            if (state == null || !state.IsSuccess) { Render(state?.ErrorCode ?? "Session changed"); return; }
            var offers = await manager.GetValueOffers();
            if (!IsCurrent(generation, user, epoch)) return;
            _state = PlayerValuesManager.Instance.CurrentState;
            _offers = offers?.Data?.offers?.Where(o => o != null && o.active).ToArray() ?? Array.Empty<PlayerValueOffer>();
            _offerDefinition = offers?.IsSuccess == true ? offers.Data.definitionVersion : -1;
            Render(offers?.IsSuccess == true ? "Loaded from the server." : offers?.ErrorCode ?? "Offers unavailable");
        }

        private async Task Begin(bool adjust)
        {
            if (!_store.TryRead(out var pending) || pending != null)
            { Render("An earlier intent exists. Reconcile it or use Retry pending; a new ID is never substituted."); return; }
            if (!PlayerValuesManager.Instance.HasServerClock || PlayerValuesManager.Instance.CurrentState == null || _state == null)
            { Render("Read current balances and offers first."); return; }
            if (adjust ? _state.values.Length == 0 : _offers.Length == 0)
            { Render(adjust ? "No value is configured." : "No active offer is configured."); return; }
            if (!adjust)
            {
                var selected = _offers[_offerIndex % _offers.Length];
                string displayed = JsonUtility.ToJson(selected); long displayedVersion = _offerDefinition;
                var manager = HorizonManager.Instance; int generation = manager.Generation, epoch = _epoch; string user = manager.GetUserId();
                var fresh = await manager.GetValueOffers();
                if (!IsCurrent(generation, user, epoch)) return;
                if (fresh?.IsSuccess != true) { Render("Fresh offer lookup failed. No purchase was sent."); return; }
                _offers = fresh.Data.offers.Where(o => o != null && o.active).ToArray();
                _offerDefinition = fresh.Data.definitionVersion;
                int found = Array.FindIndex(_offers, offer => offer.key == selected.key);
                _offerIndex = found >= 0 ? found : 0;
                if (found < 0 || displayedVersion != _offerDefinition || JsonUtility.ToJson(_offers[found]) != displayed)
                { Render("Offers changed. Review the refreshed costs and rewards, then explicitly choose Buy offer again. No purchase was sent."); return; }
            }
            var intent = new PlayerValueIntent { userId = HorizonManager.Instance.GetUserId(), kind = adjust ? "adjust" : "purchase",
                operationId = PlayerValuesManager.Instance.CreateOperationId(), amount = adjust ? 1 : 0,
                valueKey = adjust ? _state.values[_valueIndex % _state.values.Length].key : null, reason = adjust ? "example-explicit-adjustment" : null,
                offerKey = adjust ? null : _offers[_offerIndex % _offers.Length].key };
            _store.Reserve(intent);
            await Send();
        }

        private async Task Send()
        {
            if (!_store.TryRead(out var intent) || intent == null)
            { Render("No valid pending action. Unreadable intents are protected until reconciliation."); return; }
            var manager = HorizonManager.Instance; int generation = manager.Generation, epoch = _epoch; string user = manager.GetUserId();
            int series = _store.MarkAttempt(intent.operationId), transportAttempts = 0;
            string url = _host + "/api/v1/app/player-values/" + (intent.kind == "purchase" ? "purchase" : "adjust");
            bool armed = false;
            Action<NetworkRequestData> started = data => { if (armed && data?.Method == "POST" && data.Url == url) transportAttempts++; };
            // Subscribe replays a cached event synchronously; it is deliberately excluded before arming.
            var events = HorizonApp.Events;
            events.Subscribe(EventKeys.NetworkRequestStarted, started);
            PlayerValuesResult result;
            try
            {
                armed = true;
                result = intent.kind == "purchase"
                    ? await manager.PurchaseValueOffer(intent.offerKey, intent.operationId)
                    : await manager.AdjustValue(intent.valueKey, intent.amount, intent.reason, intent.operationId);
            }
            finally { armed = false; events.Unsubscribe(EventKeys.NetworkRequestStarted, started); }
            if (!IsCurrent(generation, user, epoch)) return;
            if (result?.State != null)
            {
                _state = result.State;
                if (_state.definitionVersion != _offerDefinition) { _offers = Array.Empty<PlayerValueOffer>(); _offerDefinition = -1; }
            }
            if (result?.IsSuccess == true) _store.Confirm(intent.operationId);
            bool unbooked = result?.IsSuccess == false && _store.RejectFirstUnbooked(intent.operationId,
                series, transportAttempts, result.ErrorCode, result.State != null);
            Render(result?.IsSuccess == true ? "Confirmed by the server. Practice coins are unchanged." :
                unbooked ? result.ErrorCode + ". Server confirmed no booking on the only transport attempt. Review the state before a new explicit action." :
                (result?.ErrorCode ?? "Uncertain outcome") + ". Pending intent kept. OPERATION_EXPIRED requires reconciliation; never retry with a new ID.");
        }

        private void Render(string message = null)
        {
            var text = new StringBuilder("Separate backend balances; offline practice coins are unchanged.\n");
            if (_state?.values.Length > 0)
            {
                var value = _state.values[_valueIndex % _state.values.Length];
                text.AppendLine($"Selected value {_valueIndex % _state.values.Length + 1}/{_state.values.Length}");
                text.AppendLine($"{value.key}: {value.balance:N0}  ·  earned today {value.earnedToday:N0}  ·  cap {(value.HasDailyCap ? value.dailyCap.ToString() : "none")}");
            }
            if (_offers.Length > 0)
            {
                var offer = _offers[_offerIndex % _offers.Length];
                text.AppendLine("Selected: " + offer.name + " [" + offer.key + "]");
                text.AppendLine("Cost: " + Amounts(offer.costs));
                text.AppendLine("Reward: " + Amounts(offer.rewards));
            }
            text.AppendLine("Adjust +1 requests the selected value; its studio adjustment rule must be enabled.");
            if (_store != null)
            {
                if (!_store.TryRead(out var pending)) text.AppendLine("Unreadable pending intent protected. Reconcile it before another action.");
                else if (pending != null) text.AppendLine("Pending " + pending.kind + ": same ID and payload retained across restarts.");
            }
            if (!string.IsNullOrEmpty(message)) text.AppendLine("\n" + message);
            _text.text = text.ToString();
        }
        private static string Amounts(PlayerValueAmount[] values) => values == null ? "none" :
            string.Join(", ", values.Take(4).Select(x => x.amount + " " + x.key)) + (values.Length > 4 ? $" … ({values.Length} total)" : "");
    }
}
