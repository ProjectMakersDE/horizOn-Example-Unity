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
            ScreenCapture.CaptureScreenshot(Path.GetFullPath(Path.Combine(Application.dataPath,"../../qa-title.png")));
            yield return new WaitForSecondsRealtime(.5f);
            SceneManager.LoadScene("GameScene");
            yield return new WaitForSecondsRealtime(2);
            var gm=GameManager.Instance;
            if (gm==null || !gm.ProgressLoaded || HorizonManager.Instance.Online) { Fail("Offline hub failed");yield break; }
            ScreenCapture.CaptureScreenshot(Path.GetFullPath(Path.Combine(Application.dataPath,"../../qa-hub.png")));
            yield return new WaitForSecondsRealtime(.5f);
            var sdkButton = GameObject.Find("SDK DEMOS")?.GetComponent<UnityEngine.UI.Button>();
            if (sdkButton == null) { Fail("SDK panel button missing"); yield break; }
            sdkButton.onClick.Invoke();
            yield return new WaitForSecondsRealtime(.3f);
            ScreenCapture.CaptureScreenshot(Path.GetFullPath(Path.Combine(Application.dataPath,"../../qa-sdk.png")));
            yield return new WaitForSecondsRealtime(.5f);
            GameObject.Find("Seagull interface/horizOn UI/SDK demos/CLOSE")?.GetComponent<UnityEngine.UI.Button>()?.onClick.Invoke();
            gm.Save.coins=20;
            if (!gm.TryPurchaseUpgrade("speed") || gm.Save.upgrades.speed!=1 || gm.Save.coins!=10) { Fail("Affordable meta upgrade failed"); yield break; }
            if (gm.TryPurchaseUpgrade("hp")) { Fail("Unaffordable upgrade accepted"); yield break; }
            gm.Config.RunDurationSeconds=30; gm.Config.BossWaveEnabled=false;
            gm.StartRun(); yield return new WaitForSecondsRealtime(1);
            if(gm.CurrentState!=GameState.Run || PlayerController.Instance==null){Fail("Run did not start");yield break;}
            var start=PlayerController.Instance.transform.position;
            PlayerController.Instance.transform.position=start+Vector3.right*50;
            yield return new WaitForSecondsRealtime(1);
            gm.ChangeState(GameState.Paused);float duration=gm.RunState.duration;
            yield return new WaitForSecondsRealtime(.3f);
            if(Mathf.Abs(gm.RunState.duration-duration)>.01f){Fail("Pause advanced timer");yield break;}
            ScreenCapture.CaptureScreenshot(Path.GetFullPath(Path.Combine(Application.dataPath,"../../qa-pause.png")));
            yield return new WaitForSecondsRealtime(.5f);
            gm.ChangeState(GameState.Run);
            WeaponManager.Instance.AddWeapon("screech");WeaponManager.Instance.AddWeapon("gust");
            PickupPool.Instance.SpawnXP(PlayerController.Instance.transform.position,30);
            yield return new WaitForSecondsRealtime(1);
            if(gm.CurrentState!=GameState.LevelUp){Fail("Shell XP did not offer level-up");yield break;}
            ScreenCapture.CaptureScreenshot(Path.GetFullPath(Path.Combine(Application.dataPath,"../../qa-levelup.png")));
            yield return new WaitForSecondsRealtime(.5f);
            LevelUpManager.Instance.SelectChoice(new LevelUpChoice{id="max_hp",type="stat_boost"});
            yield return new WaitForSecondsRealtime(3);
            ScreenCapture.CaptureScreenshot(Path.GetFullPath(Path.Combine(Application.dataPath,"../../qa-run.png")));
            yield return new WaitForSecondsRealtime(.5f);
            gm.EndRun(true);yield return new WaitForSecondsRealtime(1);
            if(!gm.ScoreSubmitted||gm.RunSubmissionPending||gm.Save.totalRuns<1){Fail("Offline completion failed");yield break;}
            ScreenCapture.CaptureScreenshot(Path.GetFullPath(Path.Combine(Application.dataPath,"../../qa-gameover.png")));
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
            foreach (var capture in new[]{"title","hub","sdk","pause","levelup","run","gameover"})
                if (!File.Exists(Path.GetFullPath(Path.Combine(Application.dataPath,"../../qa-"+capture+".png")))) { Fail("Screenshot missing: "+capture); yield break; }
            Debug.Log("[SEAGULL SMOKE] PASS: offline boot/hub/SDK panel, meta-upgrade affordability, start, pause, shell XP, level-up, weapon unlock, completion, duplicate-end guard replay reset and boss death/victory.");
            Application.Quit(0);
        }
        private void Update()
        {
            if (Time.realtimeSinceStartup > 60f) Fail("Smoke exceeded 60-second deadline");
        }
        private static void Fail(string reason){Debug.LogError("[SEAGULL SMOKE] FAIL: "+reason);Application.Quit(1);}
    }
}
