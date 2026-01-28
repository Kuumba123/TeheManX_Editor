using System.Collections.Generic;
using System.Linq;

namespace TeheManX_Editor
{
    public class ObjectSetting
    {
        public List<ObjectSlot> Slots { get; set; } = new();

        public ObjectSetting() { }

        public ObjectSetting(ObjectSetting other)
        {
            Slots = other.Slots.Select(slot => new ObjectSlot(slot)).ToList();
        }
    }
    public class ObjectSlot
    {
        public byte TileId { get; set; }
        public ushort VramAddress { get; set; }
        public ushort PaletteId { get; set; }
        public byte PaletteDestination { get; set; }

        public ObjectSlot()
        {
        }

        public ObjectSlot(ObjectSlot other)
        {
            TileId = other.TileId;
            VramAddress = other.VramAddress;
            PaletteId = other.PaletteId;
            PaletteDestination = other.PaletteDestination;
        }
    }

}
