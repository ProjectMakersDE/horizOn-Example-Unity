using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using TMPro;
using PM.horizOn.Cloud.Objects.Network.Responses;
namespace SeagullStorm
{
    /// <summary>Readable adaptive title/hub with optional real SDK demos, built from the same brand palette.</summary>
    public class SeagullInterface : MonoBehaviour
    {
        private Canvas _canvas;
        private GameObject _home, _demo;
        private SeagullPlayerValuesDemo _valuesDemo;
        private TMP_Text _stats, _connection, _output, _runStatus;
        private TMP_InputField _name, _email, _password, _code, _feedback, _recipient, _template;
        private Button _play, _demoButton;
        private string _page = "top";
        private List<LeaderboardBoardResponse> _boards;
        private int _boardIndex;
        private bool _busy;
        private bool _title;
        private GameState? _focusedState;
        private PlayerProfileResponse _profile;
        private int _avatarIndex;
        private string _language = "en";
        private readonly List<TMP_Text> _playLabels = new List<TMP_Text>();
        private static TMP_FontAsset Font => Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF") ?? TMP_Settings.defaultFontAsset;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install() => SceneManager.sceneLoaded += (scene, _) =>
        {
            if (scene.name == "TitleScene" || scene.name == "GameScene")
                new GameObject("Seagull interface").AddComponent<SeagullInterface>();
        };
        private void Start()
        {
            _title = SceneManager.GetActiveScene().name == "TitleScene";
            if (HorizonManager.Instance == null) new GameObject("[HorizonManager]").AddComponent<HorizonManager>();
            if (_title)
            {
                foreach (var c in FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None)) DisableLegacyCanvas(c);
                foreach (var c in FindObjectsByType<TitleScreenController>(FindObjectsSortMode.None)) c.enabled = false;
            }
            else
            {
                var hub = GameObject.Find("HubCanvas");
                if (hub != null && hub.TryGetComponent<Canvas>(out var old)) DisableLegacyCanvas(old);
                Restyle();
            }
            var go = new GameObject("horizOn UI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            go.transform.SetParent(transform);
            _canvas = go.GetComponent<Canvas>(); _canvas.renderMode = RenderMode.ScreenSpaceOverlay; _canvas.sortingOrder = 50;
            var scaler = go.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720); scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            _home = Panel(go.transform, "Home", new Vector2(1280,720), Vector2.zero, GameColors.DarkBG);
            var homeRect = _home.GetComponent<RectTransform>();
            homeRect.anchorMin = Vector2.zero; homeRect.anchorMax = Vector2.one; homeRect.sizeDelta = Vector2.zero;
            _home.AddComponent<CanvasGroup>();
            BuildHome();
            if (!_title)
            {
                BuildDemo();
                _runStatus = Text(_canvas.transform,"",new Vector2(970,34),new Vector2(-110,-291),16,GameColors.LightText);
                _runStatus.alignment = TextAlignmentOptions.Center;
                _runStatus.rectTransform.anchorMin = _runStatus.rectTransform.anchorMax = new Vector2(.5f, 0);
                _runStatus.rectTransform.anchoredPosition = new Vector2(-110, 69);
            }
            FocusControl(_play);
        }
        private void BuildHome()
        {
            Text(_home.transform,"horizOn  /  EXAMPLE GAMES",new Vector2(430,30),new Vector2(-390,302),16,GameColors.Orange);
            Text(_home.transform,"SEAGULL\nSTORM",new Vector2(580,170),new Vector2(-310,166),64,GameColors.LightText, FontStyles.Bold);
            Text(_home.transform,"Small island. Big storm. One determined seagull.",new Vector2(550,40),new Vector2(-325,50),22,GameColors.Sand);
            Text(_home.transform,"Move with WASD or arrows. Attacks fire automatically.\nCollect shells, choose upgrades, survive 3 minutes\nand defeat the octopus. Escape pauses the run.",new Vector2(555,110),new Vector2(-322,-43),20,GameColors.LightText);
            var art = SeagullArt.Load("seagull_hero");
            if (art != null)
            {
                var frame = Panel(_home.transform,"Hero card",new Vector2(405,390),new Vector2(382,100),new Color32(0x1C,0x1C,0x1C,255));
                var image = Panel(frame.transform,"Hero",new Vector2(335,335),new Vector2(0,12),Color.white).GetComponent<Image>();
                image.sprite = art; image.preserveAspect = true;
                Text(frame.transform,"YOUR NEXT RUN STARTS HERE",new Vector2(370,30),new Vector2(0,-163),15,GameColors.Teal);
            }
            _connection = Text(_home.transform,"",new Vector2(620,30),new Vector2(-290,-310),15,GameColors.Teal);
            if (_title)
            {
                _play = Button(_home.transform,"PLAY OFFLINE",new Vector2(260,58),new Vector2(-470,-155),()=>{HorizonManager.Instance.EnterPractice();SceneManager.LoadScene("GameScene");});
                _playLabels.Add(_play.GetComponentInChildren<TMP_Text>());
                _name = Input(_home.transform,"Pilot name",new Vector2(235,44),new Vector2(-187,-155));
                Button(_home.transform,"Guest sign-in",new Vector2(235,44),new Vector2(-187,-211),()=>Run(SignInGuest));
                _email = Input(_home.transform,"Email",new Vector2(260,42),new Vector2(206,-155));
                _password = Input(_home.transform,"Password",new Vector2(260,42),new Vector2(206,-205)); _password.contentType = TMP_InputField.ContentType.Password;
                Button(_home.transform,"Sign in",new Vector2(124,42),new Vector2(138,-255),()=>Run(SignInEmail));
                Button(_home.transform,"Register",new Vector2(124,42),new Vector2(274,-255),()=>Run(SignUpEmail));
                _output = Text(_home.transform,"Online features need your own imported SDK config.\nGoogle needs a platform plugin; Apple is available when configured.",new Vector2(240,135),new Vector2(470,-202),14,GameColors.Sand);
                Button(_home.transform,"Apple",new Vector2(122,36),new Vector2(-536,-260),()=>Run(SignInApple));
                Button(_home.transform,"Restore session",new Vector2(185,36),new Vector2(-371,-260),()=>Run(RestoreSession));
            }
            else
            {
                _play = Button(_home.transform,"START RUN",new Vector2(280,62),new Vector2(-460,-157),()=>GameManager.Instance.StartRun());
                _playLabels.Add(_play.GetComponentInChildren<TMP_Text>());
                _demoButton = Button(_home.transform,"SDK DEMOS",new Vector2(240,50),new Vector2(-174,-157),ShowDemo);
                _stats = Text(_home.transform,"",new Vector2(600,34),new Vector2(-300,-221),20,GameColors.Sand);
                string[] ids={"speed","damage","hp","magnet"}; string[] labels={"SPEED","DAMAGE","HEALTH","MAGNET"};
                for(int i=0;i<4;i++)
                {
                    string id=ids[i]; var b=Button(_home.transform,labels[i],new Vector2(150,44),new Vector2(-522+i*165,-269),()=>GameManager.Instance.TryPurchaseUpgrade(id));
                    b.gameObject.AddComponent<UpgradePreview>().Id=id;
                }
                Button(_home.transform,"Sign out",new Vector2(146,40),new Vector2(517,-310),()=>GameManager.Instance.SignOutAndReturn());
            }
        }
        private void BuildDemo()
        {
            _demo=Panel(_canvas.transform,"SDK demos",new Vector2(1210,630),Vector2.zero,new Color32(0x1C,0x1C,0x1C,255));
            Text(_demo.transform,"horizOn  /  SDK DEMOS",new Vector2(780,40),new Vector2(-166,265),26,GameColors.Orange,FontStyles.Bold);
            Button(_demo.transform,"CLOSE",new Vector2(125,40),new Vector2(505,265),ShowHome);
            string[] buttons={"Top 10","Around me","My rank","Next board","News","Cloud save","Profile","Avatar","Apply avatar","EN / DE","Server state","Email demo","Player values"};
            Action[] actions={ ()=>{_page="top";Run(ShowLeaderboard);},()=>{_page="around";Run(ShowLeaderboard);},()=>Run(ShowRank),()=>Run(NextBoard),()=>Run(ShowNews),()=>Run(SyncSave),()=>Run(ShowProfile),()=>Run(NextAvatar),()=>Run(ApplyAvatar),()=>Run(LoadLocalization),()=>Run(ShowState),()=>ShowEmail(),ShowPlayerValues };
            for(int i=0;i<buttons.Length;i++) Button(_demo.transform,buttons[i],new Vector2(148,42),new Vector2(-502+(i%7)*166,204-(i/7)*54),actions[i]);
            _output=Text(_demo.transform,"Sign in to use online demos. Offline progress stays on this device.",new Vector2(1090,230),new Vector2(0,-11),18,GameColors.LightText);
            _output.alignment=TextAlignmentOptions.TopLeft; _output.richText=false;
            _code=Input(_demo.transform,"Gift code",new Vector2(430,44),new Vector2(-300,-186));
            Button(_demo.transform,"Redeem",new Vector2(150,44),new Vector2(-1,-186),()=>Run(Redeem));
            _feedback=Input(_demo.transform,"Feedback message",new Vector2(430,44),new Vector2(-300,-243));
            Button(_demo.transform,"Send feedback",new Vector2(150,44),new Vector2(-1,-243),()=>Run(SendFeedback));
            Text(_demo.transform,"Validated Actions: enable validated_runs_enabled in Remote Config.\nNo ticket means practice only. Input logs are review evidence.\nNo authoritative replay is claimed. Avatar IDs come from your catalog.",new Vector2(420,100),new Vector2(331,-220),16,GameColors.Sand);
            _demo.SetActive(false);
        }
        private void ShowPlayerValues()
        {
            _valuesDemo ??= new SeagullPlayerValuesDemo(_canvas.transform, () =>
            {
                _demo.SetActive(true);
                FocusControl(_demo.transform.Find("Player values")?.GetComponent<Button>());
            });
            _demo.SetActive(false);
            _valuesDemo.Show();
        }
        private void ShowEmail()
        {
            _output.text = "EMAIL SENDING / sends a real template to the recipient you enter.\nConfigure the template in your dashboard. Never used automatically.";
            if (_recipient != null) return;
            _recipient = Input(_demo.transform,"Recipient user ID",new Vector2(430,44),new Vector2(-300,-86));
            _template = Input(_demo.transform,"Template slug",new Vector2(280,44),new Vector2(65,-86));
            Button(_demo.transform,"SEND EMAIL",new Vector2(170,44),new Vector2(316,-86),()=>Run(SendEmail));
        }
        private async Task SendEmail()
        {
            if (!Online()) return;
            if (string.IsNullOrWhiteSpace(_recipient.text) || string.IsNullOrWhiteSpace(_template.text)) { _output.text="Enter recipient user ID and template slug."; return; }
            var result=await HorizonManager.Instance.SendEmail(_recipient.text.Trim(),_template.text.Trim(),_language);
            _output.text=result!=null?"Email queued. Check the Email Sending dashboard for delivery status.":"Email request failed. Check recipient, template and project configuration.";
        }

