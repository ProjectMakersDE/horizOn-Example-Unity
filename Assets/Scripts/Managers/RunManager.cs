using System;
using System.Collections.Generic;
using UnityEngine;
using SeagullStorm.Data;
using SeagullStorm.Gameplay.Spawning;
using SeagullStorm.Gameplay.Weapons;
using SeagullStorm.SDK;

namespace SeagullStorm.Managers
{
    public class RunManager : MonoBehaviour
    {
        public static RunManager Instance { get; private set; }

        public bool IsPaused { get; private set; }
        public bool IsGameOver { get; private set; }
        public bool IsLevelingUp { get; private set; }

        public event Action OnRunTimerTick;
        public event Action OnGameOverTriggered;
        public event Action<List<LevelupOption>> OnLevelUpTriggered;
        public event Action OnRunResumed;
        public event Action OnHPChanged;
        public event Action OnScoreChanged;
        public event Action OnXPChanged;

        private bool _bossSpawned;

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            Time.timeScale = 1f;
            AudioManager.Instance?.PlayBattleMusic();
            HorizonSDKIntegration.Instance?.RecordBreadcrumb("navigation", "run_started");
        }

        private void Update()
        {
            if (IsGameOver || IsPaused || IsLevelingUp) return;

            var run = GameManager.Instance.CurrentRun;
            run.timeRemaining -= Time.deltaTime;
            run.duration += Time.deltaTime;
            run.currentScore = CalculateScore(run);

            OnRunTimerTick?.Invoke();
            OnScoreChanged?.Invoke();

            // Boss wave check
            if (run.timeRemaining <= 0f && !_bossSpawned)
            {
                run.timeRemaining = 0f;
                if (GameManager.Instance.Config.bossWaveEnabled)
                {
                    _bossSpawned = true;
                    WaveSpawner.Instance?.SpawnBossWave();
                    HorizonSDKIntegration.Instance?.RecordBreadcrumb("state", "boss_spawned");
                }
                else
                {
                    OnPlayerDeath(); // End run if boss disabled
                }
            }
        }

        private int CalculateScore(RunState run)
        {
            return run.kills * 10 + run.xpCollected + Mathf.RoundToInt(run.duration * 2f);
        }

        public void OnPlayerDeath()
        {
            if (IsGameOver) return;
            IsGameOver = true;
            Time.timeScale = 0f;

            AudioManager.Instance?.PlayGameOver();
            AudioManager.Instance?.PlayMenuMusic();

            OnGameOverTriggered?.Invoke();
        }

        public void OnLevelUp()
        {
            IsLevelingUp = true;
            Time.timeScale = 0f;

            var options = GenerateLevelupOptions();
            AudioManager.Instance?.PlayLevelup();

            var run = GameManager.Instance.CurrentRun;
            HorizonSDKIntegration.Instance?.RecordBreadcrumb("state", $"level_{run.currentLevel}");

            OnLevelUpTriggered?.Invoke(options);
        }

        public void SelectLevelupOption(LevelupOption option)
        {
            switch (option.type)
            {
                case "weapon_new":
                    WeaponManager.Instance?.AddWeapon(option.weaponId);
                    break;
                case "weapon_upgrade":
                    WeaponManager.Instance?.UpgradeWeapon(option.weaponId);
                    break;
                case "stat_boost":
                    ApplyStatBoost(option.id);
                    break;
            }

            AudioManager.Instance?.PlayUpgradeSelect();
            HorizonSDKIntegration.Instance?.RecordBreadcrumb("user_action", $"levelup_chose_{option.id}");

            IsLevelingUp = false;
            Time.timeScale = 1f;
            OnRunResumed?.Invoke();
        }

        public void PauseRun()
        {
            IsPaused = true;
            Time.timeScale = 0f;
        }

        public void ResumeRun()
        {
            IsPaused = false;
            Time.timeScale = 1f;
            OnRunResumed?.Invoke();
        }

        public void QuitRun()
        {
            Time.timeScale = 1f;
            OnPlayerDeath();
        }

