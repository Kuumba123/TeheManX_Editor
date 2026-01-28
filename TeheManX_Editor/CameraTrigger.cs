using System.Collections.Generic;

namespace TeheManX_Editor
{
    public class CameraTrigger
    {
        public ushort RightSide { get; set; }
        public ushort LeftSide { get; set; }
        public ushort BottomSide { get; set; }
        public ushort TopSide { get; set; }
        public List<byte> BorderSettings { get; set; } = new();
        public CameraTrigger()
        {
        }
        public CameraTrigger(CameraTrigger other)
        {
            RightSide = other.RightSide;
            LeftSide = other.LeftSide;
            BottomSide = other.BottomSide;
            TopSide = other.TopSide;
            BorderSettings = new List<byte>(other.BorderSettings);
        }
    }
}
