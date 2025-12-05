namespace TeheManX_Editor
{
    class Settings
    {
        #region Properties
        public string EmuPath { get; set; }
        public bool SaveOnTest { get; set; }
        public int ReferanceWidth { get; set; }
        public bool DontUpdate { get; set; }
        public bool InvertSpeed { get; set; }
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
