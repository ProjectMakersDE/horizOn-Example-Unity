using System;
using System.Collections.Generic;
using UnityEngine;

namespace SeagullStorm.Data
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
        public float runDurationSeconds = 180f;
        public bool bossWaveEnabled = true;
        public int coinDivisor = 10;
        public float xpPerKillBase = 10f;
        public float xpLevelCurve = 1.4f;

        // Wave system
        public float waveIntervalSeconds = 15f;
        public int waveEnemyCountBase = 5;
        public float waveEnemyCountGrowth = 1.3f;
        public int waveBossHp = 500;

        // Enemies
        public float enemyCrabSpeed = 40f;
        public int enemyCrabHp = 30;
        public int enemyCrabDamage = 10;
        public int enemyCrabXp = 10;
        public float enemyJellyfishSpeed = 60f;
        public int enemyJellyfishHp = 50;
        public int enemyJellyfishDamage = 15;
        public int enemyJellyfishXp = 20;
        public float enemyPirateSpeed = 100f;
        public int enemyPirateHp = 40;
        public int enemyPirateDamage = 20;
        public int enemyPirateXp = 25;

        // Weapons
        public int weaponFeatherDamage = 20;
        public float weaponFeatherCooldown = 0.8f;
        public int weaponFeatherProjectiles = 1;
        public int weaponScreechDamage = 15;
        public float weaponScreechCooldown = 2.0f;
        public float weaponScreechRadius = 80f;
        public int weaponDiveDamage = 50;
        public float weaponDiveCooldown = 3.0f;
        public float weaponDiveRange = 120f;
        public int weaponGustDamage = 10;
        public float weaponGustCooldown = 2.5f;
        public float weaponGustKnockback = 60f;

        // Hub upgrades
        public int[] upgradeSpeedCosts = { 100, 300, 600, 1000 };
        public float[] upgradeSpeedValues = { 1.0f, 1.1f, 1.2f, 1.35f, 1.5f };
        public int upgradeSpeedMax = 4;
        public int[] upgradeDamageCosts = { 100, 300, 600, 1000 };
        public float[] upgradeDamageValues = { 1.0f, 1.15f, 1.3f, 1.5f, 1.8f };
        public int upgradeDamageMax = 4;
        public int[] upgradeHpCosts = { 150, 400, 800 };
        public float[] upgradeHpValues = { 100f, 130f, 170f, 220f };
        public int upgradeHpMax = 3;
        public int[] upgradeMagnetCosts = { 200, 500 };
        public float[] upgradeMagnetValues = { 50f, 80f, 120f };
        public int upgradeMagnetMax = 2;

        // Levelup
        public int levelupChoices = 3;
        public List<LevelupPoolEntry> levelupPool = new List<LevelupPoolEntry>();

        public void ApplyRemoteConfig(Dictionary<string, string> configs)
        {
            if (configs == null) return;

            TryGetFloat(configs, "run_duration_seconds", ref runDurationSeconds);
            TryGetBool(configs, "boss_wave_enabled", ref bossWaveEnabled);
            TryGetInt(configs, "coin_divisor", ref coinDivisor);
            TryGetFloat(configs, "xp_per_kill_base", ref xpPerKillBase);
            TryGetFloat(configs, "xp_level_curve", ref xpLevelCurve);

            TryGetFloat(configs, "wave_interval_seconds", ref waveIntervalSeconds);
            TryGetInt(configs, "wave_enemy_count_base", ref waveEnemyCountBase);
            TryGetFloat(configs, "wave_enemy_count_growth", ref waveEnemyCountGrowth);
            TryGetInt(configs, "wave_boss_hp", ref waveBossHp);

            TryGetFloat(configs, "enemy_crab_speed", ref enemyCrabSpeed);
            TryGetInt(configs, "enemy_crab_hp", ref enemyCrabHp);
            TryGetInt(configs, "enemy_crab_damage", ref enemyCrabDamage);
            TryGetInt(configs, "enemy_crab_xp", ref enemyCrabXp);
            TryGetFloat(configs, "enemy_jellyfish_speed", ref enemyJellyfishSpeed);
            TryGetInt(configs, "enemy_jellyfish_hp", ref enemyJellyfishHp);
            TryGetInt(configs, "enemy_jellyfish_damage", ref enemyJellyfishDamage);
            TryGetInt(configs, "enemy_jellyfish_xp", ref enemyJellyfishXp);
            TryGetFloat(configs, "enemy_pirate_speed", ref enemyPirateSpeed);
            TryGetInt(configs, "enemy_pirate_hp", ref enemyPirateHp);
            TryGetInt(configs, "enemy_pirate_damage", ref enemyPirateDamage);
            TryGetInt(configs, "enemy_pirate_xp", ref enemyPirateXp);

            TryGetInt(configs, "weapon_feather_damage", ref weaponFeatherDamage);
            TryGetFloat(configs, "weapon_feather_cooldown", ref weaponFeatherCooldown);
            TryGetInt(configs, "weapon_feather_projectiles", ref weaponFeatherProjectiles);
            TryGetInt(configs, "weapon_screech_damage", ref weaponScreechDamage);
            TryGetFloat(configs, "weapon_screech_cooldown", ref weaponScreechCooldown);
            TryGetFloat(configs, "weapon_screech_radius", ref weaponScreechRadius);
            TryGetInt(configs, "weapon_dive_damage", ref weaponDiveDamage);
            TryGetFloat(configs, "weapon_dive_cooldown", ref weaponDiveCooldown);
            TryGetFloat(configs, "weapon_dive_range", ref weaponDiveRange);
            TryGetInt(configs, "weapon_gust_damage", ref weaponGustDamage);
            TryGetFloat(configs, "weapon_gust_cooldown", ref weaponGustCooldown);
            TryGetFloat(configs, "weapon_gust_knockback", ref weaponGustKnockback);

            TryGetIntArray(configs, "upgrade_speed_costs", ref upgradeSpeedCosts);
            TryGetFloatArray(configs, "upgrade_speed_values", ref upgradeSpeedValues);
            TryGetInt(configs, "upgrade_speed_max", ref upgradeSpeedMax);
            TryGetIntArray(configs, "upgrade_damage_costs", ref upgradeDamageCosts);
            TryGetFloatArray(configs, "upgrade_damage_values", ref upgradeDamageValues);
            TryGetInt(configs, "upgrade_damage_max", ref upgradeDamageMax);
            TryGetIntArray(configs, "upgrade_hp_costs", ref upgradeHpCosts);
            TryGetFloatArray(configs, "upgrade_hp_values", ref upgradeHpValues);
            TryGetInt(configs, "upgrade_hp_max", ref upgradeHpMax);
            TryGetIntArray(configs, "upgrade_magnet_costs", ref upgradeMagnetCosts);
            TryGetFloatArray(configs, "upgrade_magnet_values", ref upgradeMagnetValues);
            TryGetInt(configs, "upgrade_magnet_max", ref upgradeMagnetMax);

            TryGetInt(configs, "levelup_choices", ref levelupChoices);

            if (configs.TryGetValue("levelup_pool", out string poolJson))
            {
                try
                {
                    var wrapper = JsonUtility.FromJson<LevelupPoolWrapper>("{\"items\":" + poolJson + "}");
                    if (wrapper?.items != null)
                        levelupPool = new List<LevelupPoolEntry>(wrapper.items);
                }
                catch { }
            }
        }

        private void TryGetFloat(Dictionary<string, string> configs, string key, ref float value)
        {
            if (configs.TryGetValue(key, out string s) && float.TryParse(s, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float v))
                value = v;
        }

        private void TryGetInt(Dictionary<string, string> configs, string key, ref int value)
        {
            if (configs.TryGetValue(key, out string s) && int.TryParse(s, out int v))
                value = v;
        }

        private void TryGetBool(Dictionary<string, string> configs, string key, ref bool value)
        {
            if (configs.TryGetValue(key, out string s) && bool.TryParse(s, out bool v))
                value = v;
        }

        private void TryGetIntArray(Dictionary<string, string> configs, string key, ref int[] value)
        {
            if (configs.TryGetValue(key, out string s))
            {
                try
                {
                    var wrapper = JsonUtility.FromJson<IntArrayWrapper>("{\"items\":" + s + "}");
                    if (wrapper?.items != null) value = wrapper.items;
                }
                catch { }
            }
        }

        private void TryGetFloatArray(Dictionary<string, string> configs, string key, ref float[] value)
        {
            if (configs.TryGetValue(key, out string s))
            {
                try
                {
                    var wrapper = JsonUtility.FromJson<FloatArrayWrapper>("{\"items\":" + s + "}");
                    if (wrapper?.items != null) value = wrapper.items;
                }
                catch { }
            }
        }

        [Serializable]
        private class IntArrayWrapper { public int[] items; }
        [Serializable]
        private class FloatArrayWrapper { public float[] items; }
        [Serializable]
        private class LevelupPoolWrapper { public LevelupPoolEntry[] items; }
    }
}
