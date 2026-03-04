using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SeagullStorm
{
    /// <summary>
    /// Central state machine for the game. Controls which canvas is active
    /// and manages transitions between Hub, Run, Paused, LevelUp, and GameOver.
    /// Lives in GameScene (build index 2).
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Canvases")]
        [SerializeField] private GameObject hubCanvas;
        [SerializeField] private GameObject runHUDCanvas;
        [SerializeField] private GameObject levelUpCanvas;
        [SerializeField] private GameObject pauseCanvas;
        [SerializeField] private GameObject gameOverCanvas;

        [Header("Gameplay Objects")]
        [SerializeField] private GameObject player;
        [SerializeField] private GameObject tilemap;

        public GameState CurrentState { get; private set; } = GameState.Hub;
        public SaveData Save { get; set; }
        public GameConfig Config { get; set; }
        public RunState RunState { get; private set; } = new RunState();

        public event Action<GameState> OnStateChanged;
        public event Action OnSaveDataChanged;

        private int _consecutiveWave1Deaths;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            Save = SaveData.CreateDefault();
            Config = new GameConfig();
        }

        private void Start()
        {
            ChangeState(GameState.Hub);
        }

        public void ChangeState(GameState newState)
        {
            CurrentState = newState;

            // Activate/deactivate canvases
            SetCanvasActive(hubCanvas, newState == GameState.Hub);
            SetCanvasActive(runHUDCanvas, newState == GameState.Run || newState == GameState.Paused || newState == GameState.LevelUp);
            SetCanvasActive(pauseCanvas, newState == GameState.Paused);
            SetCanvasActive(levelUpCanvas, newState == GameState.LevelUp);
            SetCanvasActive(gameOverCanvas, newState == GameState.GameOver);

            // Gameplay objects
            bool inRun = newState == GameState.Run || newState == GameState.Paused || newState == GameState.LevelUp || newState == GameState.GameOver;
            if (player != null) player.SetActive(inRun);
            if (tilemap != null) tilemap.SetActive(inRun);

            // Time scale
            switch (newState)
            {
                case GameState.Hub:
                case GameState.Run:
                    Time.timeScale = 1f;
                    break;
                case GameState.Paused:
                case GameState.LevelUp:
                case GameState.GameOver:
                    Time.timeScale = 0f;
                    break;
            }

            HorizonManager.Instance?.RecordBreadcrumb("navigation", $"entered_{newState}");
            OnStateChanged?.Invoke(newState);
        }

        public void StartRun()
        {
            float maxHP = Config.UpgradeHpValues[
                Mathf.Clamp(Save.upgrades.hp, 0, Config.UpgradeHpValues.Length - 1)];
            RunState.Reset(maxHP, Config.RunDurationSeconds);

            // Reset player position
            if (player != null) player.transform.position = Vector3.zero;

            ChangeState(GameState.Run);
            AudioManager.Instance?.PlayBattleMusic();
        }

        public async void EndRun()
        {
            var run = RunState;
            run.coinsEarned = run.score / Mathf.Max(1, Config.CoinDivisor);
            Save.coins += run.coinsEarned;
            Save.totalRuns++;

            if (run.score > Save.highscore)
                Save.highscore = run.score;

            // Check balance warning
            if (run.wave <= 1 && run.playerHP <= 0)
            {
                _consecutiveWave1Deaths++;
                if (_consecutiveWave1Deaths >= 3)
                {
                    try { await HorizonManager.Instance.LogWarning(
                        "Player died in wave 1 three consecutive times - possible balancing issue"); }
                    catch { }
                    _consecutiveWave1Deaths = 0;
                }
            }
            else
            {
                _consecutiveWave1Deaths = 0;
            }

            // Submit score
            try { await HorizonManager.Instance.SubmitScore(run.score); } catch { }

            // Save cloud data
            try
            {
                string json = JsonUtility.ToJson(Save);
                await HorizonManager.Instance.SaveCloudData(json);
            }
            catch { }

            // Log run
            try
            {
                string msg = $"Run ended | Waves: {run.wave} | Level: {run.level} | Score: {run.score} | " +
                             $"Duration: {run.FormatDuration()} | " +
                             $"Upgrades: speed:{Save.upgrades.speed},dmg:{Save.upgrades.damage},hp:{Save.upgrades.hp} | " +
                             $"Coins earned: {run.coinsEarned}";
                await HorizonManager.Instance.LogRunEnd(msg);
            }
            catch { }

            OnSaveDataChanged?.Invoke();
        }

        public bool TryPurchaseUpgrade(string upgradeType)
        {
            int currentLevel;
            int[] costs;
            int maxLevel;

            switch (upgradeType)
            {
                case "speed":
                    currentLevel = Save.upgrades.speed;
                    costs = Config.UpgradeSpeedCosts;
                    maxLevel = costs.Length;
                    break;
                case "damage":
                    currentLevel = Save.upgrades.damage;
                    costs = Config.UpgradeDamageCosts;
                    maxLevel = costs.Length;
                    break;
                case "hp":
                    currentLevel = Save.upgrades.hp;
                    costs = Config.UpgradeHpCosts;
                    maxLevel = costs.Length;
                    break;
                case "magnet":
                    currentLevel = Save.upgrades.magnet;
                    costs = Config.UpgradeMagnetCosts;
                    maxLevel = costs.Length;
                    break;
                default:
                    return false;
            }

            if (currentLevel >= maxLevel) return false;

            int cost = costs[currentLevel];
            if (Save.coins < cost) return false;

            Save.coins -= cost;

            switch (upgradeType)
            {
                case "speed": Save.upgrades.speed++; break;
                case "damage": Save.upgrades.damage++; break;
                case "hp": Save.upgrades.hp++; break;
                case "magnet": Save.upgrades.magnet++; break;
            }

            HorizonManager.Instance?.RecordBreadcrumb("user_action", $"bought_{upgradeType}_{currentLevel + 1}");
            OnSaveDataChanged?.Invoke();
            return true;
        }

        public float GetSpeedMultiplier()
        {
            return Config.UpgradeSpeedValues[
                Mathf.Clamp(Save.upgrades.speed, 0, Config.UpgradeSpeedValues.Length - 1)];
        }

        public float GetDamageMultiplier()
        {
            return Config.UpgradeDamageValues[
                Mathf.Clamp(Save.upgrades.damage, 0, Config.UpgradeDamageValues.Length - 1)];
        }

        public float GetPlayerMaxHP()
        {
            return Config.UpgradeHpValues[
                Mathf.Clamp(Save.upgrades.hp, 0, Config.UpgradeHpValues.Length - 1)];
        }

        public float GetPickupRadius()
        {
            return Config.UpgradeMagnetValues[
                Mathf.Clamp(Save.upgrades.magnet, 0, Config.UpgradeMagnetValues.Length - 1)];
        }

        public void SignOutAndReturn()
        {
            HorizonManager.Instance?.SignOut();
            SceneManager.LoadScene("TitleScene");
        }

        private void SetCanvasActive(GameObject canvas, bool active)
        {
            if (canvas != null) canvas.SetActive(active);
        }
    }
}
