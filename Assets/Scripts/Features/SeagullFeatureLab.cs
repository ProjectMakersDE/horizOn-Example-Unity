using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using static SeagullStorm.SeagullInterface;
using PM.horizOn.Cloud.Core;
using PM.horizOn.Cloud.Manager;
using PM.horizOn.Cloud.Objects.Network.Responses;
using PM.horizOn.Cloud.Examples.Features;

namespace SeagullStorm
{
    /// <summary>Explicit current-session workbench. Official samples own their complex durable flows.</summary>
    public sealed class SeagullFeatureLab : MonoBehaviour
    {
        [Serializable] public sealed class Arguments
        {
            public string id, target, key, requestId, role, name = "Seagulls", description = "", mode = "open", revision = "0";
            public string json = "{}", base64 = "", text = "Hello from Seagull Storm", channelId, operationId, issuedAt;
            public string amount = "1", price = "10", duration = "3600"; public bool flag; public string[] ids = Array.Empty<string>();
        }
        [Serializable] private sealed class Binding { public string projectId, collection; }
        private static readonly string[] Features = { "Achievements", "Friends", "Groups", "Inbox", "Items", "Marketplace", "Matches", "Shared Objects", "Chat" };
        private static readonly Dictionary<string, string[]> Actions = new Dictionary<string, string[]>
        {
            ["Achievements"] = new[] { "Read", "Unlock original key", "Retry original", "Deliver selected legacy reward" },
            ["Friends"] = new[] { "Read", "Find exact", "request", "accept", "decline", "cancel", "remove", "block", "unblock", "Retry original", "Inspect original", "Acknowledge newer revision" },
            ["Groups"] = new[] { "Read", "Review group", "Read configured member page", "Next configured member page", "Create", "Update metadata", "join", "request", "invite", "accept-invite", "decline-invite", "cancel-invite", "accept-request", "decline-request", "cancel-request", "leave", "remove", "role", "transfer", "delete", "Retry original", "Acknowledge saved success" },
            ["Inbox"] = new[] { "Read", "Mark read", "Remove", "Claim selected original", "Retry original", "Inspect original" },
            ["Items"] = new[] { "Read", "Read catalog page", "Review offers", "Purchase reviewed offer", "Equip reviewed instance", "Adjust reviewed property", "Discard reviewed instance", "Retry original", "Retry original after changed-offer confirmation", "Reconcile reviewed mutation", "Acknowledge saved success" },
            ["Marketplace"] = new[] { "Read limits", "Read rules page", "Read", "Read listings page", "Read exact listing", "Read inventory", "Review create", "Review buy", "Review cancel", "Confirm reviewed trade", "Retry original", "Look up original", "Retry return delivery", "Claim return original" },
            ["Matches"] = new[] { "Read limits", "Read", "Review match", "Find open page", "Create reviewed lobby", "join", "decline", "start", "turn", "timeout", "leave", "cancel", "report", "ranked", "Submit original ranked result", "Recover original ranked source", "Retry original", "Reconcile original create", "Reconcile original action", "Acknowledge saved result" },
            ["Shared Objects"] = new[] { "Read collections page", "Read usage", "Review object", "Create reviewed object", "Update reviewed object", "Delete reviewed object", "Read rectangle page", "Find opponent sample", "Retry original", "Reconcile original object", "Reconcile original create", "Acknowledge saved result" },
            ["Chat"] = new[] { "Open project channel", "Read current channel", "Send original text", "Retry original", "Look up original", "Read local history", "Inspect original" }
        };
        public static SeagullFeatureLab Instance { get; private set; }
        public bool Visible { get; private set; }
        public bool Busy { get; private set; }
        public string LastOutput { get; private set; } = "No network action has run.";
        private string _project = "", _collection = "levels", _arguments = "{}", _scope, _chatChannel;
        private int _feature;
        private GameObject _view;
        private TMP_InputField _projectField, _collectionField, _argumentField, _outputField;
        private Transform _actionList;
        private readonly List<Button> _actionButtons = new List<Button>();
        private readonly List<Button> _featureButtons = new List<Button>();
        private readonly Dictionary<Type, MonoBehaviour> _recipes = new Dictionary<Type, MonoBehaviour>();
        public static void Open()
        {
            if (Instance == null) new GameObject("Current SDK feature lab").AddComponent<SeagullFeatureLab>();
            Instance.BuildView(); Instance.Visible = true; Instance._view.SetActive(true);
            Instance._projectField.SetTextWithoutNotify(Instance._project);
            Instance._collectionField.SetTextWithoutNotify(Instance._collection);
        }
        public void Configure(string stableProjectUuid, string sharedCollection = "levels")
        {
            if (Busy || !Guid.TryParseExact(stableProjectUuid, "D", out var id)) throw new InvalidOperationException("An idle screen and stable project UUID are required");
            _project = id.ToString("D"); _collection = sharedCollection;
        }
        private void Awake() { if (Instance != null && Instance != this) { Destroy(gameObject); return; } Instance = this; DontDestroyOnLoad(gameObject); }
        private void Hide() { Visible = false; if (_view != null) _view.SetActive(false); }
        private void Update()
        {
            if (!Visible) return;
            if (GameManager.Instance == null || GameManager.Instance.CurrentState != GameState.Hub) { Hide(); return; }
            foreach (var button in _actionButtons) button.interactable = !Busy;
            if (_outputField.text != LastOutput) _outputField.SetTextWithoutNotify(LastOutput);
        }
        private void BuildView()
        {
            if (_view != null) return;
            var canvasObject = new GameObject("Feature lab canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);
            var canvas = canvasObject.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 100;
            var scaler = canvasObject.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720); scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            _view = Panel(canvasObject.transform, "Current SDK feature lab", new Vector2(1230, 680), Vector2.zero, GameColors.DarkBG);
            Text(_view.transform, "horizOn / CURRENT SDK FEATURE LAB", new Vector2(960, 34), new Vector2(-100, 310), 24, GameColors.Orange);
            Text(_view.transform, "Existing signed-in player only. Configure the project first. Every write/retry is explicit; no automatic pages or reward copies.", new Vector2(1180, 30), new Vector2(0, 274), 14, GameColors.Sand);
            var back = Button(_view.transform, "Back to game", new Vector2(150, 38), new Vector2(515, 308), Hide);
            _projectField = Input(_view.transform, "Stable project UUID", new Vector2(760, 38), new Vector2(-205, 226));
            _projectField.onValueChanged.AddListener(value => _project = value);
            _collectionField = Input(_view.transform, "Shared collection", new Vector2(390, 38), new Vector2(395, 226));
            _collectionField.onValueChanged.AddListener(value => _collection = value);
            for (int i = 0; i < Features.Length; i++)
            {
                int index = i;
                var button = Button(_view.transform, Features[i], new Vector2(124, 38), new Vector2(-528 + i * 132, 174), () => SelectFeature(index));
                button.GetComponentInChildren<TMP_Text>().fontSize = 15;
                _featureButtons.Add(button);
            }
            var actions = Panel(_view.transform, "Feature actions", new Vector2(290, 470), new Vector2(-445, -98), new Color32(0x1C, 0x1C, 0x1C, 255));
            actions.AddComponent<RectMask2D>();
            var scroll = actions.AddComponent<ScrollRect>(); scroll.horizontal = false; scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.viewport = actions.GetComponent<RectTransform>();
            var list = new GameObject("Actions", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            list.transform.SetParent(actions.transform, false); _actionList = list.transform;
            var rect = list.GetComponent<RectTransform>(); rect.anchorMin = new Vector2(0, 1); rect.anchorMax = Vector2.one; rect.pivot = new Vector2(.5f, 1); rect.sizeDelta = Vector2.zero;
            var layout = list.GetComponent<VerticalLayoutGroup>(); layout.spacing = 7; layout.padding = new RectOffset(8, 8, 8, 8);
            layout.childControlHeight = true; layout.childControlWidth = true; layout.childForceExpandHeight = false;
            list.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.content = rect;
            Text(_view.transform, "Named arguments (JSON). Keep original IDs/revision/requestId." + "\njson carries the complete object; base64 carries opaque bytes.", new Vector2(860, 48), new Vector2(160, 106), 16, GameColors.Sand);
            _argumentField = LargeField("Named arguments JSON", new Vector2(860, 148), new Vector2(160, 0), false);
            _argumentField.SetTextWithoutNotify(_arguments); _argumentField.onValueChanged.AddListener(value => _arguments = value);
            Button(_view.transform, "Show argument field template", new Vector2(860, 36), new Vector2(160, -100), () =>
            { _arguments = JsonUtility.ToJson(new Arguments(), true); _argumentField.SetTextWithoutNotify(_arguments); });
            _outputField = LargeField("Current result", new Vector2(860, 202), new Vector2(160, -224), true);
            _outputField.SetTextWithoutNotify(LastOutput);
            SeagullMenuScope.Bind(_view, Hide, back);
            SelectFeature(_feature); _view.SetActive(false);
        }
        private TMP_InputField LargeField(string title, Vector2 size, Vector2 position, bool readOnly)
        {
            var field = Input(_view.transform, title, size, position);
            field.characterLimit = 0; field.lineType = TMP_InputField.LineType.MultiLineNewline; field.readOnly = readOnly;
            field.textComponent.richText = false; field.textComponent.alignment = TextAlignmentOptions.TopLeft;
            field.gameObject.AddComponent<RectMask2D>();
            return field;
        }
        private void SelectFeature(int index)
        {
            _feature = index;
            // Old controls leave the active navigation set immediately, before deferred Destroy.
            foreach (Transform child in _actionList) { child.gameObject.SetActive(false); Destroy(child.gameObject); }
            _actionButtons.Clear();
            foreach (string action in Actions[Features[index]])
            {
                string selectedAction = action;
                var button = Button(_actionList, action, new Vector2(270, 38), Vector2.zero, () => { _ = RunUi(Features[_feature], selectedAction); });
                var layout = button.gameObject.AddComponent<LayoutElement>(); layout.preferredHeight = 42;
                _actionButtons.Add(button);
            }
            for (int i = 0; i < _featureButtons.Count; i++)
                _featureButtons[i].GetComponent<Image>().color = i == index ? GameColors.Teal : GameColors.Orange;
            _actionList.parent.GetComponent<ScrollRect>().verticalNormalizedPosition = 1;
        }
        private async Task RunUi(string feature, string action)
        {
            try { LastOutput = await Execute(feature, action, JsonUtility.FromJson<Arguments>(_arguments)); }
            catch (Exception e) { LastOutput = e.Message + " / any saved original remains protected."; }
        }
        private T Recipe<T>() where T : MonoBehaviour
        {
            if (!_recipes.TryGetValue(typeof(T), out var value)) { value = gameObject.AddComponent<T>(); _recipes.Add(typeof(T), value); }
            // The unmodified official samples expose these serialized configuration fields.
            JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(new Binding { projectId = Project(), collection = _collection }), value);
            return (T)value;
        }
        private string Project()
        {
            string key = DurableIntentScope.Key(HorizonApp.Network.GetApiKey(), "binding", "seagull.project.");
            if (string.IsNullOrEmpty(_project) && PlayerPrefs.HasKey(key)) _project = PlayerPrefs.GetString(key);
            if (!Guid.TryParseExact(_project, "D", out var id)) throw new InvalidOperationException("Enter the immutable project UUID from your project settings");
            string canonical = id.ToString("D");
            if (PlayerPrefs.HasKey(key) && PlayerPrefs.GetString(key) != canonical) throw new InvalidOperationException("Project binding differs; original stores cannot be retargeted");
            if (!PlayerPrefs.HasKey(key)) { PlayerPrefs.SetString(key, canonical); PlayerPrefs.Save(); }
            if (PlayerPrefs.GetString(key) != canonical) throw new InvalidOperationException("Project binding could not be saved");
            return _project = canonical;
        }
        private static string CurrentScope() => HorizonManager.Instance?.Online == true
            ? HorizonManager.Instance.Generation + "|" + HorizonManager.Instance.GetUserId() + "|" + HorizonApp.Network.ContextGeneration + "|" + HorizonApp.Network.GetApiKey() + "|" + HorizonApp.Network.GetActiveHost() + "|" + HorizonApp.Network.GetSessionToken() : null;
        private static string Show(object value, string error = null) => error != null ? "Refused / original retained: " + error : value == null ? "No current authoritative snapshot. Read it explicitly." : JsonUtility.ToJson(value, true);
        private static long Integer(string raw)
        {
            if (!long.TryParse(raw, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var value) || value.ToString(CultureInfo.InvariantCulture) != raw)
                throw new InvalidOperationException("Use a canonical decimal string for the original numeric argument");
            return value;
        }
        private static long Revision(Arguments a) => Integer(a.revision);
        public async Task<string> Execute(string feature, string action, Arguments args)
        {
            if (Busy) return "BUSY";
            if (!Actions.TryGetValue(feature, out var choices) || !choices.Contains(action) || args == null) return "INVALID_ACTION";
            string scope = CurrentScope(); if (scope == null) return "SESSION_REQUIRED";
            Busy = true;
            try
            {
                if (_scope != scope) { _scope = scope; _chatChannel = null; foreach (var recipe in _recipes.Values) Destroy(recipe); _recipes.Clear(); }
                string user = HorizonManager.Instance.GetUserId();
                string result = await Dispatch(feature, action, args, user, scope);
                return CurrentScope() == scope ? result : "STALE_RESPONSE / saved original retained";
            }
            finally { Busy = false; }
        }
        private async Task<string> Dispatch(string f, string action, Arguments a, string user, string scope)
        {
            switch (f)
            {
                case "Achievements":
                    if (action == "Read") { var r = await AchievementsManager.Instance.GetAchievements(user); return Show(r.Data, r.IsSuccess ? null : r.ErrorCode); }
                    return await SmallOriginal(f, action, a, user, scope);
                case "Friends":
                    if (action == "Read") { var r = await FriendsManager.Instance.GetFriends(user); return Show(r.Data, r.IsSuccess ? null : r.ErrorCode); }
                    if (action == "Find exact") { var r = await FriendsManager.Instance.FindPlayer(user, a.id); return Show(r.Data, r.IsSuccess ? null : r.ErrorCode); }
                    return await SmallOriginal(f, action, a, user, scope);
                case "Inbox":
                    if (action == "Read") { var r = await InboxManager.Instance.GetInbox(user); return Show(r.Data, r.IsSuccess ? null : r.ErrorCode); }
                    if (action == "Mark read") { var r = await InboxManager.Instance.MarkRead(user, a.id); return Show(r.Data, r.IsSuccess ? null : r.ErrorCode); }
                    if (action == "Remove") { var r = await InboxManager.Instance.Remove(user, a.id); return Show(r.Data, r.IsSuccess ? null : r.ErrorCode); }
                    return await SmallOriginal(f, action, a, user, scope);
                case "Groups":
                    var group = Recipe<GroupsExample>();
                    switch (action)
                    {
                        case "Read": await group.Refresh(); break;
                        case "Review group": await group.ReviewGroup(a.id); break;
                        case "Read configured member page": await group.ReadOneMemberPage(); break;
                        case "Next configured member page": await group.ReadOneMemberPage(true); break;
                        case "Create": await group.SaveCreateAfterReview(a.name, a.description, a.mode, GroupJsonData.FromJson(a.json)); break;
                        case "Update metadata": await group.SaveMetadataAfterReview(a.name, a.description, a.mode, GroupJsonData.FromJson(a.json)); break;
                        case "Retry original": await group.ApplySavedIntent(); break;
                        case "Acknowledge saved success": group.AcknowledgeSavedSuccess(); break;
                        default: await group.SaveActionAfterReview(action, Empty(a.target), Empty(a.requestId), Empty(a.role)); break;
                    }
                    return Show(GroupsManager.Instance.CurrentGroups, GroupsManager.Instance.LastErrorCode) + "\nThe SDK sample logs exact group/member-page results. A no-op requires reviewing current state first.";
                case "Items":
                    var items = Recipe<ItemsExample>(); bool itemOk;
                    switch (action)
                    {
                        case "Read": itemOk = await items.RefreshInventory(); break;
                        case "Read catalog page": itemOk = await items.ReadOneCatalogPage(Empty(a.key)); break;
                        case "Review offers": itemOk = await items.ReviewOffers(); break;
                        case "Purchase reviewed offer": itemOk = await items.SavePurchaseAfterReview(a.key); break;
                        case "Equip reviewed instance": itemOk = await items.SaveEquipAfterReview(a.id, a.flag); break;
                        case "Adjust reviewed property": itemOk = await items.SaveAdjustAfterReview(a.id, a.key, Integer(a.amount)); break;
                        case "Discard reviewed instance": itemOk = await items.SaveDiscardAfterReview(a.id); break;
                        case "Reconcile reviewed mutation": itemOk = items.ReconcileSavedMutationAfterReview(); break;
                        case "Acknowledge saved success": items.AcknowledgeSavedSuccess(); return "Explicit saved-success acknowledgement complete.";
                        default: itemOk = await items.ApplySavedIntent(action.Contains("changed-offer")); break;
                    }
                    return (itemOk ? "Current sample action completed.\n" : "Refused / review current inputs or reconcile original.\n") + Show(ItemsManager.Instance.CurrentInventory, ItemsManager.Instance.LastErrorCode);
                case "Marketplace":
                    var market = Recipe<MarketplaceExample>();
                    switch (action)
                    {
                        case "Read limits": { var r = await market.ReadLimits(); return Show(r.Data, r.IsSuccess ? null : r.ErrorCode); }
                        case "Read rules page": { var r = await market.ReadRulesPage(Empty(a.key)); return Show(r.Data, r.IsSuccess ? null : r.ErrorCode); }
                        case "Read": { var r = await market.ReadMarketplace(); return Show(r.Data, r.IsSuccess ? null : r.ErrorCode); }
                        case "Read listings page": { var r = await market.ReadListings(Empty(a.key)); return Show(r.Data, r.IsSuccess ? null : r.ErrorCode); }
                        case "Read exact listing": { var r = await market.ReadListing(a.id); return Show(r.Data, r.IsSuccess ? null : r.ErrorCode); }
                        case "Read inventory": { var r = await market.ReadInventory(); return Show(r.Data, r.IsSuccess ? null : r.ErrorCode); }
                        case "Review create": return market.ReviewCreate(a.id, Integer(a.price), Integer(a.duration)) ? "Review saved: price " + market.ReviewedPrice + ", fee " + market.ReviewedFee + ". Confirm separately." : market.LastErrorCode;
                        case "Review buy": return market.ReviewBuy(a.id) ? "Buy reviewed. Confirm separately." : market.LastErrorCode;
                        case "Review cancel": return market.ReviewCancel(a.id) ? "Cancellation reviewed. Confirm separately." : market.LastErrorCode;
                        case "Confirm reviewed trade": return await market.ConfirmReviewedTrade() ? "Confirmed; current inventory, balances and market reconciled." : market.LastErrorCode;
                        case "Retry original": return await market.RetryPending() ? "Original confirmed and reconciled." : market.LastErrorCode;
                        case "Look up original": return await market.RecoverOriginalOperation() ? "Original found and reconciled." : market.LastErrorCode;
                        case "Retry return delivery": { var r = await market.RetryReturnDelivery(a.id); return Show(r.Data, r.IsSuccess ? null : r.ErrorCode); }
                        default: return await SmallOriginal("Inbox", "Claim selected original", a, user, scope);
                    }
                case "Matches":
                    var matches = Recipe<MatchesExample>(); bool matchOk;
                    switch (action)
                    {
                        case "Read limits": { var r = await matches.ReadLimits(); return Show(r.Data, r.IsSuccess ? null : r.ErrorCode); }
                        case "Read": matchOk = await matches.ReadLedger(); break;
                        case "Review match": matchOk = await matches.ReadMatch(a.id); break;
                        case "Find open page": { var r = await matches.FindOneOpenPage(2, 8); return Show(r.Data, r.IsSuccess ? null : r.ErrorCode); }
                        case "Create reviewed lobby": matchOk = await matches.SaveCreateAfterLedgerReview(JsonUtility.FromJson<MatchCreateInput>(a.json)); break;
                        case "Submit original ranked result": matchOk = await matches.SubmitOriginalRanked(Integer(a.amount), Convert.FromBase64String(a.base64 ?? "")); break;
                        case "Recover original ranked source": matchOk = await matches.SaveRankedAfterPendingResultReview(a.id); break;
                        case "Retry original": matchOk = await matches.ApplySavedIntent(); break;
                        case "Reconcile original create": matchOk = await matches.ReconcileUnknownCreateAfterLedgerReview(); break;
                        case "Reconcile original action": matchOk = await matches.ReconcileOriginalActionAfterReview(); break;
                        case "Acknowledge saved result": matches.AcknowledgeSavedResult(); return "Explicit matching-result acknowledgement complete.";
                        default: matchOk = await matches.SaveActionAfterMatchReview(action, Convert.FromBase64String(a.base64 ?? ""), a.ids, a.flag); break;
                    }
                    return (matchOk ? "Sample action completed.\n" : "Refused / review current state or reconcile original.\n") + Show(matches.ReviewedMatch) + "\n" + Show(matches.ReviewedLedger);
                case "Shared Objects":
                    var shared = Recipe<SharedObjectsExample>(); bool sharedOk;
                    var input = JsonUtility.FromJson<SharedObjectInput>(a.json ?? "{}");
                    if (input == null) throw new InvalidOperationException("Shared input must be an object");
                    input.data = Convert.FromBase64String(a.base64 ?? "");
                    switch (action)
                    {
                        case "Read collections page": { var r = await shared.ReadOneCollectionPage(Empty(a.key)); return Show(r.Data, r.IsSuccess ? null : r.ErrorCode); }
                        case "Read usage": sharedOk = await shared.ReadUsage(); break;
                        case "Review object": sharedOk = await shared.ReadObject(a.id); break;
                        case "Create reviewed object": sharedOk = await shared.SaveCreateAfterUsageReview(input); break;
                        case "Update reviewed object": sharedOk = await shared.SaveUpdateAfterObjectReview(input); break;
                        case "Delete reviewed object": sharedOk = await shared.SaveDeleteAfterSeparateConfirmation(); break;
                        case "Read rectangle page": { var r = await shared.ReadRectangle(0, 100, 0, 100, Empty(a.key)); return Show(r.Data, r.IsSuccess ? null : r.ErrorCode); }
                        case "Find opponent sample": { var r = await shared.FindOpponent(0, 100); return Show(r.Data, r.IsSuccess ? null : r.ErrorCode); }
                        case "Retry original": sharedOk = await shared.ApplySavedIntent(); break;
                        case "Reconcile original object": sharedOk = await shared.ReconcileOriginalObjectAfterReview(); break;
                        case "Reconcile original create": sharedOk = await shared.ReconcileCreateAfterOwnerReview(); break;
                        default: shared.AcknowledgeSavedResult(); return "Explicit matching-result acknowledgement complete.";
                    }
                    return sharedOk ? "Sample action completed; inspect the exact SDK log/current object. No local object synthesis." : "Read/review/reconcile required: " + SharedObjectsManager.Instance.LastErrorCode;
                case "Chat":
                    if (action == "Open project channel")
                    { var r = await ChatManager.Instance.OpenChannel(user, "project"); if (r.IsSuccess && CurrentScope() == scope) { _chatChannel = r.Data.channel.id; ChatManager.Instance.WatchChannel(user, _chatChannel); } return Show(r.Data, r.IsSuccess ? null : r.ErrorCode); }
                    if (action == "Read current channel") { var r = await ChatManager.Instance.SyncChannel(user, Empty(a.channelId) ?? _chatChannel); return Show(r.Data, r.IsSuccess ? null : r.ErrorCode); }
                    if (action == "Read local history") { var r = ChatManager.Instance.ReadLocalHistory(Empty(a.channelId) ?? _chatChannel); return Show(r.Data, r.IsSuccess ? null : r.ErrorCode); }
                    if (action == "Send original text") { a.channelId = Empty(a.channelId) ?? _chatChannel; a.operationId = Guid.NewGuid().ToString("D"); a.issuedAt = ChatManager.Instance.GetServerTime(); }
                    return await SmallOriginal(f, action, a, user, scope);
            }
            return "INVALID_ACTION";
        }
        [Serializable] private sealed class Original { public int format = 1; public string action; public Arguments args; }
        private static Arguments StoredArguments(Arguments value, bool encode)
        {
            var copy = new Arguments { flag = value.flag, ids = value.ids?.ToArray() };
            // JsonUtility's string bridge is not the original store for embedded NUL. Store ASCII
            // tokens for this fixed DTO, then restore counted UTF-8 before the SDK sees the tuple.
            foreach (var field in typeof(Arguments).GetFields())
            {
                if (field.FieldType != typeof(string)) continue;
                string text = (string)field.GetValue(value);
                string result = encode ? text == null ? "n" : "b:" + Convert.ToBase64String(new System.Text.UTF8Encoding(false, true).GetBytes(text))
                    : text == "n" ? null : text != null && text.StartsWith("b:", StringComparison.Ordinal) ? new System.Text.UTF8Encoding(false, true).GetString(Convert.FromBase64String(text.Substring(2))) : throw new InvalidOperationException("Unknown original encoding");
                field.SetValue(copy, result);
            }
            return copy;
        }
        private async Task<string> SmallOriginal(string feature, string action, Arguments args, string user, string scope)
        {
            using var store = new FeatureOriginalStore(HorizonApp.Network.GetApiKey(), user, feature);
            string raw = store.Read();
            if (action == "Inspect original")
            {
                if (raw == null) return "No saved original.";
                var saved = JsonUtility.FromJson<Original>(raw);
                if (saved?.format != 1 || saved.args == null) throw new InvalidOperationException("Unknown original format");
                return "Exact stored representation:\n" + raw + "\nDecoded display preview (never sent):\n" + JsonUtility.ToJson(new Original { action = saved.action, args = StoredArguments(saved.args, false) }, true);
            }
            if (action == "Acknowledge newer revision")
            {
                var saved = raw == null ? null : JsonUtility.FromJson<Original>(raw); var current = FriendsManager.Instance.CurrentFriends;
                if (saved == null || current?.userId != user || saved.format != 1 || current.revision <= Revision(StoredArguments(saved.args, false))) return "Read a permanently newer authoritative Friends revision first.";
                store.Confirm(raw); return "Original cannot apply at the old revision and was acknowledged.";
            }
            if (action != "Retry original" && action != "Look up original")
            {
                if (feature == "Inbox" && InboxManager.Instance.CurrentInbox?.entries.Any(e => e.id == args.id && e.CanClaim) != true) return "Select a current claimable Inbox entry first.";
                if (feature == "Friends" && FriendsManager.Instance.CurrentFriends?.revision != Revision(args)) return "Read and select the original Friends revision first.";
                raw = JsonUtility.ToJson(new Original { action = action, args = StoredArguments(args, true) }); store.SaveNew(raw);
            }
            if (raw == null) return "NO_PENDING_ORIGINAL";
            var original = JsonUtility.FromJson<Original>(raw);
            if (original?.args == null || original.format != 1) throw new InvalidOperationException("Unknown original; recover without replacement");
            var a = StoredArguments(original.args, false); string code; object data; bool ok;
            switch (feature)
            {
                case "Inbox": var claim = await InboxManager.Instance.Claim(user, a.id); ok = claim.IsSuccess; code = claim.ErrorCode; data = claim.Data; break;
                case "Achievements":
                    if (original.action == "Deliver selected legacy reward") { var r = await AchievementsManager.Instance.DeliverRewards(user, new[] { a.key }); ok = r.IsSuccess; code = r.ErrorCode; data = r.Data; }
                    else { var r = await AchievementsManager.Instance.Unlock(user, a.key); ok = r.IsSuccess; code = r.ErrorCode; data = r.Data; } break;
                case "Friends":
                    FriendsResult friends;
                    switch (original.action)
                    {
                        case "request": friends = await FriendsManager.Instance.SendRequest(user, a.target, Revision(a)); break;
                        case "accept": friends = await FriendsManager.Instance.AcceptRequest(user, a.target, Revision(a), a.requestId); break;
                        case "decline": friends = await FriendsManager.Instance.DeclineRequest(user, a.target, Revision(a), a.requestId); break;
                        case "cancel": friends = await FriendsManager.Instance.CancelRequest(user, a.target, Revision(a), a.requestId); break;
                        case "remove": friends = await FriendsManager.Instance.RemoveFriend(user, a.target, Revision(a)); break;
                        case "block": friends = await FriendsManager.Instance.BlockPlayer(user, a.target, Revision(a)); break;
                        case "unblock": friends = await FriendsManager.Instance.UnblockPlayer(user, a.target, Revision(a)); break;
                        default: throw new InvalidOperationException("Unknown saved Friends action");
                    }
                    ok = friends.IsSuccess; code = friends.ErrorCode; data = friends.Data; break;
                case "Chat":
                    if (action == "Look up original") { var r = await ChatManager.Instance.GetOperation(user, a.operationId); ok = r.IsSuccess && r.Data?.receipt?.channelId == a.channelId && r.Data.receipt.issuedAt == a.issuedAt; code = r.ErrorCode ?? "ORIGINAL_NOT_CONFIRMED"; data = r.Data; }
                    else { var r = await ChatManager.Instance.SendMessage(user, a.channelId, a.operationId, a.issuedAt, a.text); ok = r.IsSuccess; code = r.ErrorCode; data = r.Data; } break;
                default: throw new InvalidOperationException("Unknown saved feature");
            }
            if (CurrentScope() != scope) return "STALE_RESPONSE / original retained";
            if (ok) store.Confirm(raw);
            return Show(data, ok ? null : code);
        }
        private static string Empty(string value) => string.IsNullOrEmpty(value) ? null : value;
        private void OnDestroy() { if (Instance == this) Instance = null; }
    }
}
