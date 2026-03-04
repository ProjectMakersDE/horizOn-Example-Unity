using System;
using System.Collections.Generic;

namespace SeagullStorm.Data
{
    [Serializable]
    public class SaveData
    {
        public int coins;
        public int highscore;
        public UpgradeData upgrades = new UpgradeData();
        public int totalRuns;
        public List<string> giftCodesRedeemed = new List<string>();
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
