using System.Collections.Generic;

namespace TeheManX_Editor;
public class PaletteAnime
{
    // Header Data
    public byte ColorIndex { get; set; }
    public byte ColorCount { get; set; }

    // Frame Data
    public List<PaletteFrame> Frames { get; set; } = new List<PaletteFrame>();
    public class PaletteFrame
    {
        // 0x01-0xFF: Time to wait. 0x00: Trigger Loop command.
        public byte Timer { get; set; }

        // The 16-bit SNES address where the color data is stored
        public ushort ColorPointer { get; set; }
    }
    public int LoopIndex;
    public ushort LeftSide { get; set; }
    public ushort RightSide { get; set; }
    public ushort BottomSide { get; set; }
    public ushort TopSide { get; set; }
}
