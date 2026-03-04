using System;
using System.Collections.Generic;
using UnityEngine;

namespace SeagullStorm
{
    [Serializable]
    public class LevelupPoolEntry
    {
        public string id;
        public string type;
        public int weight;
    }

    public class GameConfig
    {
        // Gameplay
        public int RunDurationSeconds = 180;
        public bool BossWaveEnabled = true;
        public int CoinDivisor = 10;
        public int XpPerKillBase = 10;
        public float XpLevelCurve = 1.4f;

        // Wave system
        public int WaveIntervalSeconds = 15;
        public int WaveEnemyCountBase = 5;
        public float WaveEnemyCountGrowth = 1.3f;
        public int WaveBossHp = 500;

        // Enemies
        public float EnemyCrabSpeed = 40f;
        public int EnemyCrabHp = 30;
        public int EnemyCrabDamage = 10;
        public int EnemyCrabXp = 10;
        public float EnemyJellyfishSpeed = 60f;
        public int EnemyJellyfishHp = 50;
        public int EnemyJellyfishDamage = 15;
        public int EnemyJellyfishXp = 20;
        public float EnemyPirateSpeed = 100f;
        public int EnemyPirateHp = 40;
        public int EnemyPirateDamage = 20;
        public int EnemyPirateXp = 25;

        // Weapons
        public int WeaponFeatherDamage = 20;
        public float WeaponFeatherCooldown = 0.8f;
        public int WeaponFeatherProjectiles = 1;
        public int WeaponScreechDamage = 15;
        public float WeaponScreechCooldown = 2.0f;
        public float WeaponScreechRadius = 80f;
        public int WeaponDiveDamage = 50;
        public float WeaponDiveCooldown = 3.0f;
        public float WeaponDiveRange = 120f;
        public int WeaponGustDamage = 10;
        public float WeaponGustCooldown = 2.5f;
        public float WeaponGustKnockback = 60f;

        // Hub upgrades
        public int[] UpgradeSpeedCosts = { 100, 300, 600, 1000 };
        public float[] UpgradeSpeedValues = { 1.0f, 1.1f, 1.2f, 1.35f, 1.5f };
        public int[] UpgradeDamageCosts = { 100, 300, 600, 1000 };
        public float[] UpgradeDamageValues = { 1.0f, 1.15f, 1.3f, 1.5f, 1.8f };
        public int[] UpgradeHpCosts = { 150, 400, 800 };
        public float[] UpgradeHpValues = { 100f, 130f, 170f, 220f };
        public int[] UpgradeMagnetCosts = { 200, 500 };
        public float[] UpgradeMagnetValues = { 50f, 80f, 120f };

        // Levelup
        public int LevelupChoices = 3;
        public List<LevelupPoolEntry> LevelupPool = new List<LevelupPoolEntry>
        {
            new LevelupPoolEntry { id = "feather_dmg", type = "weapon_upgrade", weight = 3 },
            new LevelupPoolEntry { id = "feather_speed", type = "weapon_upgrade", weight = 2 },
            new LevelupPoolEntry { id = "screech_new", type = "weapon_new", weight = 1 },
            new LevelupPoolEntry { id = "dive_new", type = "weapon_new", weight = 1 },
            new LevelupPoolEntry { id = "gust_new", type = "weapon_new", weight = 1 },
            new LevelupPoolEntry { id = "move_speed", type = "stat_boost", weight = 2 },
            new LevelupPoolEntry { id = "max_hp", type = "stat_boost", weight = 2 },
            new LevelupPoolEntry { id = "xp_magnet", type = "stat_boost", weight = 2 },
        };

