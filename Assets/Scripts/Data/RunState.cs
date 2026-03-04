using System.Collections.Generic;
using UnityEngine;

namespace SeagullStorm.Data
{
    public class RunState
    {
        public int currentScore;
        public int currentWave;
        public int currentLevel;
        public float timeRemaining;
        public float playerHP;
        public float playerMaxHP;
        public Vector2 playerPosition;
        public List<string> activeWeapons = new List<string>();
        public int kills;
        public int xpCollected;
        public float xpCurrent;
        public float xpToNextLevel;
        public float duration;
        public int consecutiveWave1Deaths;

        public void Reset(float maxHP, float runDuration)
        {
            currentScore = 0;
            currentWave = 0;
            currentLevel = 1;
            timeRemaining = runDuration;
            playerHP = maxHP;
            playerMaxHP = maxHP;
            playerPosition = Vector2.zero;
            activeWeapons.Clear();
            activeWeapons.Add("feather");
            kills = 0;
            xpCollected = 0;
            xpCurrent = 0;
            xpToNextLevel = 20f;
            duration = 0f;
        }
    }
}
