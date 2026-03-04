using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using SeagullStorm.Data;
using SeagullStorm.SDK;

namespace SeagullStorm.Managers
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public SaveData PlayerData { get; private set; } = new SaveData();
        public GameConfig Config { get; private set; } = new GameConfig();
        public RunState CurrentRun { get; private set; } = new RunState();

        public event Action OnPlayerDataChanged;
        public event Action OnConfigLoaded;

        private int _consecutiveWave1Deaths;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public async Task LoadHubData()
        {
            var sdk = HorizonSDKIntegration.Instance;

            // Load remote config
            var configs = await sdk.LoadAllConfigs();
            if (configs != null)
            {
                Config.ApplyRemoteConfig(configs);
                OnConfigLoaded?.Invoke();
            }

            // Load cloud save
            PlayerData = await sdk.LoadCloudSave();
            OnPlayerDataChanged?.Invoke();

            // Start crash capture
            sdk.StartCrashCapture();
            sdk.RecordBreadcrumb("navigation", "entered_hub");
        }

        public void StartRun()
        {
            float maxHP = Config.upgradeHpValues[
                Mathf.Clamp(PlayerData.upgrades.hp, 0, Config.upgradeHpValues.Length - 1)
            ];
            CurrentRun.Reset(maxHP, Config.runDurationSeconds);

            HorizonSDKIntegration.Instance.RecordBreadcrumb("navigation", "entered_run");
            SceneManager.LoadScene(GameConstants.SceneRun);
        }

        public async Task EndRun(bool survived)
        {
            var sdk = HorizonSDKIntegration.Instance;
            var run = CurrentRun;

            // Calculate coins
            int coinsEarned = run.currentScore / Mathf.Max(1, Config.coinDivisor);
            PlayerData.coins += coinsEarned;
            PlayerData.totalRuns++;

            if (run.currentScore > PlayerData.highscore)
                PlayerData.highscore = run.currentScore;

            // Check for early death warning
            if (run.currentWave <= 1 && !survived)
            {
                _consecutiveWave1Deaths++;
                await sdk.LogEarlyDeathWarning(_consecutiveWave1Deaths);
            }
            else
            {
                _consecutiveWave1Deaths = 0;
            }

            // Submit score
            await sdk.SubmitScore(run.currentScore);

            // Save cloud data
            await sdk.SaveCloudData(PlayerData);

            // Log run
            await sdk.LogRunEnd(run.currentWave, run.currentLevel, run.currentScore,
                run.duration, PlayerData.upgrades, coinsEarned);

            sdk.RecordBreadcrumb("navigation", "game_over");

            OnPlayerDataChanged?.Invoke();
        }

        public bool TryPurchaseUpgrade(string upgradeType)
        {
            int currentLevel;
            int[] costs;
            int maxLevel;

            switch (upgradeType)
            {
                case "speed":
                    currentLevel = PlayerData.upgrades.speed;
                    costs = Config.upgradeSpeedCosts;
                    maxLevel = Config.upgradeSpeedMax;
                    break;
                case "damage":
                    currentLevel = PlayerData.upgrades.damage;
                    costs = Config.upgradeDamageCosts;
                    maxLevel = Config.upgradeDamageMax;
                    break;
                case "hp":
                    currentLevel = PlayerData.upgrades.hp;
                    costs = Config.upgradeHpCosts;
                    maxLevel = Config.upgradeHpMax;
                    break;
                case "magnet":
                    currentLevel = PlayerData.upgrades.magnet;
                    costs = Config.upgradeMagnetCosts;
                    maxLevel = Config.upgradeMagnetMax;
                    break;
                default:
                    return false;
            }

            if (currentLevel >= maxLevel) return false;
            if (currentLevel >= costs.Length) return false;

            int cost = costs[currentLevel];
            if (PlayerData.coins < cost) return false;

            PlayerData.coins -= cost;

            switch (upgradeType)
            {
                case "speed": PlayerData.upgrades.speed++; break;
                case "damage": PlayerData.upgrades.damage++; break;
                case "hp": PlayerData.upgrades.hp++; break;
                case "magnet": PlayerData.upgrades.magnet++; break;
            }

            HorizonSDKIntegration.Instance.RecordBreadcrumb("user_action",
                $"bought_{upgradeType}_{currentLevel + 1}");

            OnPlayerDataChanged?.Invoke();
            return true;
        }

        public float GetUpgradeMultiplier(string upgradeType)
        {
            switch (upgradeType)
            {
                case "speed":
                    return Config.upgradeSpeedValues[
                        Mathf.Clamp(PlayerData.upgrades.speed, 0, Config.upgradeSpeedValues.Length - 1)];
                case "damage":
                    return Config.upgradeDamageValues[
                        Mathf.Clamp(PlayerData.upgrades.damage, 0, Config.upgradeDamageValues.Length - 1)];
                default:
                    return 1f;
            }
        }

        public float GetPlayerMaxHP()
        {
            return Config.upgradeHpValues[
                Mathf.Clamp(PlayerData.upgrades.hp, 0, Config.upgradeHpValues.Length - 1)];
        }

        public float GetPickupRadius()
        {
            return Config.upgradeMagnetValues[
                Mathf.Clamp(PlayerData.upgrades.magnet, 0, Config.upgradeMagnetValues.Length - 1)];
        }

        public void GoToHub()
        {
            HorizonSDKIntegration.Instance.RecordBreadcrumb("navigation", "entered_hub");
            SceneManager.LoadScene(GameConstants.SceneHub);
        }

        public void GoToTitle()
        {
            SceneManager.LoadScene(GameConstants.SceneTitle);
        }
    }
}
