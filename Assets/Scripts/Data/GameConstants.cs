namespace SeagullStorm.Data
{
    public static class GameConstants
    {
        // Scene names
        public const string SceneTitle = "TitleScene";
        public const string SceneHub = "HubScene";
        public const string SceneRun = "RunScene";

        // Color palette (hex)
        public const string ColorOrange = "#D87943";
        public const string ColorTeal = "#527575";
        public const string ColorDarkBG = "#1A1A2E";
        public const string ColorLightText = "#EEEEEE";
        public const string ColorDarkText = "#1C1C1C";
        public const string ColorSand = "#F2D2A9";
        public const string ColorWater = "#3B7DD8";
        public const string ColorSeagullWhite = "#F5F5F0";
        public const string ColorCrabRed = "#E05B4B";
        public const string ColorJellyfishPurple = "#9B59B6";
        public const string ColorPirateDark = "#4A4A4A";
        public const string ColorXPGold = "#FFD700";

        // Base resolution
        public const int BaseWidth = 480;
        public const int BaseHeight = 270;
        public const int RenderScale = 4;

        // Default gameplay values (overridden by Remote Config)
        public const float DefaultRunDuration = 180f;
        public const int DefaultCoinDivisor = 10;
        public const float DefaultWaveInterval = 15f;
        public const int DefaultWaveEnemyCountBase = 5;
        public const float DefaultWaveEnemyCountGrowth = 1.3f;
        public const float DefaultXpPerKillBase = 10f;
        public const float DefaultXpLevelCurve = 1.4f;

        // Player defaults
        public const float DefaultPlayerSpeed = 200f;
        public const float DefaultPlayerHP = 100f;
        public const float DefaultPickupRadius = 50f;
    }
}
