using System;
using System.Collections;
using System.IO;
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
        private IEnumerator Start()
        {
            yield return new WaitUntil(()=>SceneManager.GetActiveScene().name=="TitleScene");
            yield return new WaitForSecondsRealtime(2);
            if (!CheckFocus("PLAY OFFLINE", true)) yield break;
            ScreenCapture.CaptureScreenshot(CapturePath("title"));
            yield return new WaitForSecondsRealtime(.5f);
            SceneManager.LoadScene("GameScene");
            yield return new WaitForSecondsRealtime(2);
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
            sdkButton.onClick.Invoke();
            yield return new WaitForSecondsRealtime(.3f);
            if (!CheckFocus("Top 10", true)) yield break;
            ScreenCapture.CaptureScreenshot(CapturePath("sdk"));
            yield return new WaitForSecondsRealtime(.5f);
            GameObject.Find("Player values")?.GetComponent<UnityEngine.UI.Button>()?.onClick.Invoke();
            yield return new WaitForSecondsRealtime(.3f);
            if (!CheckFocus("Read balances", true)) yield break;
            GameObject.Find("Read balances")?.GetComponent<UnityEngine.UI.Button>()?.onClick.Invoke();
            if (HorizonManager.Instance.Online) { Fail("Player values demo left offline practice"); yield break; }
            ScreenCapture.CaptureScreenshot(CapturePath("player-values"));
            yield return new WaitForSecondsRealtime(.5f);
            GameObject.Find("BACK")?.GetComponent<UnityEngine.UI.Button>()?.onClick.Invoke();
            yield return null; // Re-enabled uGUI controls register for raycasting on the next rendered frame.
            if (!CheckFocus("Player values", true)) yield break;
            GameObject.Find("Statistics")?.GetComponent<UnityEngine.UI.Button>()?.onClick.Invoke();
            yield return new WaitForSecondsRealtime(.3f);
            if (!CheckFocus("Read snapshot", true)) yield break;
            GameObject.Find("Read snapshot")?.GetComponent<UnityEngine.UI.Button>()?.onClick.Invoke();
            if (HorizonManager.Instance.Online) { Fail("Statistics demo left offline practice"); yield break; }
            ScreenCapture.CaptureScreenshot(CapturePath("statistics"));
            yield return new WaitForSecondsRealtime(.5f);
            GameObject.Find("BACK")?.GetComponent<UnityEngine.UI.Button>()?.onClick.Invoke();
            yield return null;
            if (!CheckFocus("Statistics", true)) yield break;
            gm.ProgressLoaded = false;
            yield return null;
            GameObject.Find("Seagull interface/horizOn UI/SDK demos/CLOSE")?.GetComponent<UnityEngine.UI.Button>()?.onClick.Invoke();
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
            gm.ChangeState(GameState.Paused);float duration=gm.RunState.duration;
            yield return new WaitForSecondsRealtime(.3f);
            if(Mathf.Abs(gm.RunState.duration-duration)>.01f){Fail("Pause advanced timer");yield break;}
            if (!CheckFocus("ResumeButton", true)) yield break;
            ScreenCapture.CaptureScreenshot(CapturePath("pause"));
            yield return new WaitForSecondsRealtime(.5f);
            gm.ChangeState(GameState.Run);
            WeaponManager.Instance.AddWeapon("screech");WeaponManager.Instance.AddWeapon("gust");
            PickupPool.Instance.SpawnXP(PlayerController.Instance.transform.position,30);
            yield return new WaitForSecondsRealtime(1);
            if(gm.CurrentState!=GameState.LevelUp){Fail("Shell XP did not offer level-up");yield break;}
            if (!CheckFocus("LevelUpChoiceCard(Clone)", true)) yield break;
            ScreenCapture.CaptureScreenshot(CapturePath("levelup"));
            yield return new WaitForSecondsRealtime(.5f);
            LevelUpManager.Instance.SelectChoice(new LevelUpChoice{id="max_hp",type="stat_boost"});
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
            gm.ChangeState(GameState.Hub);gm.StartRun();yield return new WaitForSecondsRealtime(1);
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
