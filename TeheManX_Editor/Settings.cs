namespace TeheManX_Editor
{
    class Settings
    {
        #region Properties
        public string EmuPath { get; set; }
        public bool SaveOnTest { get; set; }
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
        #endregion Methods
    }
}
