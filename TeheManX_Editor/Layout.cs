using System;
using System.Collections.Generic;
using System.Windows;

namespace TeheManX_Editor
{
    class Layout
    {
        public WindowLayout MainWindowLayout { get; set; }
        public List<WindowLayout> WindowLayouts { get; set; }
        //Layout Viewer
        public double LayoutLeft { get; set; }
        public double LayoutTop { get; set; }
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
        public double ScaleObjectVram { get; set ; }
        public class WindowLayout
        {
            public double Left { get; set; }
            public double Top { get; set; }
            public double Width { get; set; }
            public double Height { get; set; }
            public bool Max { get; set; }
            public int WindowState { get; set; }
            public string Type { get; set; }
            public object Child { get; set; }
        }
        public class BranchLayout
        {
            public Type FirstItemType { get; set; }
            public object FirstItem { get; set; }
            public GridLength FirstItemLength { get; set; }

            public string SecondItemType { get; set; }
            public object SecondItem { get; set; }
            public GridLength SecondItemLength { get; set; }

            public int Orientation;
        }
    }
}