        private List<LevelupOption> GenerateLevelupOptions()
        {
            var config = GameManager.Instance.Config;
            var pool = config.levelupPool;
            var options = new List<LevelupOption>();

            if (pool == null || pool.Count == 0)
            {
                // Fallback pool
                options.Add(new LevelupOption { id = "feather_dmg", type = "weapon_upgrade", weaponId = "feather", displayName = "Feather+", description = "DMG +15%" });
                options.Add(new LevelupOption { id = "move_speed", type = "stat_boost", displayName = "Speed+", description = "Move +10%" });
                options.Add(new LevelupOption { id = "max_hp", type = "stat_boost", displayName = "HP+", description = "Max HP +20" });
                return options;
            }

            // Weighted random selection
            int totalWeight = 0;
            foreach (var entry in pool) totalWeight += entry.weight;

            var used = new HashSet<string>();
            int choices = Mathf.Min(config.levelupChoices, pool.Count);

            while (options.Count < choices && used.Count < pool.Count)
            {
                int roll = UnityEngine.Random.Range(0, totalWeight);
                int cumulative = 0;

                foreach (var entry in pool)
                {
                    cumulative += entry.weight;
                    if (roll < cumulative && !used.Contains(entry.id))
                    {
                        // Skip new weapons if already owned
                        if (entry.type == "weapon_new")
                        {
                            string wid = GetWeaponIdForEntry(entry.id);
                            if (WeaponManager.Instance != null && WeaponManager.Instance.HasWeapon(wid))
                            {
                                used.Add(entry.id);
                                break;
                            }
                        }

                        used.Add(entry.id);
                        options.Add(CreateOption(entry));
                        break;
                    }
                }
            }

            return options;
        }

        private LevelupOption CreateOption(LevelupPoolEntry entry)
        {
            var option = new LevelupOption
            {
                id = entry.id,
                type = entry.type,
            };

            switch (entry.id)
            {
                case "feather_dmg":
                    option.weaponId = "feather";
                    option.displayName = "Feather+";
                    option.description = "DMG +15%";
                    break;
                case "feather_speed":
                    option.weaponId = "feather";
                    option.displayName = "Feather Speed";
                    option.description = "Fire rate +20%";
                    break;
                case "screech_new":
                    option.weaponId = "screech";
                    option.displayName = "Screech";
                    option.description = "NEW! AoE ring";
                    break;
                case "dive_new":
                    option.weaponId = "dive";
                    option.displayName = "Dive Bomb";
                    option.description = "NEW! Dash attack";
                    break;
                case "gust_new":
                    option.weaponId = "gust";
                    option.displayName = "Wind Gust";
                    option.description = "NEW! Knockback";
                    break;
                case "move_speed":
                    option.displayName = "Speed+";
                    option.description = "Move +10%";
                    break;
                case "max_hp":
                    option.displayName = "HP+";
                    option.description = "Max HP +20";
                    break;
                case "xp_magnet":
                    option.displayName = "Magnet+";
                    option.description = "Pickup range +25%";
                    break;
            }

            return option;
        }

        private string GetWeaponIdForEntry(string entryId)
        {
            switch (entryId)
            {
                case "screech_new": return "screech";
                case "dive_new": return "dive";
                case "gust_new": return "gust";
                default: return "";
            }
        }

        private void ApplyStatBoost(string id)
        {
            var run = GameManager.Instance.CurrentRun;
            switch (id)
            {
                case "move_speed":
                    // Temporary run boost - applied via multiplier check
                    break;
                case "max_hp":
                    run.playerMaxHP += 20f;
                    run.playerHP = Mathf.Min(run.playerHP + 20f, run.playerMaxHP);
                    OnHPChanged?.Invoke();
                    break;
                case "xp_magnet":
                    // Applied via pickup radius calculation
                    break;
            }
        }
    }

    [Serializable]
    public class LevelupOption
    {
        public string id;
        public string type;
        public string weaponId;
        public string displayName;
        public string description;
    }
}