        private async void Run(Func<Task> action)
        {
            if(_busy)return; _busy=true;
            try { await action(); } catch(Exception ex) { if (_output != null) _output.text="Operation failed: "+ex.Message; }
            finally { _busy=false; }
        }
        private bool Online()
        {
            if(HorizonManager.Instance.Online)return true;
            _output.text="Online service unavailable. Import your SDK config and sign in.\nOffline practice, coins and upgrades remain playable.";return false;
        }
        private async Task CompleteAuth(Func<Task<bool>> auth, string failure)
        {
            if (!HorizonManager.Instance.Configured) { Online(); return; }
            bool success = await auth();
            if (this == null || !isActiveAndEnabled || SceneManager.GetActiveScene().name != "TitleScene") return;
            if (success) SceneManager.LoadScene("GameScene");
            else _output.text = failure;
        }
        private Task RestoreSession() => CompleteAuth(() => HorizonManager.Instance.RestoreSession(), "No valid saved session; sign in or play offline.");
        private Task SignInGuest() => CompleteAuth(() => HorizonManager.Instance.SignUpGuest(string.IsNullOrWhiteSpace(_name.text) ? "Seagull" : _name.text.Trim()), "Guest sign-in failed. Check configuration and connection.");
        private Task SignInEmail() => CompleteAuth(() => HorizonManager.Instance.SignInEmail(_email.text.Trim(), _password.text), "Sign-in failed. Check email, password and connection.");
        private Task SignUpEmail() => CompleteAuth(() => HorizonManager.Instance.SignUpEmail(_email.text.Trim(), _password.text, _name.text.Trim()), "Registration failed. Check name, email and password.");
        private Task SignInApple() => CompleteAuth(() => HorizonManager.Instance.SignInWithApple(), "Apple sign-in needs platform OAuth configuration.");
        private async Task ShowLeaderboard()
        {
            if(!Online())return; _output.text="Loading leaderboard…";
            var h=HorizonManager.Instance;var entries=_page=="around"?await h.GetAround():await h.GetTop10();
            if(entries==null){_output.text="Leaderboard unavailable for board "+h.ActiveBoard;return;}
            var text=new StringBuilder($"{h.ActiveBoard}  /  {(_page=="around"?"AROUND YOU":"TOP 10")}\n");
            foreach(var e in entries)text.AppendLine($"#{e.position}   {e.username}   ·   {e.score:N0}"+(e.profile?.HasAvatar==true?"   ["+e.profile.avatarId+"]":""));
            if(entries.Count==0)text.AppendLine("No scores yet. Complete an online run to join this board.");_output.text=text.ToString();
        }
        private async Task ShowRank(){if(!Online())return;var rank=await HorizonManager.Instance.GetRank();_output.text=rank!=null?$"{HorizonManager.Instance.ActiveBoard} / YOUR RANK\n#{rank.position}  {rank.username}  ·  {rank.score:N0}":"No rank available. Finish a run on this board.";}
        private async Task NextBoard(){if(!Online())return;_boards??=await HorizonManager.Instance.ListBoards();if(_boards==null||_boards.Count==0){_output.text="No boards available. Create one in your dashboard.";return;} _boardIndex=(_boardIndex+1)%_boards.Count;var board=_boards[_boardIndex];HorizonManager.Instance.ActiveBoard=board.key;if(board.validatedOnly)GameManager.Instance.Config.ValidatedRunsEnabled=true;await ShowLeaderboard();_output.text+="\nBoard: "+board.name+(board.validatedOnly?" · validated submissions only":" · standard scores");}
        private async Task ShowNews(){if(!Online())return;var items=await HorizonManager.Instance.LoadNews();var s=new StringBuilder("NEWS\n");if(items!=null)foreach(var n in items)s.AppendLine(n.title+"  /  "+n.releaseDate);_output.text=items==null?"News request failed.":items.Count==0?"No news published in your project.":s.ToString();}
        private async Task SyncSave(){if(!Online()||!GameManager.Instance.ProgressLoaded||GameManager.Instance.RunSubmissionPending)return;if(!HorizonManager.Instance.CloudReady){_output.text="Cloud save load has not succeeded. Sign out and reconnect to safely reload progress before writing; existing cloud data is protected.";return;}bool ok=await HorizonManager.Instance.SaveCloudData(JsonUtility.ToJson(GameManager.Instance.Save));var json=ok?await HorizonManager.Instance.LoadCloudData():null;_output.text=json!=null?"Cloud Save saved and loaded for this signed-in session.\n"+json:"Cloud Save failed. Local progress is still retained.";}
        private async Task ShowProfile(){if(!Online())return;_profile=await HorizonManager.Instance.GetProfile();_output.text=_profile!=null?"PLAYER PROFILE\nAvatar: "+_profile.profile.avatarId+"\nFrame: "+_profile.profile.frameId+"\nBadges: "+string.Join(", ",_profile.profile.badges)+"\nCatalog entries: "+_profile.cosmetics.Length:"Profile unavailable: "+HorizonManager.Instance.ProfileError;}
        private async Task NextAvatar(){if(_profile==null)await ShowProfile();if(_profile==null)return;var available=_profile.GetCosmetics("avatar").FindAll(c=>c.available);if(available.Count==0){_output.text="No available avatars. Add cosmetics to your dashboard catalog.";return;}_avatarIndex=(_avatarIndex+1)%available.Count;_output.text="Selected avatar: "+available[_avatarIndex].id+"\nChoose Apply avatar to save it.";}
        private async Task ApplyAvatar(){if(!Online())return;if(_profile==null)await ShowProfile();if(_profile==null)return;var available=_profile.GetCosmetics("avatar").FindAll(c=>c.available);if(available.Count==0){_output.text="No available avatars in your catalog.";return;}var updated=await HorizonManager.Instance.SetProfile(available[_avatarIndex%available.Count].id,_profile.profile.frameId,_profile.profile.badges);_output.text=updated!=null?"Avatar saved; existing frame and badges retained.":"Profile update failed: "+HorizonManager.Instance.ProfileError;_profile=updated??_profile;}
        private async Task LoadLocalization(){if(!Online())return;_language=_language=="en"?"de":"en";var result=await HorizonManager.Instance.Localize(_language);if(result==null){_output.text="Localization unavailable.";return;}foreach(var label in _playLabels)if(result.TryGetValue("ui.play",out var translated)&&!string.IsNullOrWhiteSpace(translated))label.text=translated;_output.text=$"LOCALIZATION / {_language}\nLoaded {result.Count} strings. ui.play updates the Start Run button when provided.\n";int count=0;foreach(var pair in result){_output.text+=pair.Key+" = "+pair.Value+"\n";if(++count>=5)break;}}
        private async Task ShowState(){if(!Online())return;var state=await HorizonManager.Instance.GetValidatedState();_output.text=state!=null?"SERVER-OWNED STATE\n"+JsonUtility.ToJson(state):"State unavailable. Configure Validated Actions rules in your dashboard.";}
        private async Task Redeem(){if(!Online()||!GameManager.Instance.ProgressLoaded||GameManager.Instance.RunSubmissionPending)return;var gm=GameManager.Instance;int generation=HorizonManager.Instance.Generation;if(string.IsNullOrWhiteSpace(_code.text)){_output.text="Enter a gift code.";return;}var code=_code.text.Trim();bool? valid=await HorizonManager.Instance.ValidateGiftCode(code);if(valid!=true){_output.text=valid==null?"Gift validation unavailable.":"Gift code is invalid or expired.";return;}var reward=await HorizonManager.Instance.RedeemGiftCode(code);if(reward==null){if(_output!=null)_output.text="Gift redemption failed.";return;}if(this==null||GameManager.Instance!=gm||HorizonManager.Instance.Generation!=generation)return;if(!string.IsNullOrEmpty(reward.giftData)){var value=JsonUtility.FromJson<GiftReward>(reward.giftData);GameManager.Instance.Save.coins+=Mathf.Max(0,value?.coins??0);}GameManager.Instance.NotifyProgressChanged();await HorizonManager.Instance.SaveCloudData(JsonUtility.ToJson(GameManager.Instance.Save));_output.text="Gift redeemed. Rewards saved.";}
        [Serializable]private class GiftReward{public int coins;}
        private async Task SendFeedback(){if(!Online())return;if(string.IsNullOrWhiteSpace(_feedback.text)){_output.text="Enter feedback before sending.";return;}_output.text=await HorizonManager.Instance.SubmitFeedback("Seagull Storm feedback",_feedback.text.Trim(),"GENERAL")?"Feedback sent. Thank you!":"Feedback failed to send.";}
        private void Update()
        {
            if(_home==null)return;
            _connection.text=HorizonManager.Instance.ConnectionStatus;
            if(!_title&&GameManager.Instance!=null)
            {
                var state = GameManager.Instance.CurrentState;
                bool hub=state==GameState.Hub;_home.SetActive(hub);if(!hub){_demo.SetActive(false);_valuesDemo?.Hide();}
                bool enteredState = _focusedState != state;
                _focusedState = state;
                bool wasReady = _play.interactable;
                _play.interactable = GameManager.Instance.ProgressLoaded && !GameManager.Instance.RunSubmissionPending && !GameManager.Instance.IsStartingRun;
                bool modal = _demo.activeSelf || (_valuesDemo?.IsVisible == true);
                _home.GetComponent<CanvasGroup>().interactable = !modal;
                // Loading may clear selection when Start becomes disabled. Focus only when
                // entering the ready hub, leaving all later user navigation untouched.
                if (hub && !modal && _play.interactable && (enteredState || !wasReady)) FocusControl(_play);
                else if (enteredState && state == GameState.Run) FocusControl(null);
                _runStatus.gameObject.SetActive(!hub);
                var gm=GameManager.Instance;
                if(gm.CurrentState==GameState.GameOver)
                {
                    _runStatus.text=gm.RunSubmissionPending?"Finishing run…":gm.SubmissionStatus;
                    if(gm.LastValidatedOutcome?.Accepted==true)_runStatus.text="Server accepted • Rank #"+gm.LastValidatedOutcome.Rank+" • "+ValidatedRunService.EvidenceStatus;
                    var header=GameObject.Find("GameOverCanvas/Header")?.GetComponent<TMP_Text>();
                    if(header!=null)header.text=gm.Victory?"ISLAND DEFENDED!":"THE STORM GOT YOU";
                }
                else _runStatus.text=gm.RunMode+"  •  "+string.Join(" / ",gm.RunState.activeWeapons)+"  •  WASD / arrows to move · Esc to pause";
                var save=GameManager.Instance.Save;_stats.text=$"{save.coins:N0} coins  ·  Best {save.highscore:N0}  ·  {save.totalRuns} runs";
            }
            if(_busy)_connection.text="Request in progress…";
        }
        private void ShowDemo()
        {
            _home.GetComponent<CanvasGroup>().interactable = false;
            _demo.SetActive(true);
            FocusControl(_demo.transform.Find("Top 10")?.GetComponent<Button>());
            _ = ShowLeaderboard();
        }
        private void ShowHome()
        {
            _demo.SetActive(false);
            _home.GetComponent<CanvasGroup>().interactable = true;
            FocusControl(_play.IsInteractable() ? _play : _demoButton);
        }
        public static void FocusControl(Button button)
        {
            var events = EventSystem.current;
            if (events == null) return;
            events.firstSelectedGameObject = button != null ? button.gameObject : null;
            events.SetSelectedGameObject(events.firstSelectedGameObject);
        }
        private static void DisableLegacyCanvas(Canvas canvas)
        {
            canvas.enabled = false;
            var raycaster = canvas.GetComponent<GraphicRaycaster>();
            if (raycaster != null) raycaster.enabled = false;
            var group = canvas.GetComponent<CanvasGroup>() ?? canvas.gameObject.AddComponent<CanvasGroup>();
            group.interactable = false; group.blocksRaycasts = false;
        }
        private static void Restyle()
        {
            foreach (var canvas in FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                StyleLegacySubtree(canvas.gameObject);
                var scaler = canvas.GetComponent<CanvasScaler>();
                if (scaler != null) scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
                if (canvas.name == "RunHUDCanvas")
                {
                    HudPlate(canvas.transform, "HUD top plate", 106, true);
                    HudPlate(canvas.transform, "HUD bottom plate", 94, false);
                }
            }
        }
        public static void StyleLegacySubtree(GameObject root)
        {
            foreach (var t in root.GetComponentsInChildren<TMP_Text>(true))
            {
                t.font = Font; t.color = GameColors.LightText;
                t.enableAutoSizing = true; t.fontSizeMin = 14; t.fontSizeMax = Mathf.Max(18, t.fontSize);
            }
            foreach (var image in root.GetComponentsInChildren<Image>(true))
            {
                if (image.name == "Background" || image.name.EndsWith("Panel"))
                { image.sprite = null; image.type = Image.Type.Simple; image.color = GameColors.DarkBG; }
            }
            foreach (var button in root.GetComponentsInChildren<Button>(true))
            {
                if (button.GetComponent<SeagullSelectionOutline>() == null) button.gameObject.AddComponent<SeagullSelectionOutline>();
                var colors = button.colors;
                colors.normalColor = Color.white; colors.highlightedColor = GameColors.Sand;
                colors.pressedColor = GameColors.Teal; colors.selectedColor = Color.white;
                colors.disabledColor = new Color(.35f, .35f, .35f); button.colors = colors;
                button.spriteState = default; button.transition = Selectable.Transition.ColorTint;
                if (button.targetGraphic is Image image)
                { image.sprite = null; image.overrideSprite = null; image.type = Image.Type.Simple; image.color = GameColors.Orange; }
                foreach (var t in button.GetComponentsInChildren<TMP_Text>(true))
                { t.color = GameColors.DarkText; t.fontSizeMax = 26; t.fontStyle = FontStyles.Bold; }
            }
        }
        private static void HudPlate(Transform parent, string name, float height, bool top)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false); go.transform.SetAsFirstSibling();
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, top ? 1 : 0); rect.anchorMax = new Vector2(1, top ? 1 : 0);
            rect.pivot = new Vector2(.5f, top ? 1 : 0);
            var scaler = parent.GetComponent<CanvasScaler>();
            float unitsPerPixel = scaler != null ? scaler.referenceResolution.y / 720f : 1f;
            rect.sizeDelta = new Vector2(0, height * unitsPerPixel);
            var plate = go.GetComponent<Image>(); plate.color = GameColors.DarkBG; plate.raycastTarget = false;
        }
        public static GameObject Panel(Transform parent,string name,Vector2 size,Vector2 pos,Color color)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(Image));go.transform.SetParent(parent,false);var r=go.GetComponent<RectTransform>();r.anchorMin=r.anchorMax=new Vector2(.5f,.5f);r.sizeDelta=size;r.anchoredPosition=pos;go.GetComponent<Image>().color=color;return go;
        }
        public static TMP_Text Text(Transform parent,string value,Vector2 size,Vector2 pos,float fontSize,Color color,FontStyles style=FontStyles.Normal)
        {
            var go=new GameObject("Text",typeof(RectTransform),typeof(TextMeshProUGUI));go.transform.SetParent(parent,false);var r=go.GetComponent<RectTransform>();r.anchorMin=r.anchorMax=new Vector2(.5f,.5f);r.sizeDelta=size;r.anchoredPosition=pos;var t=go.GetComponent<TMP_Text>();t.font=Font;t.text=value;t.fontSize=fontSize;t.fontStyle=style;t.color=color;t.alignment=TextAlignmentOptions.Left;t.raycastTarget=false;return t;
        }
        internal static Button Button(Transform p,string title,Vector2 size,Vector2 pos,Action action)
        {
            var go=Panel(p,title,size,pos,GameColors.Orange);var b=go.AddComponent<Button>();go.AddComponent<SeagullSelectionOutline>();b.targetGraphic=go.GetComponent<Image>();var text=Text(go.transform,title,size-Vector2.one*8,Vector2.zero,18,GameColors.DarkBG,FontStyles.Bold);text.alignment=TextAlignmentOptions.Center;b.onClick.AddListener(()=>action());var colors=b.colors;colors.highlightedColor=GameColors.Sand;colors.pressedColor=GameColors.Teal;colors.disabledColor=new Color(.35f,.35f,.35f);b.colors=colors;return b;
        }
        private static TMP_InputField Input(Transform p,string placeholder,Vector2 size,Vector2 pos)
        {
            var go=Panel(p,placeholder,size,pos,new Color32(0x31,0x31,0x31,255));var text=Text(go.transform,"",size-new Vector2(20,4),Vector2.zero,18,GameColors.LightText);var hint=Text(go.transform,placeholder,size-new Vector2(20,4),Vector2.zero,17,GameColors.Sand);var input=go.AddComponent<TMP_InputField>();input.textViewport=go.GetComponent<RectTransform>();input.textComponent=(TextMeshProUGUI)text;input.placeholder=hint;input.characterLimit=256;return input;
        }
    }
    public class SeagullSelectionOutline : MonoBehaviour, ISelectHandler, IDeselectHandler
    {
        private Outline _outline;
        private void Awake()
        {
            _outline = gameObject.AddComponent<Outline>();
            _outline.effectColor = GameColors.LightText;
            var scaler = GetComponentInParent<CanvasScaler>();
            float units = scaler != null ? scaler.referenceResolution.y / 720f : 1f;
            _outline.effectDistance = new Vector2(3, 3) * units;
            _outline.enabled = false;
        }
        private void OnEnable() { if (_outline != null) _outline.enabled = EventSystem.current?.currentSelectedGameObject == gameObject; }
        public void OnSelect(BaseEventData data) { if (_outline != null) _outline.enabled = true; }
        public void OnDeselect(BaseEventData data) { if (_outline != null) _outline.enabled = false; }
    }
    public class UpgradePreview:MonoBehaviour
    {
        public string Id;
        private void Update()
        {
            var gm=GameManager.Instance;if(gm==null)return;int level=Id=="speed"?gm.Save.upgrades.speed:Id=="damage"?gm.Save.upgrades.damage:Id=="hp"?gm.Save.upgrades.hp:gm.Save.upgrades.magnet;
            var costs=Id=="speed"?gm.Config.UpgradeSpeedCosts:Id=="damage"?gm.Config.UpgradeDamageCosts:Id=="hp"?gm.Config.UpgradeHpCosts:gm.Config.UpgradeMagnetCosts;
            GetComponentInChildren<TMP_Text>().text=Id.ToUpper()+" "+level+"\n"+(level>=costs.Length?"MAX":costs[level]+" coins");
            GetComponentInChildren<TMP_Text>().color=GameColors.LightText;
            GetComponent<Button>().interactable=gm.ProgressLoaded&&!gm.RunSubmissionPending&&level<costs.Length&&gm.Save.coins>=costs[level];
        }
    }
}
