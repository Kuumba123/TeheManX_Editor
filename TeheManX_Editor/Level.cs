using System;
using System.Buffers.Binary;
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
        public static int TileSet = 0; //For backgrounds that use multiple TileSets
        public static byte[] Tiles = new byte[0x8000]; //Includes Filler Tiles
        public static byte[] DefaultObjectTiles; //Object Tiles for HP/Weapon/Tanks etc
        public static byte[,,] Layout = new byte[Const.MaxLevels, 2, 0x400];
        public static Color[,] Palette = new Color[8, 16]; //Converted to 24-bit Color
        public static int PaletteId;
        public static int PaletteColorAddress;
        public static List<Enemy>[] Enemies = new List<Enemy>[Const.MaxLevels];
        #endregion Fields

        #region Methods
        public static void LoadLevelData()
        {
            Id = 0;
            BG = 0;
            TileSet = 0;
            LoadLayouts();
            LoadEnemyData();
            DefaultObjectTiles = DecompressTiles(0xA, Const.Id);
        }
        public static unsafe void Draw16xTile(int id, int x, int y, int stride, IntPtr dest)
        {
            int stageId;
            if (Const.Id == Const.GameId.MegaManX3 && Id == 0xE) stageId = 0x10; //special case for MMX3 rekt version of dophler 2
            else if (Const.Id == Const.GameId.MegaManX3 && Id > 0xE) stageId = (Id - 0xF) + 0xE; //Buffalo or Beetle
            else stageId = Id;

            int offset = SNES.CpuToOffset(BinaryPrimitives.ReadInt32LittleEndian(SNES.rom.AsSpan(Const.Tile16DataPointersOffset[BG] + stageId * 3)) + id * 8);
            byte* buffer = (byte*)dest;

            Color backColor = Palette[0, 0];

            for (int i = 0; i < 4; i++)
            {
                ushort val = BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(offset + i * 2));
                int tileOffset = (val & 0x3FF) * 0x20; // 32 bytes per tile
                int set = (val >> 10) & 7;             // Palette index

                bool flipH = (val & 0x4000) != 0;
                bool flipV = (val & 0x8000) != 0;

                // Top-left of this 8x8 subtile in destination
                int destBase = (x + ((i & 1) * 8)) * 4 + (y + ((i >> 1) * 8)) * stride;

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

                        int destIndex = destBase + (destY * stride) + (destX * 4);

                        Color color = index == 0 ? backColor : Palette[set, index];

                        uint pixel = color.B | ((uint)color.G << 8) | ((uint)color.R << 16) | 0xFF000000;

                        *(uint*)(buffer + destIndex) = pixel;
                    }
                }
            }
        }
        public static unsafe void Draw16xTile_Clamped(int id, int x, int y,int stride, IntPtr dest,int bmpWidth, int bmpHeight)
        {
            int stageId;
            if (Const.Id == Const.GameId.MegaManX3 && Id == 0xE)
                stageId = 0x10;
            else if (Const.Id == Const.GameId.MegaManX3 && Id > 0xE)
                stageId = (Id - 0xF) + 0xE;
            else
                stageId = Id;

            int offset = SNES.CpuToOffset(
                BinaryPrimitives.ReadInt32LittleEndian(
                    SNES.rom.AsSpan(Const.Tile16DataPointersOffset[BG] + stageId * 3))
                + id * 8);

            byte* buffer = (byte*)dest;
            Color backColor = Palette[0, 0];

            // precalc bounds
            int maxX = bmpWidth - 1;
            int maxY = bmpHeight - 1;

            for (int i = 0; i < 4; i++)
            {
                ushort val = BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(offset + i * 2));
                int tileOffset = (val & 0x3FF) * 0x20;
                int set = (val >> 10) & 7;

                bool flipH = (val & 0x4000) != 0;
                bool flipV = (val & 0x8000) != 0;

                int tileX = x + ((i & 1) * 8);
                int tileY = y + ((i >> 1) * 8);

                for (int row = 0; row < 8; row++)
                {
                    int srcRow = flipV ? (7 - row) : row;
                    int py = tileY + srcRow;
                    if (py < 0 || py > maxY) continue;

                    int base1 = tileOffset + (row * 2);
                    int base2 = tileOffset + 0x10 + (row * 2);

                    for (int col = 0; col < 8; col++)
                    {
                        int srcCol = flipH ? (7 - col) : col;
                        int px = tileX + srcCol;
                        if (px < 0 || px > maxX) continue;

                        int bit = 7 - col;
                        int p0 = (Tiles[base1] >> bit) & 1;
                        int p1 = (Tiles[base1 + 1] >> bit) & 1;
                        int p2 = (Tiles[base2] >> bit) & 1;
                        int p3 = (Tiles[base2 + 1] >> bit) & 1;

                        byte index = (byte)(p0 | (p1 << 1) | (p2 << 2) | (p3 << 3));
                        Color color = index == 0 ? backColor : Palette[set, index];

                        uint pixel = color.B
                                    | ((uint)color.G << 8)
                                    | ((uint)color.R << 16)
                                    | 0xFF000000;

                        int destIndex = (py * stride) + (px * 4);
                        *(uint*)(buffer + destIndex) = pixel;
                    }
                }
            }
        }
        public static unsafe void DrawScreen(int s, int stride, IntPtr ptr)
        {
            int id;
            if (Const.Id == Const.GameId.MegaManX3 && Id == 0xE) id = 0x10; //special case for MMX3 rekt version of dophler 2
            else if (Const.Id == Const.GameId.MegaManX3 && Id > 0xE) id = (Id - 0xF) + 0xE; //Buffalo or Beetle
            else id = Id;

            int offset = SNES.CpuToOffset(BinaryPrimitives.ReadInt32LittleEndian(SNES.rom.AsSpan(Const.ScreenDataPointersOffset[BG] + id * 3))) + s * 0x80;
            int tile32Offset = SNES.CpuToOffset(BinaryPrimitives.ReadInt32LittleEndian(SNES.rom.AsSpan(Const.Tile32DataPointersOffset[BG] + id * 3)));

            for (int y = 0; y < 8; y++)
            {
                for (int x = 0; x < 8; x++)
                {
                    ushort tileId32 = BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(offset + (x * 2) + (y * 16)));

                    Draw16xTile(BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(tile32Offset + (tileId32 * 8))), x * 32, y * 32, stride, ptr);
                    Draw16xTile(BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(tile32Offset + (tileId32 * 8) + 2)), x * 32 + 16, y * 32, stride, ptr);
                    Draw16xTile(BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(tile32Offset + (tileId32 * 8) + 4)), x * 32, y * 32 + 16, stride, ptr);
                    Draw16xTile(BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(tile32Offset + (tileId32 * 8) + 6)), x * 32 + 16, y * 32 + 16, stride, ptr);
                }
            }
        }
        public static unsafe void DrawScreen(int s,int drawX,int drawY, int stride, IntPtr ptr)
        {
            int id;
            if (Const.Id == Const.GameId.MegaManX3 && Id == 0xE) id = 0x10; //special case for MMX3 rekt version of dophler 2
            else if (Const.Id == Const.GameId.MegaManX3 && Id > 0xE) id = (Id - 0xF) + 0xE; //Buffalo or Beetle
            else id = Id;

            int offset = SNES.CpuToOffset(BinaryPrimitives.ReadInt32LittleEndian(SNES.rom.AsSpan(Const.ScreenDataPointersOffset[BG] + id * 3))) + s * 0x80;
            int tile32Offset = SNES.CpuToOffset(BinaryPrimitives.ReadInt32LittleEndian(SNES.rom.AsSpan(Const.Tile32DataPointersOffset[BG] + id * 3)));

            for (int y = 0; y < 8; y++)
            {
                for (int x = 0; x < 8; x++)
                {
                    ushort tileId32 = BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(offset + (x * 2) + (y * 16)));

                    Draw16xTile(BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(tile32Offset + (tileId32 * 8))), x * 32 + drawX, y * 32 + drawY, stride, ptr);
                    Draw16xTile(BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(tile32Offset + (tileId32 * 8) + 2)), x * 32 + 16 + drawX, y * 32 + drawY, stride, ptr);
                    Draw16xTile(BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(tile32Offset + (tileId32 * 8) + 4)), x * 32 + drawX, y * 32 + 16 + drawY, stride, ptr);
                    Draw16xTile(BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(tile32Offset + (tileId32 * 8) + 6)), x * 32 + 16 + drawX, y * 32 + 16 + drawY, stride, ptr);
                }
            }
        }
        public static unsafe void DrawScreen_Clamped(int s, int drawX, int drawY, int stride, IntPtr ptr, int bmpWidth, int bmpHeight)
        {
            int id;
            if (Const.Id == Const.GameId.MegaManX3 && Id == 0xE) id = 0x10; //special case for MMX3 rekt version of dophler 2
            else if (Const.Id == Const.GameId.MegaManX3 && Id > 0xE) id = (Id - 0xF) + 0xE; //Buffalo or Beetle
            else id = Id;

            int offset = SNES.CpuToOffset(BinaryPrimitives.ReadInt32LittleEndian(SNES.rom.AsSpan(Const.ScreenDataPointersOffset[BG] + id * 3))) + s * 0x80;
            int tile32Offset = SNES.CpuToOffset(BinaryPrimitives.ReadInt32LittleEndian(SNES.rom.AsSpan(Const.Tile32DataPointersOffset[BG] + id * 3)));

            for (int y = 0; y < 8; y++)
            {
                for (int x = 0; x < 8; x++)
                {
                    ushort tileId32 = BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(offset + (x * 2) + (y * 16)));

                    Draw16xTile_Clamped(BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(tile32Offset + (tileId32 * 8))), x * 32 + drawX, y * 32 + drawY, stride, ptr, bmpWidth, bmpHeight);
                    Draw16xTile_Clamped(BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(tile32Offset + (tileId32 * 8) + 2)), x * 32 + 16 + drawX, y * 32 + drawY, stride, ptr, bmpWidth, bmpHeight);
                    Draw16xTile_Clamped(BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(tile32Offset + (tileId32 * 8) + 4)), x * 32 + drawX, y * 32 + 16 + drawY, stride, ptr, bmpWidth, bmpHeight);
                    Draw16xTile_Clamped(BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(tile32Offset + (tileId32 * 8) + 6)), x * 32 + 16 + drawX, y * 32 + 16 + drawY, stride, ptr, bmpWidth, bmpHeight);
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
                    int id;
                    if (Const.Id == Const.GameId.MegaManX3 && i == 0xE) id = 0x10; //special case for MMX3 rekt version of dophler 2
                    else if (Const.Id == Const.GameId.MegaManX3 && i > 0xE) id = (i - 0xF) + 0xE; //Buffalo or Beetle
                    else id = i;
                    int infoOffset = SNES.CpuToOffset(BinaryPrimitives.ReadInt32LittleEndian(SNES.rom.AsSpan(Const.LayoutPointersOffset[l] + id * 3)));

                    Array.Clear(temp, 0, temp.Length);
                    int destIndex = 0;
                    byte controlB;
                    int count;
                    int flags;

                    //Copy 3 byte header
                    temp[0] = SNES.rom[infoOffset];     //width
                    temp[1] = SNES.rom[infoOffset + 1]; //height
                    temp[2] = SNES.rom[infoOffset + 2]; //screen count (not needed for layout but is nice to know)
                    Const.ScreenCount[i, l] = temp[2];
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

            List<byte> compressed = new List<byte>(0x100);
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

                // Measure repeat runs
                while (i + repeatCount < total &&
                       data[i + repeatCount] == start &&
                       repeatCount < 0x7E)
                    repeatCount++;

                // Measure increment runs
                while (i + incCount < total &&
                       data[i + incCount] == (byte)(data[i + incCount - 1] + 1) &&
                       incCount < 0x7F)
                    incCount++;

                // Prefer repeat when tied
                bool useRepeat = repeatCount >= incCount;
                int runLength = useRepeat ? repeatCount : incCount;

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
        private static int GetCompressedLayoutLength(byte[] layout)
        {
            GetLayoutDimensions(layout, out byte width, out byte height);

            // Initial header: width, height, screenCount
            int size = 3;

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

                // Measure repeat runs
                while (i + repeatCount < total &&
                       data[i + repeatCount] == start &&
                       repeatCount < 0x7F)
                    repeatCount++;

                // Measure increment runs
                while (i + incCount < total &&
                       data[i + incCount] == (byte)(data[i + incCount - 1] + 1) &&
                       incCount < 0x7F)
                    incCount++;

                // Prefer repeat when tied
                bool useRepeat = repeatCount >= incCount;
                int runLength = useRepeat ? repeatCount : incCount;

                // The compressor writes:
                //   control byte + start byte
                size += 2;

                i += runLength;
            }

            // Terminator byte
            size += 1;

            return size;
        }
        public static bool SaveLayouts()
        {
            byte[] layout = new byte[0x400];

            //Before Attempting to export the Layouts we do Length Checks
            int totalSize = 0;
            int allowedSize = Const.TotalLayoutDataLength;

            if (Const.Id != Const.GameId.MegaManX) //Size Check for MegaMan X2 & X3
            {
                for (int l = 0; l < 2; l++)
                {
                    for (int i = 0; i < Const.PlayableLevelsCount; i++)
                    {
                        for (int d = 0; d < 0x400; d++)
                            layout[d] = Layout[i, l, d];
                        int length = GetCompressedLayoutLength(layout);

                        if (SNES.expanded && length > Const.ExpandLayoutLength)
                        {
                            MessageBox.Show($"The Layout for Stage {i:X2} Layer {l + 1} is too large ({length:X} bytes). The max length is {Const.ExpandLayoutLength:X} bytes!", "ERROR");
                            return false;
                        }

                        totalSize += length;
                    }
                }
            }
            else //Size Check for MegaMan X1
            {
                for (int l = 0; l < 2; l++)
                {
                    for (int i = 0; i < Const.LevelsCount; i++)
                    {
                        if (SNES.expanded && (i == 0xD || (i > 0xE && i <= 0x1A) || (i > 0x1B && i <= 0x22))) //Duped Layouts
                            continue;

                        for (int d = 0; d < 0x400; d++)
                            layout[d] = Layout[i, l, d];
                        int length = GetCompressedLayoutLength(layout);

                        if (i < Const.PlayableLevelsCount && SNES.expanded && length > Const.ExpandLayoutLength)
                        {
                            MessageBox.Show($"The Layout for Stage {i:X2} Layer {l + 1} is too large ({length:X} bytes). The max length is {Const.ExpandLayoutLength:X} bytes!", "ERROR");
                            return false;
                        }

                        if (i < Const.PlayableLevelsCount || !SNES.expanded)
                            totalSize += length;
                    }
                }
            }

            if ((totalSize > allowedSize && Const.Id != Const.GameId.MegaManX && !SNES.expanded) || (totalSize > allowedSize && Const.Id == Const.GameId.MegaManX))
            {
                MessageBox.Show($"Layout Data is too large to be saved to the game ({totalSize:X} vs {allowedSize:X}).", "ERROR");
                return false;
            }

            /*
             *  Size Check is Done!
             *  Now it is time to export the Layout Data
             *  
             *  1. Check the expand flag and dump the layouts (for X1 dont dump the non playable stages) .
             *  2. If the expand option is not enabled or the game is X1 attempt to dump the layouts semi normally.
             */

            if (SNES.expanded)
            {
                for (int l = 0; l < 2; l++)
                {
                    for (int i = 0; i < Const.PlayableLevelsCount; i++)
                    {
                        for (int d = 0; d < 0x400; d++)
                            layout[d] = Layout[i, l, d];

                        byte[] compressedLayout = CompressLayout(layout, (byte)Const.ScreenCount[i, l]);

                        //Save Layout to Rom
                        int id;
                        if (Const.Id == Const.GameId.MegaManX3 && i == 0xE) id = 0x10; //special case for MMX3 rekt version of dophler 2
                        else if (Const.Id == Const.GameId.MegaManX3 && i > 0xE) id = (i - 0xF) + 0xE; //Buffalo or Beetle
                        else id = i;
                        int offset = SNES.CpuToOffset(BinaryPrimitives.ReadInt32LittleEndian(SNES.rom.AsSpan(Const.LayoutPointersOffset[l] + id * 3)));
                        Array.Copy(compressedLayout, 0, SNES.rom, offset, compressedLayout.Length);
                    }
                }
            }

            if (!SNES.expanded || Const.Id == Const.GameId.MegaManX)
            {
                int dumpOffset = Const.LayoutDataOffset;

                for (int l = 0; l < 2; l++)
                {
                    byte[] pointerData = new byte[Const.LevelsCount * 3];

                    int startIndex;

                    if ((Const.Id != Const.GameId.MegaManX) || !SNES.expanded)
                        startIndex = 0;
                    else
                        startIndex = 0xD;

                    for (int i = startIndex; i < Const.LevelsCount; i++)
                    {
                        for (int d = 0; d < 0x400; d++)
                            layout[d] = Layout[i, l, d];

                        int dumpAddr = SNES.OffsetToCpu(dumpOffset);

                        if (Const.Id == Const.GameId.MegaManX)
                        {
                            dumpAddr |= 0x800000;

                            //Check if layout should be skipped
                            if ((i == 0xD && !SNES.expanded) || (i > 0xE && i <= 0x1A) || (i > 0x1B && i <= 0x22))
                                continue;


                            //Determine witch layouts are shared and export them
                            if (i == 4 || (SNES.expanded && i == 0xD))
                            {
                                if (SNES.expanded) //direct to expanded Stage 4 if expansion is enabled
                                    dumpAddr = BinaryPrimitives.ReadInt32LittleEndian(SNES.rom.AsSpan(Const.LayoutPointersOffset[l] + 4 * 3));

                                BinaryPrimitives.WriteUInt16LittleEndian(pointerData.AsSpan(4 * 3), (ushort)(dumpAddr & 0xFFFF));
                                pointerData[4 * 3 + 2] = (byte)((dumpAddr >> 16) & 0xFF);

                                BinaryPrimitives.WriteUInt16LittleEndian(pointerData.AsSpan(0xD * 3), (ushort)(dumpAddr & 0xFFFF));
                                pointerData[0xD * 3 + 2] = (byte)((dumpAddr >> 16) & 0xFF);
                            }
                            else if (i == 0xE)
                            {
                                for (int c = 0; c < 13; c++)
                                {
                                    BinaryPrimitives.WriteUInt16LittleEndian(pointerData.AsSpan((c + 0xE) * 3), (ushort)(dumpAddr & 0xFFFF));
                                    pointerData[(c + 0xE) * 3 + 2] = (byte)((dumpAddr >> 16) & 0xFF);
                                }
                            }
                            else if (i == 0x1B)
                            {
                                for (int c = 0; c < 8; c++)
                                {
                                    BinaryPrimitives.WriteUInt16LittleEndian(pointerData.AsSpan((c + 0x1B) * 3), (ushort)(dumpAddr & 0xFFFF));
                                    pointerData[(c + 0x1B) * 3 + 2] = (byte)((dumpAddr >> 16) & 0xFF);
                                }
                            }
                        }

                        byte[] compressedLayout = CompressLayout(layout, (byte)Const.ScreenCount[i, l]);

                        int id;
                        if (Const.Id == Const.GameId.MegaManX3 && i == 0xE) id = 0x10; //special case for MMX3 rekt version of dophler 2
                        else if (Const.Id == Const.GameId.MegaManX3 && i > 0xE) id = (i - 0xF) + 0xE; //Buffalo or Beetle
                        else id = i;

                        Array.Copy(compressedLayout, 0, SNES.rom, dumpOffset, compressedLayout.Length);
                        BinaryPrimitives.WriteUInt16LittleEndian(pointerData.AsSpan(id * 3), (ushort)(dumpAddr & 0xFFFF));
                        pointerData[id * 3 + 2] = (byte)((dumpAddr >> 16) & 0xFF);

                        dumpOffset += compressedLayout.Length;
                    }

                    Array.Copy(pointerData, startIndex * 3, SNES.rom, Const.LayoutPointersOffset[l] + startIndex * 3, pointerData.Length - (startIndex * 3));
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
                int totalStages = (Const.Id == Const.GameId.MegaManX3) ? 0xF : Const.PlayableLevelsCount;

                for (int i = 0; i < totalStages; i++)
                {
                    stage = i;
                    //Get Address of Enemy Data
                    int addr = SNES.CpuToOffset(BinaryPrimitives.ReadInt32LittleEndian(SNES.rom.AsSpan(Const.EnemyPointersOffset + (i * 2))) , Const.EnemyDataBank);
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
                        Enemies[i][t].Y = (short)(BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(addr + 1)) & 0x7FFF);
                        //Assign Id & Sub Id
                        Enemies[i][t].Id = SNES.rom[addr + 3];
                        Enemies[i][t].SubId = SNES.rom[addr + 4];
                        //Assign X
                        Enemies[i][t].X = (short)(BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(addr + 5)) & 0x7FFF);

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
        private static byte[] CreateEnemyData(List<Enemy> enemyList)
        {
            MemoryStream ms = new MemoryStream(0x660);
            BinaryWriter bw = new BinaryWriter(ms);

            List<Enemy> sorted = enemyList.OrderBy(e => e.Column).ToList();

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
                        bw.Write((ushort)(sorted[i].X | 0x8000)); // Set high byte to mark end of column
                        column = sorted[i + 1].Column;
                        bw.Write(column); // Write new column byte
                    }
                    else
                        bw.Write(sorted[i].X);
                }
            }
            bw.Close();

            return ms.ToArray();
        }
        private static int GetEnemyDataLength(List<Enemy> enemyList)
        {
            int length = 1;

            List<Enemy> sorted = enemyList.OrderBy(e => e.Column).ToList();

            byte column = sorted[0].Column;

            for (int i = 0; i < sorted.Count; i++)
            {
                length += 5;

                if (i == (sorted.Count - 1)) // Last Enemy
                {
                    length += 2;
                    length++;
                }
                else
                {
                    if (column != sorted[i + 1].Column)
                    {
                        column = sorted[i + 1].Column;
                        length++;
                    }
                    length += 2;
                }
            }
            return length;
        }
        public static bool SaveEnemyData()
        {
            int totalStages = (Const.Id == Const.GameId.MegaManX3) ? 0xF : Const.PlayableLevelsCount;

            //1st Check if All Stages have enemies
            for (int id = 0; id < totalStages; id++)
            {
                if (Enemies[id].Count == 0)
                {
                    MessageBox.Show(
                        $"Enemy Data for Stage {id:X2} needs atleast 1 enemy because of a bug in the game's enemy dumping code.", "ERROR");
                    return false;
                }
            }

            if (!SNES.expanded) // Normal Export
            {
                int totalSize = 0;
                int allowedSize = (Const.Id == Const.GameId.MegaManX) ? Const.TotalEnemyDataLength + Const.MegaManX.ExtraTotalEnemyDataLength : Const.TotalEnemyDataLength;

                //Size Check in case enemy data is too long
                for (int id = 0; id < totalStages; id++)
                    totalSize += GetEnemyDataLength(Enemies[id]);

                if (totalSize > allowedSize)
                {
                    MessageBox.Show($"Enemy Data is too large to be saved to the game ({totalSize:X} vs allowed size of {allowedSize:X}).", "ERROR");
                    return false;
                }

                ushort[] pointerData = new ushort[totalStages];

                int dumpOffset = Const.EnemyPointersOffset + totalStages * 2;
                int dumpAmount = 0;

                if (Const.Id != Const.GameId.MegaManX3)
                    dumpOffset += 6; //X1 & X2 have 3 dummy entries for some reason...
                else
                    dumpOffset += 2; //X3 has 1 dummy entry...

                bool extraData = false;

                for (int id = 0; id < totalStages; id++)
                {
                    byte[] data = CreateEnemyData(Enemies[id]);

                    if (Const.Id == Const.GameId.MegaManX && !extraData && (dumpAmount + data.Length) > Const.MegaManX.TotalEnemyDataLength)
                    {
                        extraData = true;
                        dumpOffset = Const.MegaManX.ExtraTotalEnemyDataOffset;
                    }

                    //copy actual enemy data and save location
                    Array.Copy(data, 0, SNES.rom, dumpOffset, data.Length);
                    pointerData[id] = (ushort)(SNES.OffsetToCpu(dumpOffset) & 0xFFFF);

                    //Increament Offset
                    dumpOffset += data.Length;
                    dumpAmount += data.Length;
                }
                //Now Write 16-bit Pointers
                Buffer.BlockCopy(pointerData, 0, SNES.rom, Const.EnemyPointersOffset, totalStages * 2);
            }
            else // Expanded Export
            {
                for (int id = 0; id < totalStages; id++)
                {
                    byte[] data = CreateEnemyData(Enemies[id]);

                    // Get offset from ROM
                    int offset = SNES.CpuToOffset(
                        BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(Const.EnemyPointersOffset + (id * 2))),
                        Const.EnemyDataBank
                    );

                    Array.Copy(data, 0, SNES.rom, offset, data.Length);
                }
            }

            return true;
        }
        public static void AssignPallete()
        {
            if (Id < Const.PlayableLevelsCount)
            {
                int id;
                if (Const.Id == Const.GameId.MegaManX3 && Id == 0xE) id = 0xB; //special case for MMX3 rekt version of dophler 2
                else if (Const.Id == Const.GameId.MegaManX3 && Id > 0xE) id = (Id - 0xF) + 2;
                else id = Id;

                PaletteId = id * 2 + Const.PaletteStageBase;
                int infoOffset = SNES.CpuToOffset(BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(Const.PaletteInfoOffset + PaletteId)), Const.PaletteBank);

                while (SNES.rom[infoOffset] != 0)
                {
                    int colorCount = SNES.rom[infoOffset]; //how many colors are going to be dumped
                    byte colorIndex = SNES.rom[infoOffset + 3]; //which color index to start dumping at
                    int colorOffset = SNES.CpuToOffset(BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(infoOffset + 1)) + (Const.PaletteColorBank << 16)); //where the colors are located
                    PaletteColorAddress = SNES.OffsetToCpu(colorOffset);

                    for (int c = 0; c < colorCount; c++)
                    {
                        if ((colorIndex + c) > 0x7F)
                            return;

                        ushort color = BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(colorOffset + c * 2));
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
                PaletteId = -1;
                PaletteColorAddress = -1;
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
        public static void LoadLevelTiles()
        {
            Array.Copy(Const.VRAM_B, 0, Tiles, 0, 0x200);
            Array.Clear(Tiles, 0x200, Tiles.Length - 0x200);
            if (Id >= Const.PlayableLevelsCount)
                return;
            DecompressLevelTiles();
            LoadDynamicBackgroundTiles();
        }
        public static void LoadDynamicBackgroundTiles()
        {
            //Load Dynamic Background Tiles
            int id;
            if (Const.Id == Const.GameId.MegaManX3 && Id > 0xE) id = (Id & 1) + 2; //Buffalo or Beetle
            else id = Id;
            
            int stageOffset = BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(Const.BackgroundTileInfoOffset + id * 2)) + Const.BackgroundTileInfoOffset;
            int offset = BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(stageOffset + TileSet * 2)) + Const.BackgroundTileInfoOffset;

            ushort transferSize = BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(offset));

            while (transferSize != 0)
            {
                ushort vramAddress = BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(offset + 2));
                int srcOffset = SNES.CpuToOffset(BinaryPrimitives.ReadInt32LittleEndian(SNES.rom.AsSpan(offset + 4)));

                try
                {
                    int dest = (vramAddress * 2) - 0x2000;
                    Array.Copy(SNES.rom, srcOffset, Tiles, dest, transferSize);

                }
                catch (Exception)
                {
                    MessageBox.Show($"Error happened when loading Tile Graphics from ROM offset 0x{srcOffset:X}\nCorrupted ROM ?", "ERROR");
                    Application.Current.Shutdown();
                }
                //Now for the Pallete Data
                int palInfoOffset = 0;
                ushort palId = 0;
                try
                {
                    palId = BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(offset + 7));
                    palInfoOffset = SNES.CpuToOffset(BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(Const.PaletteInfoOffset + palId)), Const.PaletteBank);

                    while (SNES.rom[palInfoOffset] != 0)
                    {
                        int colorCount = SNES.rom[palInfoOffset]; //how many colors are going to be dumped
                        byte colorIndex = SNES.rom[palInfoOffset + 3]; //which color index to start dumping at
                        int colorOffset = SNES.CpuToOffset(BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(palInfoOffset + 1)) + (Const.PaletteColorBank << 16)); //where the colors are located

                        for (int c = 0; c < colorCount; c++)
                        {
                            if ((colorIndex + c) > 0x7F)
                                return;

                            ushort color = BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(colorOffset + c * 2));
                            byte R = (byte)(color % 32 * 8);
                            byte G = (byte)(color / 32 % 32 * 8);
                            byte B = (byte)(color / 1024 % 32 * 8);

                            Palette[((colorIndex + c) >> 4) & 0xF, (colorIndex + c) & 0xF] = Color.FromRgb(R, G, B);
                        }
                        palInfoOffset += 4;
                    }
                }
                catch (Exception)
                {
                    MessageBox.Show($"Error happened when loading Tile Graphics from ROM offset 0x{palInfoOffset:X} via Id 0x{palId:X}\nCorrupted ROM ?", "ERROR");
                    Application.Current.Shutdown();
                }

                //Next Transfer
                offset += 9;
                transferSize = BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(offset));
            }
        }
        public static void DecompressLevelTiles() //also loads dynamic background tiles and pallete data for those tiles
        {
            int id;
            if (Const.Id == Const.GameId.MegaManX3 && Id == 0xE) id = 0xB; //special case for MMX3 rekt version of dophler 2
            else if (Const.Id == Const.GameId.MegaManX3 && Id > 0xE) id = (Id - 0xF) + 2; //Buffalo or Beetle
            else id = Id;

            int infoOffset = SNES.CpuToOffset(BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(Const.LoadTileSetInfoOffset + id * 2 + Const.LoadTileSetStageBase)), Const.LoadTileSetBank);

            if (SNES.rom[infoOffset] != 0xFF)
            {
                int compressId = SNES.rom[infoOffset]; //which compressed tile Id to load
                DecompressTiles2(compressId, Tiles, 0x200, Const.Id);
            }
        }
        public static byte[] DecompressTiles(int compressedTileId, Const.GameId gameId)
        {
            byte[] decompressed = null;

            if (gameId == Const.GameId.MegaManX)
            {
                int addr_W = 0;
                int addr_R = SNES.CpuToOffset(BinaryPrimitives.ReadInt32LittleEndian(SNES.rom.AsSpan((compressedTileId * 5) + Const.CompressedTileInfoOffset + 2)));
                ushort size = BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(compressedTileId * 5 + Const.CompressedTileInfoOffset));
                decompressed = new byte[size];
                size = (ushort)((size + 7) >> 3);
                int controlB;
                byte copyB;

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
                                decompressed[addr_W] = copyB;
                                addr_W++;
                            }
                            else
                            {
                                decompressed[addr_W] = SNES.rom[addr_R];
                                addr_R++;
                                addr_W++;
                            }
                        }
                        size--;
                    }
                }
                catch (Exception e)
                {
                    MessageBox.Show($"Error happened when decompress - {compressedTileId:X}" + " Tile Graphics" + e.Message + "\nCorrupted ROM ?", "ERROR");
                    Application.Current.Shutdown();
                }
            }
            else
            {
                int addr_W = 0;
                int addr_R = SNES.CpuToOffset(BinaryPrimitives.ReadInt32LittleEndian(SNES.rom.AsSpan(compressedTileId * 5 + Const.CompressedTileInfoOffset)));
                int size = BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(compressedTileId * 5 + Const.CompressedTileInfoOffset + 3));
                decompressed = new byte[size];

                try
                {
                    byte controlB = SNES.rom[addr_R];
                    addr_R++;
                    byte controlC = 8;

                    while (true)
                    {
                        if ((controlB & 0x80) == 0)
                        {
                            decompressed[addr_W] = SNES.rom[addr_R];
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
                                decompressed[addr_W] = decompressed[addr_W - windowPosition];
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
                    MessageBox.Show($"Error happened when decompress - {compressedTileId:X}" + " Tile Graphics" + e.Message + "\nCorrupted ROM ?", "ERROR");
                    Application.Current.Shutdown();
                }
            }

            return decompressed;
        }
        public static byte[] DecompressTiles2(int compressedTileId, byte[] dest, int destOffset, Const.GameId gameId)
        {
            byte[] decompressed = dest;
            int addr_W = destOffset;

            if (gameId == Const.GameId.MegaManX)
            {
                int addr_R = SNES.CpuToOffset(BinaryPrimitives.ReadInt32LittleEndian(SNES.rom.AsSpan((compressedTileId * 5) + Const.CompressedTileInfoOffset + 2)));
                ushort size = BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(compressedTileId * 5 + Const.CompressedTileInfoOffset));
                size = (ushort)((size + 7) >> 3);
                int controlB;
                byte copyB;


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
                                decompressed[addr_W] = copyB;
                                addr_W++;
                            }
                            else
                            {
                                decompressed[addr_W] = SNES.rom[addr_R];
                                addr_R++;
                                addr_W++;
                            }
                        }
                        size--;
                    }
                }
                catch (Exception e)
                {
                    MessageBox.Show($"Error happened when decompress - {compressedTileId:X}" + " Tile Graphics" + e.Message + "\nCorrupted ROM ?", "ERROR");
                    Application.Current.Shutdown();
                }
            }
            else
            {
                int addr_R = SNES.CpuToOffset(BinaryPrimitives.ReadInt32LittleEndian(SNES.rom.AsSpan(compressedTileId * 5 + Const.CompressedTileInfoOffset)));
                int size = BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(compressedTileId * 5 + Const.CompressedTileInfoOffset + 3));

                try
                {
                    byte controlB = SNES.rom[addr_R];
                    addr_R++;
                    byte controlC = 8;

                    while (true)
                    {
                        if ((controlB & 0x80) == 0)
                        {
                            dest[addr_W] = SNES.rom[addr_R];
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
                                dest[addr_W] = dest[addr_W - windowPosition];
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
                    MessageBox.Show($"Error happened when decompress - {compressedTileId:X}" + " Tile Graphics" + e.Message + "\nCorrupted ROM ?", "ERROR");
                    Application.Current.Shutdown();
                }
            }

            return decompressed;
        }
        #endregion Methods
    }
}