namespace TeheManX_Editor
{
    public class Checkpoint
    {
        public byte ObjectTileSetting { get; set; }
        public byte BackgroundTileSetting { get; set; }
        public byte BackgroundPaletteSetting { get; set; }
        public byte SilkShotType { get; set; }
        public ushort MegaX { get; set; }
        public ushort MegaY { get; set; }
        public ushort CameraX { get; set; }
        public ushort CameraY { get; set; }
        public ushort BG2X { get; set; }
        public ushort BG2Y { get; set; }
        public ushort BorderLeft {  get; set; }
        public ushort BorderRight { get; set; }
        public ushort BorderTop { get; set; }
        public ushort BorderBottom { get; set; }
        public ushort BG2X_Base { get; set; }
        public ushort BG2Y_Base { get; set; }
        public byte WramFlag { get; set; }
        public byte MegaFlip { get; set; }
        public byte CollisionTimer { get; set; }
    }
}
