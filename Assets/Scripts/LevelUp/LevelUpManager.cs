using System;
using System.Collections.Generic;
using UnityEngine;

namespace SeagullStorm
{
    /// <summary>
    /// XP tracking, level curve, and level-up choice generation.
    /// </summary>
    public class LevelUpManager : MonoBehaviour
    {
        public static LevelUpManager Instance { get; private set; }

        public event Action<List<LevelUpChoice>> OnLevelUp;

        private void Awake()
        {
            Instance = this;
        }

        public void TriggerLevelUp()
        {
            var choices = GenerateChoices();

            HorizonManager.Instance?.RecordBreadcrumb("state",
                $"level_{GameManager.Instance.RunState.level}");

            AudioManager.Instance?.PlayLevelup();
            GameManager.Instance.ChangeState(GameState.LevelUp);

            OnLevelUp?.Invoke(choices);
        }

        public void SelectChoice(LevelUpChoice choice)
        {
            switch (choice.type)
            {
                case "weapon_new":
                    WeaponManager.Instance?.AddWeapon(choice.weaponId);
                    break;
                case "weapon_upgrade":
                    WeaponManager.Instance?.UpgradeWeapon(choice.weaponId);
                    break;
                case "stat_boost":
                    ApplyStatBoost(choice.id);
                    break;
            }

            AudioManager.Instance?.PlayUpgradeSelect();
            HorizonManager.Instance?.RecordBreadcrumb("user_action", $"levelup_chose_{choice.id}");

            GameManager.Instance.ChangeState(GameState.Run);
        }

        private List<LevelUpChoice> GenerateChoices()
        {
            var config = GameManager.Instance?.Config;
            var pool = config?.LevelupPool;
            var choices = new List<LevelUpChoice>();

            if (pool == null || pool.Count == 0)
            {
                choices.Add(new LevelUpChoice { id = "feather_dmg", type = "weapon_upgrade", weaponId = "feather", displayName = "Feather+", description = "DMG +15%" });
                choices.Add(new LevelUpChoice { id = "move_speed", type = "stat_boost", displayName = "Speed+", description = "Move +10%" });
                choices.Add(new LevelUpChoice { id = "max_hp", type = "stat_boost", displayName = "HP+", description = "Max HP +20" });
                return choices;
            }

            int totalWeight = 0;
            foreach (var e in pool) totalWeight += e.weight;

            var used = new HashSet<string>();
            int numChoices = Mathf.Min(config.LevelupChoices, pool.Count);

            while (choices.Count < numChoices && used.Count < pool.Count)
            {
                int roll = UnityEngine.Random.Range(0, totalWeight);
                int cumulative = 0;

                foreach (var entry in pool)
                {
                    cumulative += entry.weight;
                    if (roll < cumulative && !used.Contains(entry.id))
                    {
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
                        choices.Add(CreateChoice(entry));
                        break;
                    }
                }
            }

            return choices;
        }

        private LevelUpChoice CreateChoice(LevelupPoolEntry entry)
        {
            var choice = new LevelUpChoice { id = entry.id, type = entry.type };

            switch (entry.id)
            {
                case "feather_dmg": choice.weaponId = "feather"; choice.displayName = "Feather+"; choice.description = "DMG +15%"; break;
                case "feather_speed": choice.weaponId = "feather"; choice.displayName = "Feather Speed"; choice.description = "Fire rate +20%"; break;
                case "screech_new": choice.weaponId = "screech"; choice.displayName = "Screech"; choice.description = "NEW! AoE ring"; break;
                case "dive_new": choice.weaponId = "dive"; choice.displayName = "Dive Bomb"; choice.description = "NEW! Dash attack"; break;
                case "gust_new": choice.weaponId = "gust"; choice.displayName = "Wind Gust"; choice.description = "NEW! Knockback"; break;
                case "move_speed": choice.displayName = "Speed+"; choice.description = "Move +10%"; break;
                case "max_hp": choice.displayName = "HP+"; choice.description = "Max HP +20"; break;
                case "xp_magnet": choice.displayName = "Magnet+"; choice.description = "Pickup range +25%"; break;
            }

            return choice;
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
            var run = GameManager.Instance?.RunState;
            if (run == null) return;

            switch (id)
            {
                case "max_hp":
                    run.playerMaxHP += 20f;
                    run.playerHP = Mathf.Min(run.playerHP + 20f, run.playerMaxHP);
                    break;
            }
        }
    }
}