        public static GameConfig Parse(Dictionary<string, string> configs)
        {
            var c = new GameConfig();
            if (configs == null) return c;

            TryGetInt(configs, "run_duration_seconds", ref c.RunDurationSeconds);
            TryGetBool(configs, "boss_wave_enabled", ref c.BossWaveEnabled);
            TryGetInt(configs, "coin_divisor", ref c.CoinDivisor);
            TryGetInt(configs, "xp_per_kill_base", ref c.XpPerKillBase);
            TryGetFloat(configs, "xp_level_curve", ref c.XpLevelCurve);

            TryGetInt(configs, "wave_interval_seconds", ref c.WaveIntervalSeconds);
            TryGetInt(configs, "wave_enemy_count_base", ref c.WaveEnemyCountBase);
            TryGetFloat(configs, "wave_enemy_count_growth", ref c.WaveEnemyCountGrowth);
            TryGetInt(configs, "wave_boss_hp", ref c.WaveBossHp);

            TryGetFloat(configs, "enemy_crab_speed", ref c.EnemyCrabSpeed);
            TryGetInt(configs, "enemy_crab_hp", ref c.EnemyCrabHp);
            TryGetInt(configs, "enemy_crab_damage", ref c.EnemyCrabDamage);
            TryGetInt(configs, "enemy_crab_xp", ref c.EnemyCrabXp);
            TryGetFloat(configs, "enemy_jellyfish_speed", ref c.EnemyJellyfishSpeed);
            TryGetInt(configs, "enemy_jellyfish_hp", ref c.EnemyJellyfishHp);
            TryGetInt(configs, "enemy_jellyfish_damage", ref c.EnemyJellyfishDamage);
            TryGetInt(configs, "enemy_jellyfish_xp", ref c.EnemyJellyfishXp);
            TryGetFloat(configs, "enemy_pirate_speed", ref c.EnemyPirateSpeed);
            TryGetInt(configs, "enemy_pirate_hp", ref c.EnemyPirateHp);
            TryGetInt(configs, "enemy_pirate_damage", ref c.EnemyPirateDamage);
            TryGetInt(configs, "enemy_pirate_xp", ref c.EnemyPirateXp);

            TryGetInt(configs, "weapon_feather_damage", ref c.WeaponFeatherDamage);
            TryGetFloat(configs, "weapon_feather_cooldown", ref c.WeaponFeatherCooldown);
            TryGetInt(configs, "weapon_feather_projectiles", ref c.WeaponFeatherProjectiles);
            TryGetInt(configs, "weapon_screech_damage", ref c.WeaponScreechDamage);
            TryGetFloat(configs, "weapon_screech_cooldown", ref c.WeaponScreechCooldown);
            TryGetFloat(configs, "weapon_screech_radius", ref c.WeaponScreechRadius);
            TryGetInt(configs, "weapon_dive_damage", ref c.WeaponDiveDamage);
            TryGetFloat(configs, "weapon_dive_cooldown", ref c.WeaponDiveCooldown);
            TryGetFloat(configs, "weapon_dive_range", ref c.WeaponDiveRange);
            TryGetInt(configs, "weapon_gust_damage", ref c.WeaponGustDamage);
            TryGetFloat(configs, "weapon_gust_cooldown", ref c.WeaponGustCooldown);
            TryGetFloat(configs, "weapon_gust_knockback", ref c.WeaponGustKnockback);

            TryGetIntArray(configs, "upgrade_speed_costs", ref c.UpgradeSpeedCosts);
            TryGetFloatArray(configs, "upgrade_speed_values", ref c.UpgradeSpeedValues);
            TryGetIntArray(configs, "upgrade_damage_costs", ref c.UpgradeDamageCosts);
            TryGetFloatArray(configs, "upgrade_damage_values", ref c.UpgradeDamageValues);
            TryGetIntArray(configs, "upgrade_hp_costs", ref c.UpgradeHpCosts);
            TryGetFloatArray(configs, "upgrade_hp_values", ref c.UpgradeHpValues);
            TryGetIntArray(configs, "upgrade_magnet_costs", ref c.UpgradeMagnetCosts);
            TryGetFloatArray(configs, "upgrade_magnet_values", ref c.UpgradeMagnetValues);

            TryGetInt(configs, "levelup_choices", ref c.LevelupChoices);

            if (configs.TryGetValue("levelup_pool", out string poolJson))
            {
                try
                {
                    var wrapper = JsonUtility.FromJson<LevelupPoolWrapper>("{\"items\":" + poolJson + "}");
                    if (wrapper?.items != null)
                        c.LevelupPool = new List<LevelupPoolEntry>(wrapper.items);
                }
                catch (System.Exception ex) { Debug.LogWarning($"Config parse error for levelup_pool: {ex.Message}"); }
            }

            return c;
        }

        private static void TryGetFloat(Dictionary<string, string> configs, string key, ref float value)
        {
            if (configs.TryGetValue(key, out string s) && float.TryParse(s,
                System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out float v))
                value = v;
        }

        private static void TryGetInt(Dictionary<string, string> configs, string key, ref int value)
        {
            if (configs.TryGetValue(key, out string s) && int.TryParse(s, out int v))
                value = v;
        }

        private static void TryGetBool(Dictionary<string, string> configs, string key, ref bool value)
        {
            if (configs.TryGetValue(key, out string s) && bool.TryParse(s, out bool v))
                value = v;
        }

        private static void TryGetIntArray(Dictionary<string, string> configs, string key, ref int[] value)
        {
            if (configs.TryGetValue(key, out string s))
            {
                try
                {
                    var wrapper = JsonUtility.FromJson<IntArrayWrapper>("{\"items\":" + s + "}");
                    if (wrapper?.items != null) value = wrapper.items;
                }
                catch (System.Exception ex) { Debug.LogWarning($"Config parse error for key: {ex.Message}"); }
            }
        }

        private static void TryGetFloatArray(Dictionary<string, string> configs, string key, ref float[] value)
        {
            if (configs.TryGetValue(key, out string s))
            {
                try
                {
                    var wrapper = JsonUtility.FromJson<FloatArrayWrapper>("{\"items\":" + s + "}");
                    if (wrapper?.items != null) value = wrapper.items;
                }
                catch (System.Exception ex) { Debug.LogWarning($"Config parse error for key: {ex.Message}"); }
            }
        }

        [Serializable] private class IntArrayWrapper { public int[] items; }
        [Serializable] private class FloatArrayWrapper { public float[] items; }
        [Serializable] private class LevelupPoolWrapper { public LevelupPoolEntry[] items; }
    }
}
