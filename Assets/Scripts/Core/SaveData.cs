using System;
using System.Collections.Generic;

namespace SeagullStorm
{
    [Serializable]
    public class SaveData
    {
        public int coins;
        public int highscore;
        public UpgradeData upgrades = new UpgradeData();
        public int totalRuns;
        public List<string> giftCodesRedeemed = new List<string>();

        public static SaveData CreateDefault()
        {
            return new SaveData
            {
                coins = 0,
                highscore = 0,
                upgrades = new UpgradeData(),
                totalRuns = 0,
                giftCodesRedeemed = new List<string>()
            };
        }
    }

    [Serializable]
    public class UpgradeData
    {
        public int speed;
        public int damage;
        public int hp;
        public int magnet;
    }
}
