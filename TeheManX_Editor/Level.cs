using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace TeheManX_Editor
{
    static class Level
    {
        #region Fields
        public static int Id = 0;
        public static int BG = 0;
        public static byte[] Tiles = new byte[0x8000]; //Includes Filler Tiles
        public static byte[,,] Layout = new byte[Const.MaxLevels, 2, 0x400];
        public static Color[,] Palette = new Color[8, 16]; //Converted to 24-bit Color
        public static List<Enemy>[] Enemies = new List<Enemy>[Const.MaxLevels];
        #endregion Fields

        #region Methods
        public static void LoadLevelData()
        {
            /*NOTE: this code is too make defining the Constants much quicker so dont delete this*/
#if false
            for (int i = 0; i < Const.LevelsCount; i++)
            {
                for (int l = 0; l < 1; l++)
                {
                    ushort max32x = 0;
                    ushort max16x = 0;

                    byte screenCount = SNES.rom[SNES.CpuToOffset(BitConverter.ToInt32(SNES.rom, Const.LayoutPointersOffset[l] + i * 3)) + 2];

                    int offset = SNES.CpuToOffset(BitConverter.ToInt32(SNES.rom, Const.ScreenDataPointersOffset[l] + i * 3));
                    for (int t = 0; t < (screenCount * 64); t++)
                    {
                        ushort id = BitConverter.ToUInt16(SNES.rom, offset + (t * 2));
                        if (id == 0xFFFF)
                        {
                            System.Diagnostics.Debug.WriteLine("Max 16-bit int hitted");
                        }
                        if (id > max32x)
                            max32x = id;
                    }

                    offset = SNES.CpuToOffset(BitConverter.ToInt32(SNES.rom, Const.Tile32DataPointersOffset[l] + i * 3));
                    for (int t = 0; t < (max32x + 1); t++)
                    {
                        ushort id = BitConverter.ToUInt16(SNES.rom, offset + (t * 8));
                        ushort id2 = BitConverter.ToUInt16(SNES.rom, offset + (t * 8) + 2);
                        ushort id3 = BitConverter.ToUInt16(SNES.rom, offset + (t * 8) + 4);
                        ushort id4 = BitConverter.ToUInt16(SNES.rom, offset + (t * 8) + 6);

                        //For Ignoring specific 32x32 tiles

                        if (i == 9 && l == 0)
                        {
                            if (t == 0xD8 || t == 0x111)
                            {
                                continue;
                            }
                        }


                        if (id > max16x)
                            max16x = id;
                        else if (id2 > max16x)
                            max16x = id2;
                        else if (id3 > max16x)
                            max16x = id3;
                        else if (id4 > max16x)
                            max16x = id4;

                        if (id == 0x7CE)
                        {
                            System.Diagnostics.Debug.WriteLine("Hitted Very High ID");
                        }

                        if (id == 0xFFFF || id2 == 0xFFFF || id3 == 0xFFFF || id4 == 0xFFFF)
                        {
                            System.Diagnostics.Debug.WriteLine("Max 16-bit int hitted");
                        }
                    }
                    if (l == 0)
                        System.Diagnostics.Debug.WriteLine($"Stage - 0x{i:X2} BG - {l} Tile Count is 0x{max16x + 1:X} ");
                }
            }
#endif
            Id = 0;
            BG = 0;
            LoadLayouts();
            LoadEnemyData();
        }
        public static unsafe void Draw16xTile(int id, int x, int y, int stride, IntPtr dest)
        {
            int offset = SNES.CpuToOffset(BitConverter.ToInt32(SNES.rom, Const.Tile16DataPointersOffset[BG] + Id * 3)) + id * 8;
            byte* buffer = (byte*)dest;

            Color backColor = Palette[0, 0];

            for (int i = 0; i < 4; i++)
            {
                ushort val = BitConverter.ToUInt16(SNES.rom, offset + i * 2);
                int tileOffset = (val & 0x3FF) * 0x20; // 32 bytes per tile
                int set = (val >> 10) & 7;             // Palette index

                bool flipH = (val & 0x4000) != 0;
                bool flipV = (val & 0x8000) != 0;

                // Top-left of this 8x8 subtile in destination
                int destBase = (x + ((i & 1) * 8)) * 3 + (y + ((i >> 1) * 8)) * stride;

                for (int row = 0; row < 8; row++)
                {
                    int base1 = tileOffset + (row * 2);
                    int base2 = tileOffset + 0x10 + (row * 2);

                    // Apply vertical flipping when calculating destination Y
                    int destY = flipV ? (7 ^ row) : row;

                    for (int col = 0; col < 8; col++)
                    {
                        int bit = 7 - col; // leftmost pixel = bit7
                        int p0 = (Tiles[base1] >> bit) & 1;
                        int p1 = (Tiles[base1 + 1] >> bit) & 1;
                        int p2 = (Tiles[base2] >> bit) & 1;
                        int p3 = (Tiles[base2 + 1] >> bit) & 1;

                        byte index = (byte)(p0 | (p1 << 1) | (p2 << 2) | (p3 << 3));

                        // Apply horizontal flipping when calculating destination X
                        int destX = flipH ? (7 ^ col) : col;

                        int destIndex = destBase + (destY * stride) + (destX * 3);

                        Color color = index == 0 ? backColor : Palette[set, index];

                        buffer[destIndex + 0] = color.R;
                        buffer[destIndex + 1] = color.G;
                        buffer[destIndex + 2] = color.B;
                    }
                }
            }
        }
        public static unsafe void DrawScreen(int s, int stride, IntPtr ptr)
        {
            int offset = SNES.CpuToOffset(BitConverter.ToInt32(SNES.rom, Const.ScreenDataPointersOffset[BG] + Id * 3)) + s * 0x80;
            int tile32Offset = SNES.CpuToOffset(BitConverter.ToInt32(SNES.rom, Const.Tile32DataPointersOffset[BG] + Id * 3));

            for (int y = 0; y < 8; y++)
            {
                for (int x = 0; x < 8; x++)
                {
                    ushort tileId32 = BitConverter.ToUInt16(SNES.rom, offset + (x * 2) + (y * 16));

                    Draw16xTile(BitConverter.ToUInt16(SNES.rom, tile32Offset + (tileId32 * 8)), x * 32, y * 32, stride, ptr);
                    Draw16xTile(BitConverter.ToUInt16(SNES.rom, tile32Offset + (tileId32 * 8) + 2), x * 32 + 16, y * 32, stride, ptr);
                    Draw16xTile(BitConverter.ToUInt16(SNES.rom, tile32Offset + (tileId32 * 8) + 4), x * 32, y * 32 + 16, stride, ptr);
                    Draw16xTile(BitConverter.ToUInt16(SNES.rom, tile32Offset + (tileId32 * 8) + 6), x * 32 + 16, y * 32 + 16, stride, ptr);
                }
            }
        }
        public static unsafe void DrawScreen(int s,int drawX,int drawY, int stride, IntPtr ptr)
        {
            int offset = SNES.CpuToOffset(BitConverter.ToInt32(SNES.rom, Const.ScreenDataPointersOffset[BG] + Id * 3)) + s * 0x80;
            int tile32Offset = SNES.CpuToOffset(BitConverter.ToInt32(SNES.rom, Const.Tile32DataPointersOffset[BG] + Id * 3));

            for (int y = 0; y < 8; y++)
            {
                for (int x = 0; x < 8; x++)
                {
                    ushort tileId32 = BitConverter.ToUInt16(SNES.rom, offset + (x * 2) + (y * 16));

                    Draw16xTile(BitConverter.ToUInt16(SNES.rom, tile32Offset + (tileId32 * 8)), x * 32 + drawX, y * 32 + drawY, stride, ptr);
                    Draw16xTile(BitConverter.ToUInt16(SNES.rom, tile32Offset + (tileId32 * 8) + 2), x * 32 + 16 + drawX, y * 32 + drawY, stride, ptr);
                    Draw16xTile(BitConverter.ToUInt16(SNES.rom, tile32Offset + (tileId32 * 8) + 4), x * 32 + drawX, y * 32 + 16 + drawY, stride, ptr);
                    Draw16xTile(BitConverter.ToUInt16(SNES.rom, tile32Offset + (tileId32 * 8) + 6), x * 32 + 16 + drawX, y * 32 + 16 + drawY, stride, ptr);
                }
            }
        }
        private static void LoadLayouts()
        {
            //Pre Clear the Layout
            for (int l = 0; l < 2; l++)
            {
                for (int i = 0; i < Const.LevelsCount; i++)
                {
                    for (int b = 0; b < 0x400; b++)
                    {
                        Layout[i, l, b] = 0;
                    }
                }
            }
            byte[] temp = new byte[0x800];
            for (int l = 0; l < 2; l++)
            {
                for (int i = 0; i < Const.LevelsCount; i++)
                {
                    int infoOffset = SNES.CpuToOffset(BitConverter.ToInt32(SNES.rom, Const.LayoutPointersOffset[l] + i * 3));

                    Array.Clear(temp, 0, temp.Length);
                    int destIndex = 0;
                    byte controlB;
                    int count;
                    int flags;

                    //Copy 3 byte header
                    temp[0] = SNES.rom[infoOffset];     //width
                    temp[1] = SNES.rom[infoOffset + 1]; //height
                    temp[2] = SNES.rom[infoOffset + 2]; //screen count (not needed for layout but is nice to know)
                    infoOffset += 3;

                    while (true)
                    {
                        controlB = SNES.rom[infoOffset];
                        infoOffset++;

                        if (controlB == 0xFF)
                            break;

                        flags = controlB;
                        count = controlB & 0x7F;

                        controlB = SNES.rom[infoOffset];
                        infoOffset++;

                        //Write Loop
                        while (count != 0)
                        {
                            count--;

                            temp[destIndex + 3] = controlB;
                            destIndex++;

                            if ((flags & 0x80) == 0)
                                controlB++;
                        }
                    }

                    infoOffset = 0;
                    destIndex = 0;
                    byte width = temp[0];
                    byte height = temp[1];
                    infoOffset += 3;

                    while (height != 0)
                    {
                        height--;
                        count = width;

                        int destTemp = destIndex;

                        while (count != 0)
                        {
                            count--;
                            Layout[i, l, destTemp] = temp[infoOffset];
                            destTemp++;
                            infoOffset++;
                        }
                        destIndex += 0x20;
                    }

                }
            }
        }
        static void GetLayoutDimensions(byte[] layout, out byte width, out byte height)
        {
            int usedRight = 0;
            int usedBottom = 0;

            for (int y = 0; y < 32; y++)
            {
                for (int x = 0; x < 32; x++)
                {
                    if (layout[y * 32 + x] != 0)
                    {
                        if (x + 1 > usedRight) usedRight = x + 1;
                        if (y + 1 > usedBottom) usedBottom = y + 1;
                    }
                }
            }

            width = (byte)Math.Max(1, usedRight);
            height = (byte)Math.Max(1, usedBottom);
        }
        static byte[] CompressLayout(byte[] layout, byte screenCount)
        {
            GetLayoutDimensions(layout, out byte width, out byte height);

            var compressed = new List<byte>(0x100);
            compressed.Add(width);
            compressed.Add(height);
            compressed.Add(screenCount);

            int stride = 32;
            List<byte> activeArea = new List<byte>(width * height);
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                    activeArea.Add(layout[y * stride + x]);

            var data = activeArea.ToArray();
            int total = data.Length;
            int i = 0;

            while (i < total)
            {
                byte start = data[i];
                int repeatCount = 1;
                int incCount = 1;

                // Measure repeat and increment runs
                while (i + repeatCount < total &&
                       data[i + repeatCount] == start &&
                       repeatCount < 0x7F)
                    repeatCount++;

                while (i + incCount < total &&
                       data[i + incCount] == (byte)(data[i + incCount - 1] + 1) &&
                       incCount < 0x7F)
                    incCount++;

                // Prefer repeat when tied
                bool useRepeat = repeatCount >= incCount;
                int runLength = useRepeat ? repeatCount : incCount;

                // use real width for column wrapping
                int col = i % width;

                // Only skip zeros that fall outside the active width (padding)
                if (useRepeat && start == 0 && runLength <= 3 && (col + runLength) > width)
                {
                    i += runLength;
                    continue;
                }

                byte control = (byte)runLength;
                if (useRepeat)
                    control |= 0x80;

                compressed.Add(control);
                compressed.Add(start);

                i += runLength;
            }

            compressed.Add(0xFF);
            return compressed.ToArray();
        }
        public static bool SaveLayouts()
        {
            byte[] layout = new byte[0x400];
            for (int l = 0; l < 2; l++)
            {
                for (int i = 0; i < Const.LevelsCount; i++)
                {
                    for (int d = 0; d < 0x400; d++)
                        layout[d] = Layout[i, l, d];

                    if (Const.LayoutLength[i, l] == 0) break;

                    byte[] compressedLayout = CompressLayout(layout, (byte)Const.ScreenCount[i, l]);

                    if (compressedLayout.Length > Const.LayoutLength[i, l])
                    {
                        MessageBox.Show($"The Layout for Stage {i:X2} Layer {l + 1} is too large ({compressedLayout.Length:X} bytes). The max length is {Const.LayoutLength[i, l]:X} bytes!","ERROR");
                        return false;
                    }
                    //Save Layout to Rom
                    int offset = SNES.CpuToOffset(BitConverter.ToInt32(SNES.rom, Const.LayoutPointersOffset[l] + i * 3));
                    Array.Copy(compressedLayout, 0, SNES.rom, offset, compressedLayout.Length);
                }
            }
            return true;
        }
        private static void LoadEnemyData()
        {
            for (int i = 0; i < Const.LevelsCount; i++)
            {
                if (Enemies[i] != null)
                    Enemies[i].Clear();
                else
                    Enemies[i] = new List<Enemy>();
            }

            int stage = 0;
            try
            {
                for (int i = 0; i < Const.PlayabledLevelsCount; i++)
                {
                    stage = i;
                    Enemies[i].Clear();
                    //Get Address of Enemy Data
                    int addr = SNES.CpuToOffset(BitConverter.ToInt32(SNES.rom, Const.EnemyPointersOffset + (i * 2)) , Const.EnemyDataBank);
                    //Get Spawn Cam Byte
                    byte column = SNES.rom[addr];
                    if (column == 0xFF) // No Enemies in this stage
                        continue;
                    addr++;
                    while (true)
                    {
                        var t = Enemies[i].Count;
                        Enemies[i].Add(new Enemy());

                        if (Enemies[i].Count == 0xCC /* Max Amount of Enemies*/)
                        {
                            MessageBox.Show("Incorrect Enemy Data Format \nfor stage " + Convert.ToString(i, 16));
                            Application.Current.Shutdown();
                        }

                        //Assign Type
                        Enemies[i][t].Column = column;
                        Enemies[i][t].Type = SNES.rom[addr];
                        //Assign Y
                        Enemies[i][t].Y = (short)(BitConverter.ToUInt16(SNES.rom, addr + 1) & 0x7FFF);
                        //Assign Id & Sub Id
                        Enemies[i][t].Id = SNES.rom[addr + 3];
                        Enemies[i][t].SubId = SNES.rom[addr + 4];
                        //Assign X
                        Enemies[i][t].X = (short)(BitConverter.ToUInt16(SNES.rom, addr + 5) & 0x7FFF);

                        // Check X high byte
                        if ((SNES.rom[addr + 6] & 0x80) == 0)
                            addr += 7;
                        else
                        {
                            addr += 7;
                            if (SNES.rom[addr] == column)  // end of enemy data
                                break;
                            column = SNES.rom[addr];
                            addr++;
                        }
                    }
                }
            }catch(Exception e)
            {
                MessageBox.Show($"Stage {stage:X}  Enemy Data Corrupted?\n" + e.Message, "ERROR");
                Application.Current.Shutdown();
            }
        }
        public static bool SaveEnemyData()
        {
            for (int id = 0; id < Const.PlayabledLevelsCount; id++)
            {
                List<Enemy> sorted = Enemies[id].OrderBy(e => e.Column).ToList();

                MemoryStream ms = new MemoryStream(0x660);
                BinaryWriter bw = new BinaryWriter(ms);

                // If no enemies write FF and skip
                if (sorted.Count == 0)
                {
                    SNES.rom[SNES.CpuToOffset(BitConverter.ToInt32(SNES.rom, Const.EnemyPointersOffset + (id * 2)),Const.EnemyDataBank)] = 0xFF;
                    continue;
                }

                byte column = sorted[0].Column;
                bw.Write(column); // Write initial column byte

                for (int i = 0; i < sorted.Count; i++)
                {
                    bw.Write(sorted[i].Type);
                    bw.Write(sorted[i].Y);
                    bw.Write(sorted[i].Id);
                    bw.Write(sorted[i].SubId);

                    if (i == (sorted.Count - 1)) // Last Enemy
                    {
                        bw.Write((ushort)(sorted[i].X | 0x8000)); // Set high byte to mark end of data
                        bw.Write(column); // Write final column byte
                    }
                    else
                    {
                        if (column != sorted[i + 1].Column)
                        {
                            bw.Write((ushort)(sorted[i].X | 0x8000)); // Set high byte to mark end of data
                            column = sorted[i + 1].Column;
                            bw.Write(column); // Write new column byte
                        }
                        else
                            bw.Write(sorted[i].X);
                    }
                }

                // Final size check
                if (ms.Length > Const.EnemiesLength[id])
                {
                    MessageBox.Show(
                        $"Enemy Data for Stage {id:X2} too large ({ms.Length:X}). Max {Const.EnemiesLength[id]:X}",
                        "ERROR"
                    );
                    return false;
                }

                // Get offset from ROM
                int offset = SNES.CpuToOffset(
                    BitConverter.ToInt32(SNES.rom, Const.EnemyPointersOffset + (id * 2)),
                    Const.EnemyDataBank
                );

                Array.Copy(ms.ToArray(), 0, SNES.rom, offset, ms.Length);

                bw.Close();
                ms.Close();
            }

            return true;
        }
        public static void AssignPallete()
        {
            if (Id < Const.PlayabledLevelsCount)
            {
                int id;
                if (Const.Id == Const.GameId.MegaManX3 && Id == 0xE)
                    id = 0xB; //special case for MMX3 rekt version of dophler 2
                else
                    id = Id;

                int infoOffset = SNES.CpuToOffset(BitConverter.ToUInt16(SNES.rom, Const.PaletteInfoOffset + id * 2 + Const.PaletteStageBase), Const.PaletteBank);

                while (SNES.rom[infoOffset] != 0)
                {
                    int colorCount = SNES.rom[infoOffset]; //how many colors are going to be dumped
                    byte colorIndex = SNES.rom[infoOffset + 3]; //which color index to start dumping at
                    int colorOffset = SNES.CpuToOffset(BitConverter.ToUInt16(SNES.rom, infoOffset + 1) + (Const.PaletteColorBank << 16)); //where the colors are located

                    for (int c = 0; c < colorCount; c++)
                    {
                        if ((colorIndex + c) > 0x7F)
                            return;

                        ushort color = BitConverter.ToUInt16(SNES.rom, colorOffset + c * 2);
                        byte R = (byte)(color % 32 * 8);
                        byte G = (byte)(color / 32 % 32 * 8);
                        byte B = (byte)(color / 1024 % 32 * 8);

                        Palette[((colorIndex + c) >> 4) & 0xF, (colorIndex + c) & 0xF] = Color.FromRgb(R, G, B);
                    }
                    infoOffset += 4;
                }
            }
            else
            {
                for (int s = 0; s < 8; s++)
                {
                    for (int i = 0; i < 16; i++) // 0x00 → 0xF8
                    {
                        byte shade = (byte)(((uint)(i * 0x10) >> 3) << 3);

                        Palette[s, i] = Color.FromRgb(shade, shade, shade);
                    }
                }
            }
        }
        public static void DecompressLevelTiles() //TODO: should probably check vram address , also get user to submit Tiles+PAL if non playable level
        {
            Array.Copy(Const.VRAM_B, 0, Tiles, 0, 0x200);
            Array.Clear(Tiles, 0x200, Tiles.Length - 0x200);

            if (Id >= Const.PlayabledLevelsCount)
                return;

            int id;
            if (Const.Id == Const.GameId.MegaManX3 && Id == 0xE)
                id = 0xB; //special case for MMX3 rekt version of dophler 2
            else
                id = Id;

            int infoOffset = SNES.CpuToOffset(BitConverter.ToUInt16(SNES.rom, Const.LoadTileSetInfoOffset + id * 2 + Const.LoadTileSetStageBase), Const.LoadTileSetBank);

            if (Const.Id == Const.GameId.MegaManX) // compression algorithem just for MMX
            {
                //TODO: not really sure what I should do about this...
                if (Id == 0)
                    Array.Copy(SNES.rom, 0x15AAE0, Tiles, 0x4A00, 0x3600); //Extra CHR loaded by thread

                int addr_W = 0x200;
                int controlB;
                byte copyB;

                if (SNES.rom[infoOffset] != 0xFF)
                {
                    int compressId = SNES.rom[infoOffset]; //which compressed tile Id to load
                    int vramOffset = BitConverter.ToUInt16(SNES.rom, infoOffset + 3) & 0x7FFF; //where in vram to diump the tiles
                    ushort size = BitConverter.ToUInt16(SNES.rom, compressId * 5 + Const.CompressedTileInfoOffset);
                    size = (ushort)((size + 7) >> 3);
                    int addr_R = SNES.CpuToOffset(BitConverter.ToInt32(SNES.rom, (compressId * 5) + Const.CompressedTileInfoOffset + 2));
                    try
                    {
                        while (size != 0)
                        {
                            controlB = SNES.rom[addr_R];
                            addr_R++;
                            copyB = SNES.rom[addr_R];
                            addr_R++;
                            for (int i = 0; i < 8; i++)
                            {
                                controlB <<= 1;
                                if ((controlB & 0x100) != 0x100)
                                {
                                    Tiles[addr_W] = copyB;
                                    addr_W++;
                                }
                                else
                                {
                                    Tiles[addr_W] = SNES.rom[addr_R];
                                    addr_R++;
                                    addr_W++;
                                }
                            }
                            size--;
                        }
                    }
                    catch (Exception e)
                    {
                        MessageBox.Show($"Error happened when decompress - {compressId:X}" + " Tile Graphics" + e.Message + "\nCorrupted ROM ?", "ERROR");
                        Application.Current.Shutdown();
                    }
                }
            }
            else // compression algorithem for MMX2 and MMX3
            {
                if (SNES.rom[infoOffset] != 0xFF)
                {
                    int compressId = SNES.rom[infoOffset]; //which compressed tile Id to load
                    int vramOffset = BitConverter.ToUInt16(SNES.rom, infoOffset + 3) & 0x7FFF; //where in vram to diump the tiles

                    int addr_R = SNES.CpuToOffset(BitConverter.ToInt32(SNES.rom, compressId * 5 + Const.CompressedTileInfoOffset));
                    int size = BitConverter.ToUInt16(SNES.rom, compressId * 5 + Const.CompressedTileInfoOffset + 3);
                    int addr_W = 0x200;

                    try
                    {
                        byte controlB = SNES.rom[addr_R];
                        addr_R++;
                        byte controlC = 8;

                        while (true)
                        {
                            if ((controlB & 0x80) == 0)
                            {
                                Tiles[addr_W] = SNES.rom[addr_R];
                                addr_R++;
                                addr_W++;
                                size--;
                            }
                            else // Copy from Window
                            {
                                int windowPosition = (SNES.rom[addr_R] & 3) << 8;
                                windowPosition |= SNES.rom[addr_R + 1];
                                int length = SNES.rom[addr_R] >> 2;

                                for (int i = 0; i < length; i++)
                                {
                                    Tiles[addr_W] = Tiles[addr_W - windowPosition];
                                    addr_W++;
                                }
                                size -= length;
                                addr_R += 2;
                            }
                            controlB <<= 1;
                            controlC--;

                            if (size < 1)
                                break;

                            if (controlC == 0)
                            {
                                //Reload Control Byte
                                controlB = SNES.rom[addr_R];
                                addr_R++;
                                controlC = 8;
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        MessageBox.Show($"Error happened when decompress - {compressId:X}" + " Tile Graphics" + e.Message + "\nCorrupted ROM ?", "ERROR");
                        Application.Current.Shutdown();
                    }
                }
            }
        }
    }
        #endregion Methods
}
