using System.Collections.Generic;
using UnityEngine;

namespace SeagullStorm
{
    public class RunState
    {
        public int score;
        public int wave;
        public int level = 1;
        public float timeRemaining;
        public float playerHP;
        public float playerMaxHP;
        public Vector2 playerPosition;
        public float moveSpeedMultiplier = 1f;
        public float pickupRadiusMultiplier = 1f;
        public List<string> activeWeapons = new List<string>();
        public int kills;
        public int xpCollected;
        public float xpCurrent;
        public float xpToNextLevel = 20f;
        public float duration;
        public int coinsEarned;

        public void Reset(float maxHP, float runDuration)
        {
            score = 0;
            wave = 0;
            level = 1;
            timeRemaining = runDuration;
            playerHP = maxHP;
            playerMaxHP = maxHP;
            playerPosition = Vector2.zero;
            moveSpeedMultiplier = 1f;
            pickupRadiusMultiplier = 1f;
            activeWeapons.Clear();
            activeWeapons.Add("feather");
            kills = 0;
            xpCollected = 0;
            xpCurrent = 0;
            xpToNextLevel = 20f;
            duration = 0f;
            coinsEarned = 0;
        }

        public string FormatDuration()
        {
            int m = Mathf.FloorToInt(duration / 60f);
            int s = Mathf.FloorToInt(duration % 60f);
            return $"{m}m{s:D2}s";
        }
    }
}
