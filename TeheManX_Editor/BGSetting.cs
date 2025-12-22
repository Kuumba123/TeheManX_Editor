using System.Collections.Generic;

namespace TeheManX_Editor
{
    public class BGSetting
    {
        public List<BGSlot> Slots { get; set; } = new List<BGSlot>();
    }
    public class BGSlot
    {
        public ushort Length { get; set; }
        public ushort VramAddress { get; set; }
        public int CpuAddress { get; set; }
        public ushort PaletteId { get; set; }
    }
}
