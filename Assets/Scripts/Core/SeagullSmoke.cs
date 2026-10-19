using System;
using System.Collections;
using System.IO;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace SeagullStorm
{
    /// <summary>Opt-in local practice QA. Never configures a backend or sends an online request.</summary>
    public class SeagullSmoke : MonoBehaviour
    {
        public static bool Enabled => Array.IndexOf(Environment.GetCommandLineArgs(),"-seagull-smoke") >= 0;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            if (!Enabled) return;
            Application.runInBackground = true;
            PlayerPrefs.DeleteKey("seagull-storm.progress.qa.practice");
            var go = new GameObject("Practice smoke test"); DontDestroyOnLoad(go); go.AddComponent<SeagullSmoke>();
        }
        private Keyboard _keyboard;
        private Gamepad _gamepad;
        private IEnumerator Start()
        {
            _keyboard = InputSystem.AddDevice<Keyboard>();
            _gamepad = InputSystem.AddDevice<Gamepad>();
            yield return new WaitUntil(()=>SceneManager.GetActiveScene().name=="TitleScene");
            yield return new WaitForSecondsRealtime(2);
            if (!CheckFocus("PLAY OFFLINE", true)) yield break;
            ScreenCapture.CaptureScreenshot(CapturePath("title"));
            yield return new WaitForSecondsRealtime(.5f);
            // Exercise the actual module, not Button.onClick, in the ordinary practice path.
            yield return KeyPress(Key.Tab);
            if (!CheckSelection("Pilot name")) yield break;
            yield return KeyPress(Key.Enter);
            if (SceneManager.GetActiveScene().name != "TitleScene") { Fail("Text Enter submitted the title menu"); yield break; }
            yield return KeyPress(Key.LeftShift, Key.Tab);
            if (!CheckFocus("PLAY OFFLINE", true)) yield break;
            yield return Pad(new GamepadState().WithButton(GamepadButton.DpadRight));
            if (!CheckSelection("Pilot name")) yield break;
            yield return KeyPress(Key.LeftShift, Key.Tab);
            yield return KeyPress(Key.Space);
            yield return new WaitForSecondsRealtime(2);
            if (SceneManager.GetActiveScene().name != "GameScene") { Fail("Space did not enter practice"); yield break; }
            var gm=GameManager.Instance;
            if (gm==null || !gm.ProgressLoaded || HorizonManager.Instance.Online) { Fail("Offline hub failed");yield break; }
            try { PlayerValueIntentSmoke.Check(); }
            catch (Exception error) { Fail("Player value intent protection failed: " + error.Message); yield break; }
            try { StatisticsIntentSmoke.Check(); }
            catch (Exception error) { Fail("Statistics intent protection failed: " + error.Message); yield break; }
            if (!CheckFocus("START RUN", true)) yield break;
            ScreenCapture.CaptureScreenshot(CapturePath("hub"));
            yield return new WaitForSecondsRealtime(.5f);
            var sdkButton = GameObject.Find("SDK DEMOS")?.GetComponent<UnityEngine.UI.Button>();
            if (sdkButton == null) { Fail("SDK panel button missing"); yield break; }
            yield return KeyPress(Key.Tab);
            if (!CheckFocus("SDK DEMOS", true)) yield break;
            yield return KeyPress(Key.Enter);
            yield return new WaitForSecondsRealtime(.3f);
            if (!CheckFocus("Top 10", true)) yield break;
            ScreenCapture.CaptureScreenshot(CapturePath("sdk"));
            yield return new WaitForSecondsRealtime(.5f);
            yield return TabTo("Player values");
            yield return KeyPress(Key.Enter);
            yield return new WaitForSecondsRealtime(.3f);
            if (!CheckFocus("Read balances", true)) yield break;
            GameObject.Find("Read balances")?.GetComponent<UnityEngine.UI.Button>()?.onClick.Invoke();
            if (HorizonManager.Instance.Online) { Fail("Player values demo left offline practice"); yield break; }
            ScreenCapture.CaptureScreenshot(CapturePath("player-values"));
            yield return new WaitForSecondsRealtime(.5f);
            yield return KeyPress(Key.Escape);
            yield return null; // Re-enabled uGUI controls register for raycasting on the next rendered frame.
            if (!CheckFocus("Player values", true)) yield break;
            yield return KeyPress(Key.Tab);
            if (!CheckFocus("Statistics", true)) yield break;
            yield return Pad(new GamepadState().WithButton(GamepadButton.South));
            yield return new WaitForSecondsRealtime(.3f);
            if (!CheckFocus("Read snapshot", true)) yield break;
            GameObject.Find("Read snapshot")?.GetComponent<UnityEngine.UI.Button>()?.onClick.Invoke();
            if (HorizonManager.Instance.Online) { Fail("Statistics demo left offline practice"); yield break; }
            ScreenCapture.CaptureScreenshot(CapturePath("statistics"));
            yield return new WaitForSecondsRealtime(.5f);
            yield return Pad(new GamepadState().WithButton(GamepadButton.East));
            yield return null;
            if (!CheckFocus("Statistics", true)) yield break;
            yield return TextAndLabNavigation();
            gm.ProgressLoaded = false;
            yield return null;
            yield return KeyPress(Key.Escape);
            if (!CheckFocus("SDK DEMOS", true)) yield break;
            gm.ProgressLoaded = true;
            yield return null;
            if (!CheckFocus("START RUN", true)) yield break;
            gm.Save.coins=20;
            if (!gm.TryPurchaseUpgrade("speed") || gm.Save.upgrades.speed!=1 || gm.Save.coins!=10) { Fail("Affordable meta upgrade failed"); yield break; }
            if (gm.TryPurchaseUpgrade("hp")) { Fail("Unaffordable upgrade accepted"); yield break; }
            gm.Config.RunDurationSeconds=30; gm.Config.BossWaveEnabled=false;
            gm.StartRun(); yield return new WaitForSecondsRealtime(1);
            if(gm.CurrentState!=GameState.Run || PlayerController.Instance==null){Fail("Run did not start");yield break;}
            yield return SeagullCutoutSmoke.Check();
            if (!SeagullCutoutSmoke.Passed) yield break;
            var start=PlayerController.Instance.transform.position;
            PlayerController.Instance.transform.position=start+Vector3.right*50;
            yield return new WaitForSecondsRealtime(1);
            yield return KeyPress(Key.Escape);
            if (gm.CurrentState != GameState.Paused) { Fail("Escape did not pause"); yield break; }
            float duration=gm.RunState.duration;
            yield return new WaitForSecondsRealtime(.3f);
            if(Mathf.Abs(gm.RunState.duration-duration)>.01f){Fail("Pause advanced timer");yield break;}
            if (!CheckFocus("ResumeButton", true)) yield break;
            ScreenCapture.CaptureScreenshot(CapturePath("pause"));
            yield return new WaitForSecondsRealtime(.5f);
            yield return Pad(new GamepadState().WithButton(GamepadButton.East));
            if (gm.CurrentState != GameState.Run) { Fail("Gamepad Back did not resume"); yield break; }
            WeaponManager.Instance.AddWeapon("screech");WeaponManager.Instance.AddWeapon("gust");
            PickupPool.Instance.SpawnXP(PlayerController.Instance.transform.position,30);
            yield return new WaitForSecondsRealtime(1);
            if(gm.CurrentState!=GameState.LevelUp){Fail("Shell XP did not offer level-up");yield break;}
            if (!CheckFocus("LevelUpChoiceCard(Clone)", true)) yield break;
            ScreenCapture.CaptureScreenshot(CapturePath("levelup"));
            yield return new WaitForSecondsRealtime(.5f);
            yield return KeyPress(Key.Escape);
            if (gm.CurrentState != GameState.LevelUp) { Fail("Back skipped required choice"); yield break; }
            yield return Pad(new GamepadState { leftStick = Vector2.right });
            yield return Pad(new GamepadState().WithButton(GamepadButton.South));
            yield return new WaitForSecondsRealtime(3);
            // Random enemy XP may offer another level-up during the native run.
            for (int extra = 0; gm.CurrentState == GameState.LevelUp && extra < 5; extra++)
            {
                if (!CheckFocus("LevelUpChoiceCard(Clone)", true)) yield break;
                UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject.GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
                yield return null;
            }
            if (gm.CurrentState != GameState.Run) { Fail("Run capture blocked by state " + gm.CurrentState); yield break; }
            ScreenCapture.CaptureScreenshot(CapturePath("run"));
            yield return new WaitForSecondsRealtime(.5f);
            for (int extra = 0; gm.CurrentState == GameState.LevelUp && extra < 5; extra++)
            {
                if (!CheckFocus("LevelUpChoiceCard(Clone)", true)) yield break;
                UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject.GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
                yield return null;
            }
            if (gm.CurrentState != GameState.Run) { Fail("Completion blocked by state " + gm.CurrentState); yield break; }
            gm.EndRun(true);yield return new WaitForSecondsRealtime(1);
            if(!gm.ScoreSubmitted||gm.RunSubmissionPending||gm.Save.totalRuns<1){Fail("Offline completion failed: state="+gm.CurrentState+", submitted="+gm.ScoreSubmitted+", pending="+gm.RunSubmissionPending+", runs="+gm.Save.totalRuns);yield break;}
            if (!CheckFocus("PlayAgainButton", true)) yield break;
            ScreenCapture.CaptureScreenshot(CapturePath("gameover"));
            yield return new WaitForSecondsRealtime(.5f);
            int runs=gm.Save.totalRuns;gm.EndRun();
            if(gm.Save.totalRuns!=runs){Fail("Duplicate end rewarded twice");yield break;}
            yield return KeyPress(Key.Escape);
            if (gm.CurrentState != GameState.Hub) { Fail("Game over Back did not return to hub"); yield break; }
            yield return KeyPress(Key.Enter);
            yield return new WaitForSecondsRealtime(1);
            if(gm.RunState.level!=1||gm.RunState.kills!=0||gm.RunState.activeWeapons.Count!=1){Fail("Replay reset failed");yield break;}
            gm.Config.BossWaveEnabled = true;
            var boss = EnemyPool.Instance.SpawnEnemy("boss", PlayerController.Instance.transform.position + Vector3.right * 80, PlayerController.Instance.transform);
            if (boss == null) { Fail("Boss prefab failed to spawn"); yield break; }
            boss.TakeHit(9999);
            yield return new WaitForSecondsRealtime(.5f);
            if (!gm.Victory || gm.CurrentState != GameState.GameOver) { Fail("Boss death failed to trigger victory"); yield break; }
            foreach (var capture in new[]{"title","hub","sdk","player-values","statistics","pause","levelup","run","gameover"})
                if (!File.Exists(CapturePath(capture))) { Fail("Screenshot missing: "+capture); yield break; }
            Debug.Log("[SEAGULL SMOKE] PASS: offline boot/hub/SDK panel, menu keyboard focus and unobstructed button raycasts, meta-upgrade affordability, start, pause, shell XP, level-up, weapon unlock, completion, duplicate-end guard replay reset and boss death/victory.");
            Application.Quit(0);
        }
        private IEnumerator KeyPress(params Key[] keys)
        {
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState(keys));
            yield return null; yield return null;
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState());
            yield return null; yield return null;
        }
        private IEnumerator Pad(GamepadState state)
        {
            InputSystem.QueueStateEvent(_gamepad, state); yield return null; yield return null;
            InputSystem.QueueStateEvent(_gamepad, new GamepadState()); yield return null; yield return null;
        }
        private IEnumerator TabTo(string target)
        {
            for (int i = 0; i < 64 && EventSystem.current.currentSelectedGameObject?.name != target; i++) yield return KeyPress(Key.Tab);
            CheckSelection(target);
        }
        private IEnumerator TextAndLabNavigation()
        {
            yield return TabTo("Gift code");
            var code = EventSystem.current.currentSelectedGameObject.GetComponent<TMP_InputField>();
            string original = code.text; int prepared = 0;
            UnityEngine.Events.UnityAction observe = () => prepared++;
            var prepare = GameObject.Find("Prepare gift").GetComponent<Button>(); prepare.onClick.AddListener(observe);
            code.text = "keyboard-only draft"; yield return KeyPress(Key.Enter);
            prepare.onClick.RemoveListener(observe); code.text = original;
            if (prepared != 0 || !CheckSelection("Gift code")) { Fail("Text Enter prepared a gift"); yield break; }
            yield return TabTo("Feature lab"); yield return KeyPress(Key.Enter);
            if (!CheckFocus("Back to game", true)) yield break;
            yield return TabTo("Chat"); yield return KeyPress(Key.Enter);
            yield return TabTo("Named arguments JSON");
            var arguments = EventSystem.current.currentSelectedGameObject.GetComponent<TMP_InputField>();
            string json = arguments.text, output = SeagullFeatureLab.Instance.LastOutput;
            InputSystem.QueueTextEvent(_keyboard, ' '); yield return KeyPress(Key.Enter);
            if (SeagullFeatureLab.Instance.Busy || SeagullFeatureLab.Instance.LastOutput != output) { Fail("Chat JSON editing executed an action"); yield break; }
            arguments.text = json;
            yield return KeyPress(Key.Escape); // Leave text editing before closing the menu.
            if (!SeagullFeatureLab.Instance.Visible) { Fail("Text Back closed its menu"); yield break; }
            yield return KeyPress(Key.Escape);
            if (SeagullFeatureLab.Instance.Visible || !CheckFocus("Feature lab", true)) { Fail("Lab Back/return focus failed"); yield break; }
            yield return KeyPress(Key.LeftArrow);
            var selection = EventSystem.current.currentSelectedGameObject;
            if (selection == null || !selection.transform.IsChildOf(GameObject.Find("SDK demos").transform)) Fail("Arrow navigation escaped active menu");
            ScreenCapture.CaptureScreenshot(CapturePath("keyboard-lab"));
        }
        private static bool CheckSelection(string expected)
        {
            var selected = EventSystem.current.currentSelectedGameObject;
            if (selected == null || selected.name != expected || selected.GetComponent<Selectable>()?.IsInteractable() != true || selected.GetComponent<Outline>()?.enabled != true)
            { Fail("Input focus missing: " + expected); return false; }
            return true;
        }
        private void OnDestroy()
        {
            if (_keyboard != null) InputSystem.RemoveDevice(_keyboard);
            if (_gamepad != null) InputSystem.RemoveDevice(_gamepad);
        }
        private static string CapturePath(string name)
        {
            var args = Environment.GetCommandLineArgs();
            int option = Array.IndexOf(args, "-seagull-smoke-output");
            string folder = option >= 0 && option + 1 < args.Length ? args[option + 1] : Path.GetFullPath(Path.Combine(Application.dataPath, "../../tmp"));
            Directory.CreateDirectory(folder);
            return Path.Combine(folder, "qa-" + name + ".png");
        }
        private static bool CheckFocus(string expected, bool raycast)
        {
            var events = UnityEngine.EventSystems.EventSystem.current;
            var selected = events?.currentSelectedGameObject;
            if (selected == null || selected.name != expected)
            { Fail("Keyboard focus missing: " + expected + "; selected=" + (selected != null ? selected.name : "<none>")); return false; }
            var button = selected.GetComponent<UnityEngine.UI.Button>();
            var image = button?.targetGraphic as UnityEngine.UI.Image;
            if (button == null || !button.IsInteractable() || button.transition != UnityEngine.UI.Selectable.Transition.ColorTint || image == null || image.sprite != null || image.overrideSprite != null || selected.GetComponent<UnityEngine.UI.Outline>()?.enabled != true)
            { Fail("Selected menu control is disabled or uses a legacy state sprite: " + expected); return false; }
            if (raycast)
            {
                Canvas.ForceUpdateCanvases();
                var rect = selected.GetComponent<RectTransform>();
                var point = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center));
                var hits = new System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult>();
                events.RaycastAll(new UnityEngine.EventSystems.PointerEventData(events) { position = point }, hits);
                if (hits.Count == 0 || hits[0].gameObject != selected)
                { Fail("Button raycast blocked: " + expected + "; hit=" + (hits.Count > 0 ? hits[0].gameObject.name : "<none>")); return false; }
            }
            return true;
        }
        private void Update()
        {
            if (Time.realtimeSinceStartup > 60f) Fail("Smoke exceeded 60-second deadline");
        }
        private static void Fail(string reason){Debug.LogError("[SEAGULL SMOKE] FAIL: "+reason);Application.Quit(1);}
    }
}
