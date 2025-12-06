namespace TeheManX_Editor
{
    class Settings
    {
        #region Properties
        public string EmuPath { get; set; }
        public bool SaveOnTest { get; set; }
        public int ReferanceWidth { get; set; }
        public bool DontUpdate { get; set; }
        public bool InvertSpeed { get; set; } //For Enemy Tab
        public bool UseFixedScale { get; set; }
        public double LayoutScale { get; set; }
        public double LayoutScreenScale { get; set; }
        public double ScreenScale { get; set; }
        public double ScreenTilesScale { get; set; }
        public double Tile32Scale { get; set; }
        public double Tile32Image16Scale { get; set; }
        public double Tile16Scale { get; set; }
        public bool EnemyFixedScale { get; set; }
        #endregion Properties

        #region Constructors
        public Settings()
        {

        }
        #endregion Constructors

        #region Methods
        public static Settings SetDefaultSettings()
        {
            Settings s = new Settings();
            s.EmuPath = "";
            s.SaveOnTest = true;

            s.LayoutScale = 1.0;
            s.LayoutScreenScale = 1.0;
            s.ScreenScale = 1.0;
            s.ScreenTilesScale = 1.0;
            s.Tile32Scale = 1.0;
            s.Tile32Image16Scale = 1.0;
            s.Tile16Scale = 1.0;
            return s;
        }
        public static bool IsPastVersion(string ver)
        {
            foreach (var v in Const.PastVersions)
            {
                if (v == ver)
                    return true;
            }
            return false;
        }
        #endregion Methods
    }
}
