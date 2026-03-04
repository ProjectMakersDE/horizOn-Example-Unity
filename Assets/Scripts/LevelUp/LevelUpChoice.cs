namespace SeagullStorm
{
    /// <summary>
    /// Data struct for a single level-up choice.
    /// </summary>
    public struct LevelUpChoice
    {
        public string id;
        public string type;          // "weapon_new", "weapon_upgrade", "stat_boost"
        public string weaponId;
        public string displayName;
        public string description;
    }
}
