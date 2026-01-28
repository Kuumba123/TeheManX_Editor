namespace TeheManX_Editor
{
    class Layout
    {
        public static readonly string CurrentVersion = "1.0";
        public string Version { get; set; }
        public int MainWindowX { get; set; }
        public int MainWindowY { get; set; }
        public int MainWindowWidth { get; set; }
        public int MainWindowHeight { get; set; }
        public int MainWindowState { get; set; }
        public string DockJson { get; set; }
        //Layout Viewer
        public int LayoutLeft { get; set; }
        public int LayoutTop { get; set; }
        public double LayoutWidth { get; set; }
        public double LayoutHeight { get; set; }
        public int LayoutState { get; set; }
        //Palette Tools
        public double PickerLeft { get; set; }
        public double PickerTop { get; set; }
        //Tools Window
        public bool MegaManXOpen { get; set; }
        public bool MegaManX2Open { get; set; }
        public bool MegaManX3Open { get; set; }
        //Vram Tiles for 16x16 and Palette Tab
        public double ScaleVram { get; set; }
        public double ScaleVram2 { get; set; }
        //Enemy Viewer
        public double ScaleEnemy { get; set; }
        //VRAM Tiles Viewer
        public double ScaleObjectVram { get; set; }
        public bool UseRomOffset { get; set; }
        public bool RefreshBackground { get; set; }
        public bool RefreshObject { get; set; }
    }
}
