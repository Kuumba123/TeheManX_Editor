using System.Collections.Generic;

namespace TeheManX_Editor
{
    public class ObjectSetting
    {
        public List<ObjectSlot> Slots { get; set; } = new List<ObjectSlot>();
    }
    public class ObjectSlot
    {
        public byte TileId { get; set; }
        public ushort VramAddress { get; set; }
        public ushort PaletteId { get; set; }
        public byte PaletteDestination { get; set; }
    }
}
