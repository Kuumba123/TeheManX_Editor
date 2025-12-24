using System.Collections.Generic;
using System.Linq;

namespace TeheManX_Editor
{
    public class BGSetting
    {
        public List<BGSlot> Slots { get; set; } = new();
        public BGSetting()
        {
        }
        public BGSetting(BGSetting other)
        {
            Slots = other.Slots.Select(slot => new BGSlot(slot)).ToList();
        }
    }
    public class BGSlot
    {
        public ushort Length { get; set; }
        public ushort VramAddress { get; set; }
        public int CpuAddress { get; set; }
        public ushort PaletteId { get; set; }

        public BGSlot()
        {
        }
        public BGSlot(BGSlot other)
        {
            Length = other.Length;
            VramAddress = other.VramAddress;
            CpuAddress = other.CpuAddress;
            PaletteId = other.PaletteId;
        }
    }
}
