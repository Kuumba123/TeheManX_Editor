using System.Collections.Generic;
using System.Linq;

namespace TeheManX_Editor
{
    public class BGPalette
    {
        public List<BGPaletteSlot> Slots { get; set; } = new();
        public BGPalette()
        {
        }
        public BGPalette(BGPalette other)
        {
            Slots = other.Slots.Select(slot => new BGPaletteSlot(slot)).ToList();
        }
    }
    public class BGPaletteSlot
    {
        public ushort Address { get; set; }
        public byte ColorIndex { get; set; }

        public BGPaletteSlot()
        {
        }
        public BGPaletteSlot(BGPaletteSlot other)
        {
            Address = other.Address;
            ColorIndex = other.ColorIndex;
        }
    }
}
