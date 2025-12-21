using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace TeheManX_Editor.Forms
{
    /// <summary>
    /// Interaction logic for TileEditor.xaml
    /// </summary>
    public partial class TileEditor : UserControl
    {
        #region Fields
        public static byte[] ObjectTiles = new byte[0x4000];
        public static Color[,] Palette = new Color[8, 16]; //Converted to 24-bit Color
        public static byte[] Bank7F = new byte[0x7E00];
        #endregion Fields

        #region Properties
        private bool _suppressBgSrcBoxTextChanged;
        public static int objectTileSetId;
        public static int objectTileSlotId;
        public static int palId = 1;
        WriteableBitmap vramTiles = new WriteableBitmap(128, 128, 96, 96, PixelFormats.Bgra32, null);
        Rectangle selectSetRect = new Rectangle() { IsHitTestVisible = false, StrokeThickness = 2.5, StrokeDashArray = new DoubleCollection() { 2.2 }, CacheMode = null, Stroke = Brushes.PapayaWhip };
        private bool suppressObjectTileInt;
        #endregion Properties

        #region Constructor
        public TileEditor()
        {
            InitializeComponent();

            for (int col = 0; col < 16; col++)
            {
                paletteGrid.ColumnDefinitions.Add(new ColumnDefinition());
                objectTileGrid.ColumnDefinitions.Add(new ColumnDefinition());
            }
            for (int row = 0; row < 8; row++)
                paletteGrid.RowDefinitions.Add(new RowDefinition());

            for (int row = 0; row < 16; row++)
                objectTileGrid.RowDefinitions.Add(new RowDefinition());

            for (int y = 0; y < 8; y++)
            {
                for (int x = 0; x < 16; x++)
                {
                    //Create Color
                    Rectangle rect = new Rectangle();
                    rect.Focusable = false;
                    rect.Width = 16;
                    rect.Height = 16;
                    rect.MouseDown += Color_Down;
                    rect.Fill = Brushes.Transparent;
                    rect.HorizontalAlignment = HorizontalAlignment.Stretch;
                    rect.VerticalAlignment = VerticalAlignment.Stretch;
                    Grid.SetColumn(rect, x);
                    Grid.SetRow(rect, y);
                    paletteGrid.Children.Add(rect);
                }
            }
            Grid.SetColumnSpan(selectSetRect, 16);
            paletteGrid.Children.Add(selectSetRect);
            objectTilesImage.Source = vramTiles;
        }
        #endregion Constructor

        #region Methods
        public void AssignLimits()
        {
            if (Level.Id >= Const.PlayableLevelsCount || (Const.Id == Const.GameId.MegaManX3 && Level.Id > 0xE))
            {
                // Disable UI
                MainWindow.window.tileE.bgTileSetInt.IsEnabled = false;
                MainWindow.window.tileE.bgLengthInt.IsEnabled = false;
                MainWindow.window.tileE.bgAddressInt.IsEnabled = false;
                MainWindow.window.tileE.bgSrcBox.IsEnabled = false;
                MainWindow.window.tileE.bgPalInt.IsEnabled = false;

                MainWindow.window.tileE.objTileSetInt.IsEnabled = false;
                MainWindow.window.tileE.compressTileInt.IsEnabled = false;
                MainWindow.window.tileE.vramLocationInt.IsEnabled = false;
                MainWindow.window.tileE.palSetInt.IsEnabled = false;
                MainWindow.window.tileE.dumpInt.IsEnabled = false;
                MainWindow.window.tileE.oam1Btn.IsEnabled = false;
                MainWindow.window.tileE.oam2Btn.IsEnabled = false;
                return;
            }
            suppressObjectTileInt = true;

            // Enable Background Tile UI
            MainWindow.window.tileE.bgTileSetInt.IsEnabled = true;


            //Get Max Amount of BG Tile Settings
            int id;
            if (Const.Id == Const.GameId.MegaManX3 && Level.Id > 0xE) id = (Level.Id - 0xF) + 2; //Buffalo or Beetle
            else id = Level.Id;

            int maxLevels;
            if (Const.Id == Const.GameId.MegaManX3) maxLevels = 0xF;
            else maxLevels = Const.PlayableLevelsCount;

            ushort[] offsets = new ushort[maxLevels];
            Buffer.BlockCopy(SNES.rom, Const.BackgroundTileInfoOffset, offsets, 0, offsets.Length * 2);
            ushort toFindOffset = offsets[id];
            int index = Array.IndexOf(offsets, toFindOffset);
            int maxBGTiles = ((offsets[index + 1] - toFindOffset) / 2) - 1;

            if (maxBGTiles >= 0)
            {
                MainWindow.window.tileE.bgTileSetInt.Maximum = maxBGTiles;
                if (MainWindow.window.tileE.bgTileSetInt.Value > maxBGTiles)
                    MainWindow.window.tileE.bgTileSetInt.Value = maxBGTiles;
            }
            else
            {
                MainWindow.window.tileE.bgTileSetInt.Maximum = 0;
                MainWindow.window.tileE.bgTileSetInt.Value = 0;
            }
            // Set Background Tile Values
            int listOffset = BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(Const.BackgroundTileInfoOffset + id * 2)) + Const.BackgroundTileInfoOffset;
            int offset = BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(listOffset + (int)MainWindow.window.tileE.bgTileSetInt.Value * 2)) + Const.BackgroundTileInfoOffset;

            if (BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(offset)) != 0)
            {
                MainWindow.window.tileE.bgLengthInt.IsEnabled = true;
                MainWindow.window.tileE.bgAddressInt.IsEnabled = true;
                MainWindow.window.tileE.bgSrcBox.IsEnabled = true;
                MainWindow.window.tileE.bgPalInt.IsEnabled = true;
                SetBackgroundValues(offset);
            }
            else
            {
                MainWindow.window.tileE.bgLengthInt.IsEnabled = false;
                MainWindow.window.tileE.bgAddressInt.IsEnabled = false;
                MainWindow.window.tileE.bgSrcBox.IsEnabled = false;
                MainWindow.window.tileE.bgPalInt.IsEnabled = false;
            }


            //Get Max Amount of Object Tile Settings
            int objectStages = Const.Id == Const.GameId.MegaManX ? 0x24 : Const.Id == Const.GameId.MegaManX2 ? 0xF : 0x12;
            offsets = new ushort[objectStages];
            Buffer.BlockCopy(SNES.rom, Const.ObjectTileInfoOffset, offsets, 0, objectStages * 2);
            toFindOffset = offsets[id];
            Array.Sort(offsets);
            index = Array.IndexOf(offsets, toFindOffset);
            int maxOBJTiles = ((offsets[index + 1] - toFindOffset) / 2) - 1;

            objTileSetInt.Maximum = maxOBJTiles;
            objTileSetInt.Value = 0;
            objectTileSetId = (int)MainWindow.window.tileE.objTileSetInt.Value;
            objectTileSlotId = 0;
            objectSlotInt.Value = 0;

            //Set Max Compressed Tiles
            if (Const.Id == Const.GameId.MegaManX)
                compressTileInt.Maximum = Const.MegaManX.CompressedTilesAmount;
            else if (Const.Id == Const.GameId.MegaManX2)
                compressTileInt.Maximum = Const.MegaManX2.CompressedTilesAmount;
            else
                compressTileInt.Maximum = Const.MegaManX3.CompressedTilesAmount;

            for (int i = 0; i < 0x80; i++)
                Palette[i >> 4, i & 0xF] = Color.FromRgb(0, 0, 0);

            Array.Clear(ObjectTiles);

            int[] palsToLoad = { 0, 0x14, 0x1C, 0x40 };
            int[] palsDest = { 0x10, 0x0, 0x20, 0x30 };

            for (int i = 0; i < 4; i++)
            {
                int palId = palsToLoad[i];
                int dumpLocation = palsDest[i];

                int infoOffset = SNES.CpuToOffset(BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(Const.PaletteInfoOffset + palId)), Const.PaletteBank);

                while (SNES.rom[infoOffset] != 0)
                {
                    int colorCount = SNES.rom[infoOffset]; //how many colors are going to be dumped
                    int colorIndex = SNES.rom[infoOffset + 3] + dumpLocation - 0x80; //which color index to start dumping at
                    int colorOffset = SNES.CpuToOffset(BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(infoOffset + 1)) + (Const.PaletteColorBank << 16)); //where the colors are located

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

            Array.Copy(SNES.rom, Const.MegaManTilesOffset, ObjectTiles, 0, 32 * 16 * 2);
            Array.Copy(Level.DefaultObjectTiles, 0, ObjectTiles, 0x1000, Level.DefaultObjectTiles.Length);

            //Green Charge Shot
            Array.Copy(SNES.rom, Const.MegaManGreenChargeShotTilesOffset[0], ObjectTiles, 0x400, 0x100);
            Array.Copy(SNES.rom, Const.MegaManGreenChargeShotTilesOffset[1], ObjectTiles, 0x600, 0x100);

            DrawObjectTiles();
            DrawPalette();
            UpdateCursor();
            SetMaxObjectSlots();
            SetObjectSlotValues();

            //Re Enable Object Tile UI
            MainWindow.window.tileE.objTileSetInt.IsEnabled = true;
            MainWindow.window.tileE.compressTileInt.IsEnabled = true;
            MainWindow.window.tileE.vramLocationInt.IsEnabled = true;
            MainWindow.window.tileE.palSetInt.IsEnabled = true;
            MainWindow.window.tileE.dumpInt.IsEnabled = true;
            MainWindow.window.tileE.oam1Btn.IsEnabled = true;
            MainWindow.window.tileE.oam2Btn.IsEnabled = true;

            suppressObjectTileInt = false;
        }
        private void SetBackgroundValues(int offset)
        {
            int length = BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(offset));
            int dest = BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(offset + 2));
            int srcAddr = BinaryPrimitives.ReadInt32LittleEndian(SNES.rom.AsSpan(offset + 4)) & 0xFFFFFF;
            int palId = BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(offset + 7));

            if (MainWindow.window.tileE.romOffsetCheck.IsChecked == true)
                srcAddr = SNES.CpuToOffset(srcAddr);

            MainWindow.window.tileE.bgLengthInt.Value = length;
            MainWindow.window.tileE.bgAddressInt.Value = dest;
            _suppressBgSrcBoxTextChanged = true;
            MainWindow.window.tileE.bgSrcBox.Text = srcAddr.ToString("X6");
            _suppressBgSrcBoxTextChanged = false;
            MainWindow.window.tileE.bgPalInt.Value = palId;
        }
        private unsafe void DrawObjectTiles()
        {
            int id;
            if (Const.Id == Const.GameId.MegaManX3 && Level.Id > 0xE) id = (Level.Id - 0xF) + 2; //Buffalo or Beetle
            else id = Level.Id;
            int listOffset = BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(Const.ObjectTileInfoOffset + id * 2)) + Const.ObjectTileInfoOffset;
            int offset = BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(listOffset + (int)objectTileSetId * 2)) + Const.ObjectTileInfoOffset;

            while (true)
            {
                byte compressedTileId = SNES.rom[offset];

                if (compressedTileId == 0xFF)
                    break;

                //Load Object Tiles
                ushort relativeVramAddr = BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(offset + 1));

                int specOffset = BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(Const.CompressedTilesSwapInfoOffset + compressedTileId * 2)) + Const.CompressedTilesSwapInfoOffset;
                int srcOffset = 0;

                Level.DecompressTiles2(compressedTileId, Bank7F, 0, Const.Id);

                while (true)
                {
                    int length = SNES.rom[specOffset];
                    if (length == 0)
                        break;
                    if (length == 0xFF)
                    {
                        specOffset++;
                        continue;
                    }
                    length *= 16;

                    byte vramBaseHigh = SNES.rom[specOffset + 1];
                    int destOffset = (relativeVramAddr + (((vramBaseHigh & 0x7F) - 0x60) << 8)) * 2;

                    int srcAvailable = Bank7F.Length - srcOffset;
                    int dstAvailable = ObjectTiles.Length - destOffset;

                    // Nothing to copy
                    if (srcAvailable <= 0 || dstAvailable <= 0 || length <= 0)
                        ;
                    else
                    {
                        // Clamp length to what is actually available
                        int safeLength = Math.Min(length, Math.Min(srcAvailable, dstAvailable));

                        Array.Copy(Bank7F, srcOffset, ObjectTiles, destOffset, safeLength);
                    }


                    if ((vramBaseHigh & 0x80) != 0)
                        break;
                    srcOffset += length;
                    specOffset += 2;
                }

                //Load Object Palette
                ushort palId = BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(offset + 3));
                byte dumpLocation = SNES.rom[offset + 5];

                int infoOffset = SNES.CpuToOffset(BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(Const.PaletteInfoOffset + palId)), Const.PaletteBank);

                while (SNES.rom[infoOffset] != 0)
                {
                    int colorCount = SNES.rom[infoOffset]; //how many colors are going to be dumped
                    int colorIndex = SNES.rom[infoOffset + 3] + dumpLocation - 0x80; //which color index to start dumping at
                    int colorOffset = SNES.CpuToOffset(BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(infoOffset + 1)) + (Const.PaletteColorBank << 16)); //where the colors are located

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
                offset += 6;
            }
            /****************/

            vramTiles.Lock();
            byte* buffer = (byte*)vramTiles.BackBuffer;
            int set = palId;

            /*
             *  Draw 0x200 tiles from VRAM
             */

            int readBase = (MainWindow.window.tileE.oam1Btn.IsChecked != false) ? 0x0000 : 0x2000;

            for (int y = 0; y < 16; y++)
            {
                for (int x = 0; x < 16; x++)
                {
                    int tid = x + (y * 16);
                    int tileOffset = tid * 0x20 + readBase; // 32 bytes per tile

                    for (int row = 0; row < 8; row++)
                    {
                        int base1 = tileOffset + (row * 2);
                        int base2 = tileOffset + 0x10 + (row * 2);

                        for (int col = 0; col < 8; col++)
                        {
                            int bit = 7 - col; // leftmost pixel = bit7
                            int p0 = (ObjectTiles[base1] >> bit) & 1;
                            int p1 = (ObjectTiles[base1 + 1] >> bit) & 1;
                            int p2 = (ObjectTiles[base2] >> bit) & 1;
                            int p3 = (ObjectTiles[base2 + 1] >> bit) & 1;

                            byte index = (byte)(p0 | (p1 << 1) | (p2 << 2) | (p3 << 3));

                            // compute pixel position once and write 32-bit BGRA in a single store
                            int px = x * 8 + col;
                            int py = y * 8 + row;
                            int baseIdx = px * 4 + py * vramTiles.BackBufferStride;
                            Color colStruct = Palette[set, index];
                            uint bgra = (0xFFu << 24) | ((uint)colStruct.R << 16) | ((uint)colStruct.G << 8) | (uint)colStruct.B;
                            *(uint*)(buffer + baseIdx) = bgra;
                        }
                    }
                }
            }
            vramTiles.AddDirtyRect(new Int32Rect(0, 0, 128, 128));
            vramTiles.Unlock();
        }
        private void DrawPalette()
        {
            for (int y = 0; y < 8; y++)
            {
                for (int x = 0; x < 16; x++)
                {
                    int index = x + (y * 16);
                    Rectangle rect = (Rectangle)paletteGrid.Children[index];
                    Color colStruct = Palette[y, x];
                    rect.Fill = new SolidColorBrush(colStruct);
                }
            }
        }
        private void SetObjectSlotValues()
        {
            int id;
            if (Const.Id == Const.GameId.MegaManX3 && Level.Id > 0xE) id = (Level.Id - 0xF) + 2; //Buffalo or Beetle
            else id = Level.Id;
            int listOffset = BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(Const.ObjectTileInfoOffset + id * 2)) + Const.ObjectTileInfoOffset;
            int offset = BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(listOffset + (int)objectTileSetId * 2)) + Const.ObjectTileInfoOffset;

            int currentSlot = 0;

            while (true)
            {
                int compressedTileId = SNES.rom[offset];

                if (compressedTileId == 0xFF)
                    break;

                if (currentSlot != objectTileSlotId)
                {
                    offset += 6;
                    currentSlot++;
                    continue;
                }

                //Load Object Tiles
                int relativeVramAddr = BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(offset + 1));
                int paletteId = BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(offset + 3));
                int dumpLocation = SNES.rom[offset + 5];
                MainWindow.window.tileE.compressTileInt.Value = compressedTileId;
                MainWindow.window.tileE.vramLocationInt.Value = relativeVramAddr;
                MainWindow.window.tileE.palSetInt.Value = paletteId;
                MainWindow.window.tileE.dumpInt.Value = dumpLocation;
                return;
            }
        }
        private void SetMaxObjectSlots()
        {
            int id;
            if (Const.Id == Const.GameId.MegaManX3 && Level.Id > 0xE) id = (Level.Id - 0xF) + 2; //Buffalo or Beetle
            else id = Level.Id;
            int listOffset = BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(Const.ObjectTileInfoOffset + id * 2)) + Const.ObjectTileInfoOffset;
            int offset = BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(listOffset + (int)objectTileSetId * 2)) + Const.ObjectTileInfoOffset;

            int currentSlot = 0;

            while (true)
            {
                int compressedTileId = SNES.rom[offset];

                if (compressedTileId == 0xFF)
                {
                    objectSlotInt.Maximum = currentSlot - 1;
                    return;
                }

                offset += 6;
                currentSlot++;
            }
        }
        private void SetObjectSlotProperty(int propertyId)
        {
            int id;
            if (Const.Id == Const.GameId.MegaManX3 && Level.Id > 0xE) id = (Level.Id - 0xF) + 2; //Buffalo or Beetle
            else id = Level.Id;
            int listOffset = BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(Const.ObjectTileInfoOffset + id * 2)) + Const.ObjectTileInfoOffset;
            int offset = BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(listOffset + (int)objectTileSetId * 2)) + Const.ObjectTileInfoOffset;

            int currentSlot = 0;

            while (true)
            {
                int compressedTileId = SNES.rom[offset];

                if (compressedTileId == 0xFF)
                    break;

                if (currentSlot != objectTileSlotId)
                {
                    offset += 6;
                    currentSlot++;
                    continue;
                }

                //Load Object Tiles
                switch (propertyId)
                {
                    case 0: //Compressed Tile Id
                        SNES.rom[offset] = (byte)MainWindow.window.tileE.compressTileInt.Value;
                        break;
                    case 1: //VRAM Location
                        ushort vramAddr = (ushort)(int)MainWindow.window.tileE.vramLocationInt.Value;
                        BinaryPrimitives.WriteUInt16LittleEndian(SNES.rom.AsSpan(offset + 1), vramAddr);
                        break;
                    case 2: //Palette Set
                        ushort palId = (ushort)(int)MainWindow.window.tileE.palSetInt.Value;
                        BinaryPrimitives.WriteUInt16LittleEndian(SNES.rom.AsSpan(offset + 3), palId);
                        break;
                    case 3: //Dump Location
                        SNES.rom[offset + 5] = (byte)MainWindow.window.tileE.dumpInt.Value;
                        break;
                    default:
                        break;
                }
                SNES.edit = true;
                return;
            }
        }
        public void UpdateCursor()
        {
            Grid.SetRow(selectSetRect, palId);
        }
        public static byte[] CreateObjectSettingsData(List<List<ObjectSetting>> sourceSettings,int[] sharedList)
        {
            Dictionary<ReadOnlyMemory<byte>, int> dict =
                new Dictionary<ReadOnlyMemory<byte>, int>();

            /*
             * Step 1. Create a dictionary of unique object settings data & keep track of stage keys
             */

            int nextKey = 0; //used as an offset into the object settings data table

            List<List<int>> keyList = new List<List<int>>(sourceSettings.Count);

            foreach (var innerList in sourceSettings)
                keyList.Add(Enumerable.Repeat(0, innerList.Count).ToList());


            for (int id = 0; id < sourceSettings.Count; id++)
            {
                if (sharedList[id] != -1)
                    continue;
                for (int s = 0; s < sourceSettings[id].Count; s++)
                {
                    byte[] slotsData = new byte[sourceSettings[id][s].Slots.Count * 6 + 1];
                    if (slotsData.Length == 1)
                        slotsData[0] = 0xFF;
                    else
                    {
                        slotsData[slotsData.Length - 1] = 0xFF;
                        for (int slot = 0; slot < sourceSettings[id][s].Slots.Count; slot++)
                        {
                            slotsData[slot * 6] = sourceSettings[id][s].Slots[slot].TileId;
                            BinaryPrimitives.WriteUInt16LittleEndian(slotsData.AsSpan(slot * 6 + 1), sourceSettings[id][s].Slots[slot].VramAddress);
                            BinaryPrimitives.WriteUInt16LittleEndian(slotsData.AsSpan(slot * 6 + 3), sourceSettings[id][s].Slots[slot].PaletteId);
                            slotsData[slot * 6 + 5] = sourceSettings[id][s].Slots[slot].PaletteDestination;
                        }
                    }
                    if (!dict.ContainsKey(slotsData))
                    {
                        dict.Add(slotsData, nextKey);
                        nextKey += slotsData.Length;
                    }
                    int value = dict[slotsData];
                    keyList[id][s] = value;
                }
            }

            /*
             * Step 2. Get the length of all the pointers
             */

            int totalPointersLength = 0;

            for (int id = 0; id < sourceSettings.Count; id++)
            {
                totalPointersLength += 2;

                if (sharedList[id] != -1)
                    continue;

                for (int s = 0; s < sourceSettings[id].Count; s++)
                    totalPointersLength += 2;
            }

            /*
             * Step 3. Create the byte array and setup the pointers
             */

            int stagePointersLength = sourceSettings.Count * 2;
            int nextOffset = stagePointersLength;

            byte[] exportData = new byte[nextKey + totalPointersLength];

            //Fix the stage pointers
            for (int i = 0; i < sourceSettings.Count; i++)
            {
                if (sharedList[i] == -1)
                {
                    BinaryPrimitives.WriteUInt16LittleEndian(exportData.AsSpan(i * 2), (ushort)nextOffset);
                    nextOffset += sourceSettings[i].Count * 2;
                }
                else
                {
                    ushort writeOffset = BinaryPrimitives.ReadUInt16LittleEndian(exportData.AsSpan(sharedList[i] * 2));
                    BinaryPrimitives.WriteUInt16LittleEndian(exportData.AsSpan(i * 2), writeOffset);
                }
            }
            //Fix the object setting pointers
            nextOffset = stagePointersLength;
            for (int i = 0; i < sourceSettings.Count; i++)
            {
                if (sharedList[i] != -1)
                    continue;

                for (int st = 0; st < sourceSettings[i].Count; st++)
                    BinaryPrimitives.WriteUInt16LittleEndian(exportData.AsSpan(nextOffset + st * 2), (ushort)(keyList[i][st] + totalPointersLength));
                nextOffset += sourceSettings[i].Count * 2;
            }
            /*
             * Step 4. Copy the unique object settings data
             */
            nextOffset = totalPointersLength;
            foreach (var kvp in dict)
            {
                kvp.Key.Span.CopyTo(exportData.AsSpan(nextOffset));
                nextOffset += kvp.Key.Length;
            }

            // Done
            return exportData;
        }
        public static void GetMaxObjectSettingsFromRom(int[] destAmount, int[] shared = null)
        {
            int objectStages = Const.Id == Const.GameId.MegaManX ? 0x24 : Const.Id == Const.GameId.MegaManX2 ? 0xF : 0x12;

            if (shared == null)
                shared = new int[objectStages];

            for (int i = 0; i < objectStages; i++)
                shared[i] = -1;

            ushort[] offsets = new ushort[objectStages];
            ushort[] sortedOffsets = new ushort[objectStages];
            Buffer.BlockCopy(SNES.rom, Const.ObjectTileInfoOffset, offsets, 0, objectStages * 2);
            Array.Copy(offsets, sortedOffsets, objectStages);
            Array.Sort(sortedOffsets);

            for (int i = 0; i < objectStages; i++)
            {
                if (i == 0) continue;

                ushort stageOffset = offsets[i];

                for (int j = i; j != 0; j--)
                {
                    if (i == j) continue;
                    ushort currentOffset = offsets[j];
                    if (stageOffset == currentOffset)
                    {
                        shared[i] = j;
                        break;
                    }
                }
            }

            int[] maxAmounts = destAmount;

            int maxIndex = 0;
            for (int j = 0; j < offsets.Length; j++)
            {
                if (sortedOffsets[j] > sortedOffsets[maxIndex])
                    maxIndex = j;
            }

            for (int i = 0; i < objectStages; i++)
            {
                if (shared[i] != -1) continue;

                ushort toFindOffset = offsets[i];

                if (Array.IndexOf(sortedOffsets, toFindOffset) != maxIndex)
                {
                    int index = Array.IndexOf(sortedOffsets, toFindOffset);

                    while (sortedOffsets[index] == sortedOffsets[index + 1])
                        index++;
                    maxAmounts[i] = ((sortedOffsets[index + 1] - toFindOffset) / 2);
                }
                else //Last Stage
                {
                    if (Const.Id != Const.GameId.MegaManX)
                    {
                        int tempOffset = Const.ObjectTileInfoOffset + objectStages * 2;
                        int endOffset = offsets[maxIndex] + Const.ObjectTileInfoOffset;

                        int lowestPointer = BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(tempOffset));

                        while (tempOffset != endOffset)
                        {
                            int addr = BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(tempOffset));

                            if (addr < lowestPointer)
                                lowestPointer = addr;
                            tempOffset += 2;
                        }
                        ushort currentOffset = offsets[i];
                        maxAmounts[i] = ((lowestPointer - currentOffset) / 2);
                    }
                    else //MegaMan X Special Case (math just doesnt work cause of how they jumbled around the pointers)
                        maxAmounts[i] = 1;
                }
                System.Diagnostics.Debug.WriteLine($"Stage {i:X2} Max Amount: {maxAmounts[i]}");
            }
        }
        public static List<List<ObjectSetting>> CollecObjectSettingsFromRom(int[] destAmount, int[] shared)
        {
            List<List<ObjectSetting>> sourceSettings = new List<List<ObjectSetting>>();
            int objectStages = Const.Id == Const.GameId.MegaManX ? 0x24 : Const.Id == Const.GameId.MegaManX2 ? 0xF : 0x12;

            for (int i = 0; i < objectStages; i++)
            {
                List<ObjectSetting> objectSettings = new List<ObjectSetting>();
                if (shared[i] != -1)
                {
                    sourceSettings.Add(objectSettings);
                    continue;
                }
                for (int j = 0; j < destAmount[i]; j++)
                {
                    int listOffset = BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(Const.ObjectTileInfoOffset + i * 2)) + Const.ObjectTileInfoOffset;
                    int offset = BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(listOffset + j * 2)) + Const.ObjectTileInfoOffset;

                    ObjectSetting setting = new ObjectSetting();

                    while (true)
                    {
                        byte compressedTileId = SNES.rom[offset];

                        if (compressedTileId == 0xFF)
                            break;

                        ObjectSlot slot = new ObjectSlot();

                        slot.TileId = compressedTileId;
                        slot.VramAddress = BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(offset + 1));
                        slot.PaletteId = BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(offset + 3));
                        slot.PaletteDestination = SNES.rom[offset + 5];
                        setting.Slots.Add(slot);
                        offset += 6;
                    }
                    objectSettings.Add(setting);
                }
                sourceSettings.Add(objectSettings);
            }
            return sourceSettings;
        }
        #endregion Methods

        #region Events
        private void RedrawBtn_Click(object sender, RoutedEventArgs e)
        {
            if (Level.Id >= Const.PlayableLevelsCount || MainWindow.window.tileE.bgTileSetInt.Value == null)
                return;

            int id;
            if (Const.Id == Const.GameId.MegaManX3 && Level.Id > 0xE) id = (Level.Id - 0xF) + 2; //Buffalo or Beetle
            else id = Level.Id;

            int listOffset = BitConverter.ToUInt16(SNES.rom, Const.BackgroundTileInfoOffset + id * 2) + Const.BackgroundTileInfoOffset;
            int offset = BitConverter.ToUInt16(SNES.rom, listOffset + (int)MainWindow.window.tileE.bgTileSetInt.Value * 2) + Const.BackgroundTileInfoOffset;

            Level.TileSet = (int)MainWindow.window.tileE.bgTileSetInt.Value;

            if (BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(offset)) != 0)
            {
                if (freshCheck.IsChecked == true)
                    Level.LoadLevelTiles();
                else
                    Level.LoadDynamicBackgroundTiles();
            }
            else
                Level.LoadLevelTiles();

            MainWindow.window.layoutE.DrawLayout();
            MainWindow.window.layoutE.DrawScreen();
            MainWindow.window.enemyE.DrawLayout();

            MainWindow.window.screenE.DrawScreen();
            MainWindow.window.screenE.DrawTiles();
            MainWindow.window.screenE.DrawTile();

            MainWindow.window.tile32E.DrawTiles();
            MainWindow.window.tile32E.Draw16xTiles();
            MainWindow.window.tile32E.DrawTile();

            MainWindow.window.tile16E.Draw16xTiles();
            MainWindow.window.tile16E.DrawVramTiles();

            MainWindow.window.paletteE.DrawPalette();
            MainWindow.window.paletteE.DrawVramTiles();
        }
        private void bgTileSetInt_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue == null || Level.Id >= Const.PlayableLevelsCount || SNES.rom == null)
                return;

            int id;
            if (Const.Id == Const.GameId.MegaManX3 && Level.Id > 0xE) id = (Level.Id - 0xF) + 2; //Buffalo or Beetle
            else id = Level.Id;

            int listOffset = BitConverter.ToUInt16(SNES.rom, Const.BackgroundTileInfoOffset + id * 2) + Const.BackgroundTileInfoOffset;
            int offset = BitConverter.ToUInt16(SNES.rom, listOffset + (int)MainWindow.window.tileE.bgTileSetInt.Value * 2) + Const.BackgroundTileInfoOffset;
            if (BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(offset)) != 0)
            {
                MainWindow.window.tileE.bgLengthInt.IsEnabled = true;
                MainWindow.window.tileE.bgAddressInt.IsEnabled = true;
                MainWindow.window.tileE.bgSrcBox.IsEnabled = true;
                MainWindow.window.tileE.bgPalInt.IsEnabled = true;
                SetBackgroundValues(offset);
            }
            else
            {
                MainWindow.window.tileE.bgLengthInt.IsEnabled = false;
                MainWindow.window.tileE.bgAddressInt.IsEnabled = false;
                MainWindow.window.tileE.bgSrcBox.IsEnabled = false;
                MainWindow.window.tileE.bgPalInt.IsEnabled = false;
            }
        }
        private void bgLengthInt_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue == null || Level.Id >= Const.PlayableLevelsCount || SNES.rom == null)
                return;

            int id;
            if (Const.Id == Const.GameId.MegaManX3 && Level.Id > 0xE) id = (Level.Id - 0xF) + 2; //Buffalo or Beetle
            else id = Level.Id;

            int listOffset = BitConverter.ToUInt16(SNES.rom, Const.BackgroundTileInfoOffset + id * 2) + Const.BackgroundTileInfoOffset;
            int offset = BitConverter.ToUInt16(SNES.rom, listOffset + (int)MainWindow.window.tileE.bgTileSetInt.Value * 2) + Const.BackgroundTileInfoOffset;

            ushort val = (ushort)(int)e.NewValue;

            if (BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(offset)) == 0 || BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(offset)) == val)
                return;

            BinaryPrimitives.WriteUInt16LittleEndian(SNES.rom.AsSpan(offset), val);
            SNES.edit = true;
        }
        private void bgAddressInt_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue == null || Level.Id >= Const.PlayableLevelsCount || SNES.rom == null)
                return;

            int id;
            if (Const.Id == Const.GameId.MegaManX3 && Level.Id > 0xE) id = (Level.Id - 0xF) + 2; //Buffalo or Beetle
            else id = Level.Id;

            int listOffset = BitConverter.ToUInt16(SNES.rom, Const.BackgroundTileInfoOffset + id * 2) + Const.BackgroundTileInfoOffset;
            int offset = BitConverter.ToUInt16(SNES.rom, listOffset + (int)MainWindow.window.tileE.bgTileSetInt.Value * 2) + Const.BackgroundTileInfoOffset;

            ushort val = (ushort)(int)e.NewValue;

            if (BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(offset)) == 0 || BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(offset + 2)) == val)
                return;

            BinaryPrimitives.WriteUInt16LittleEndian(SNES.rom.AsSpan(offset + 2), val);
            SNES.edit = true;
        }
        private void bgSrcBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (Level.Id >= Const.PlayableLevelsCount || SNES.rom == null || _suppressBgSrcBoxTextChanged)
                return;

            int id;
            if (Const.Id == Const.GameId.MegaManX3 && Level.Id > 0xE) id = (Level.Id - 0xF) + 2; //Buffalo or Beetle
            else id = Level.Id;

            int srcAddr = 0;
            try
            {
                srcAddr = int.Parse(bgSrcBox.Text, System.Globalization.NumberStyles.HexNumber) & 0xFFFFFF;
                if (MainWindow.window.tileE.romOffsetCheck.IsChecked == true)
                    srcAddr = SNES.OffsetToCpu(srcAddr);
            }
            catch (Exception)
            {
                return;
            }
            int listOffset = BitConverter.ToUInt16(SNES.rom, Const.BackgroundTileInfoOffset + id * 2) + Const.BackgroundTileInfoOffset;
            int offset = BitConverter.ToUInt16(SNES.rom, listOffset + (int)MainWindow.window.tileE.bgTileSetInt.Value * 2) + Const.BackgroundTileInfoOffset;


            if (BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(offset)) == 0 || (BinaryPrimitives.ReadInt32LittleEndian(SNES.rom.AsSpan(offset + 4)) & 0x7FFFFF) == (srcAddr & 0x7FFFFF))
                return;
            if (Const.Id == Const.GameId.MegaManX == false)
                srcAddr |= 0x800000;
            BinaryPrimitives.WriteInt32LittleEndian(SNES.rom.AsSpan(offset + 4), (srcAddr & 0x7FFFFF));
            SNES.edit = true;
        }
        private void bgPalInt_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue == null || Level.Id >= Const.PlayableLevelsCount || SNES.rom == null)
                return;

            int id;
            if (Const.Id == Const.GameId.MegaManX3 && Level.Id > 0xE) id = (Level.Id - 0xF) + 2; //Buffalo or Beetle
            else id = Level.Id;

            int listOffset = BitConverter.ToUInt16(SNES.rom, Const.BackgroundTileInfoOffset + id * 2) + Const.BackgroundTileInfoOffset;
            int offset = BitConverter.ToUInt16(SNES.rom, listOffset + (int)MainWindow.window.tileE.bgTileSetInt.Value * 2) + Const.BackgroundTileInfoOffset;

            ushort val = (ushort)(int)e.NewValue;

            if (BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(offset)) == 0 || BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(offset)) == 0 || BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(offset + 7)) == val)
                return;

            BinaryPrimitives.WriteUInt16LittleEndian(SNES.rom.AsSpan(offset + 7), val);
            SNES.edit = true;
        }
        private void romOffsetCheck_Changed(object sender, RoutedEventArgs e)
        {
            if (Level.Id >= Const.PlayableLevelsCount || SNES.rom == null)
                return;

            int id;
            if (Const.Id == Const.GameId.MegaManX3 && Level.Id > 0xE) id = (Level.Id - 0xF) + 2; //Buffalo or Beetle
            else id = Level.Id;

            int listOffset = BitConverter.ToUInt16(SNES.rom, Const.BackgroundTileInfoOffset + id * 2) + Const.BackgroundTileInfoOffset;
            int offset = BitConverter.ToUInt16(SNES.rom, listOffset + (int)MainWindow.window.tileE.bgTileSetInt.Value * 2) + Const.BackgroundTileInfoOffset;

            if (MainWindow.window.tileE.romOffsetCheck.IsChecked == true)
                bgSrcText.Text = "ROM Offset:";
            else
                bgSrcText.Text = "CPU Address:";

            if (BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(offset)) == 0) return;

            int valNew = 0;
            _suppressBgSrcBoxTextChanged = true;
            try
            {
                valNew = int.Parse(bgSrcBox.Text, System.Globalization.NumberStyles.HexNumber) & 0xFFFFFF;
                if (MainWindow.window.tileE.romOffsetCheck.IsChecked == true)
                    valNew = SNES.CpuToOffset(valNew);
                else
                    valNew = SNES.OffsetToCpu(valNew);

                if (Const.Id == Const.GameId.MegaManX && MainWindow.window.tileE.romOffsetCheck.IsChecked == false) valNew |= 0x800000;
                bgSrcBox.Text = valNew.ToString("X6");
            }
            catch (Exception)
            {
                return;
            }
            _suppressBgSrcBoxTextChanged = false;
        }
        private void GearBtn_Click(object sender, RoutedEventArgs e)
        {

        }
        private void LoadBtn_Click(object sender, RoutedEventArgs e)
        {
            LoadWindow loadWindow = new LoadWindow();
            loadWindow.ShowDialog();
        }
        private void Color_Down(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (SNES.rom == null || !MainWindow.window.tileE.objTileSetInt.IsEnabled)
                return;
            Rectangle rect = (Rectangle)sender;
            palId = Grid.GetRow(rect);
            DrawPalette();
            UpdateCursor();
            DrawObjectTiles();
        }
        private void oamBtn_CheckChange(object sender, RoutedEventArgs e)
        {
            if (SNES.rom == null || objTileSetInt.Value == null)
                return;
            DrawObjectTiles();
        }
        private void objectTileSetInt_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue == null || SNES.rom == null || suppressObjectTileInt)
                return;
            suppressObjectTileInt = true;
            objectTileSetId = (int)e.NewValue;
            objectSlotInt.Value = 0;

            if (MainWindow.window.tileE.objectFreshCheck.IsChecked == true)
            {
                for (int i = 0; i < 0x80; i++)
                    Palette[i >> 4, i & 0xF] = Color.FromRgb(0, 0, 0);

                Array.Clear(ObjectTiles);

                int[] palsToLoad = { 0, 0x14, 0x1C, 0x40 };
                int[] palsDest = { 0x10, 0x0, 0x20, 0x30 };

                for (int i = 0; i < 4; i++)
                {
                    int palId = palsToLoad[i];
                    int dumpLocation = palsDest[i];

                    int infoOffset = SNES.CpuToOffset(BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(Const.PaletteInfoOffset + palId)), Const.PaletteBank);

                    while (SNES.rom[infoOffset] != 0)
                    {
                        int colorCount = SNES.rom[infoOffset]; //how many colors are going to be dumped
                        int colorIndex = SNES.rom[infoOffset + 3] + dumpLocation - 0x80; //which color index to start dumping at
                        int colorOffset = SNES.CpuToOffset(BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(infoOffset + 1)) + (Const.PaletteColorBank << 16)); //where the colors are located

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

                Array.Copy(SNES.rom, Const.MegaManTilesOffset, ObjectTiles, 0, 32 * 16 * 2);
                Array.Copy(Level.DefaultObjectTiles, 0, ObjectTiles, 0x1000, Level.DefaultObjectTiles.Length);

                //Green Charge Shot
                Array.Copy(SNES.rom, Const.MegaManGreenChargeShotTilesOffset[0], ObjectTiles, 0x400, 0x100);
                Array.Copy(SNES.rom, Const.MegaManGreenChargeShotTilesOffset[1], ObjectTiles, 0x600, 0x100);

            }

            SetMaxObjectSlots();
            SetObjectSlotValues();
            DrawObjectTiles();
            DrawPalette();
            UpdateCursor();
            suppressObjectTileInt = false;
        }
        private void objectSlotInt_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue == null || SNES.rom == null || suppressObjectTileInt)
                return;
            suppressObjectTileInt = true;
            objectTileSlotId = (int)e.NewValue;
            SetObjectSlotValues();
            suppressObjectTileInt = false;
        }
        private void compressTileId_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue == null || SNES.rom == null || suppressObjectTileInt)
                return;
            SetObjectSlotProperty(0);
        }
        private void vramLocationInt_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue == null || SNES.rom == null || suppressObjectTileInt)
                return;
            SetObjectSlotProperty(1);
        }
        private void palSetInt_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue == null || SNES.rom == null || ((int)e.NewValue & 1) != 0 || suppressObjectTileInt)
                return;
            SetObjectSlotProperty(2);
        }
        private void dumpInt_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue == null || SNES.rom == null || suppressObjectTileInt)
                return;
            SetObjectSlotProperty(3);
        }
        private void objectTilesImage_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (SNES.rom == null || !MainWindow.window.tileE.objTileSetInt.IsEnabled || MainWindow.window.tileE.vramSelectToggleBtn.IsChecked == false)
                return;
            Point pos = e.GetPosition(objectTilesImage);
            int cX = SNES.GetSelectedTile((int)pos.X, MainWindow.window.tileE.objectTilesImage.ActualWidth, 16);
            int cY = SNES.GetSelectedTile((int)pos.Y, MainWindow.window.tileE.objectTilesImage.ActualHeight, 16);
            int oamBase = (MainWindow.window.tileE.oam1Btn.IsChecked != false) ? 0 : 0x100;
            int selectedTile = cX + (cY * 16) + oamBase;
            vramLocationInt.Value = selectedTile * 16;
        }
        private void ReDrawVramBtn_Click(object sender, RoutedEventArgs e)
        {
            if (SNES.rom == null || !MainWindow.window.tileE.objTileSetInt.IsEnabled)
                return;
            if (MainWindow.window.tileE.objectFreshCheck.IsChecked == true)
            {
                for (int i = 0; i < 0x80; i++)
                    Palette[i >> 4, i & 0xF] = Color.FromRgb(0, 0, 0);

                Array.Clear(ObjectTiles);

                int[] palsToLoad = { 0, 0x14, 0x1C, 0x40 };
                int[] palsDest = { 0x10, 0x0, 0x20, 0x30 };

                for (int i = 0; i < 4; i++)
                {
                    int palId = palsToLoad[i];
                    int dumpLocation = palsDest[i];

                    int infoOffset = SNES.CpuToOffset(BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(Const.PaletteInfoOffset + palId)), Const.PaletteBank);

                    while (SNES.rom[infoOffset] != 0)
                    {
                        int colorCount = SNES.rom[infoOffset]; //how many colors are going to be dumped
                        int colorIndex = SNES.rom[infoOffset + 3] + dumpLocation - 0x80; //which color index to start dumping at
                        int colorOffset = SNES.CpuToOffset(BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(infoOffset + 1)) + (Const.PaletteColorBank << 16)); //where the colors are located

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

                Array.Copy(SNES.rom, Const.MegaManTilesOffset, ObjectTiles, 0, 32 * 16 * 2);
                Array.Copy(Level.DefaultObjectTiles, 0, ObjectTiles, 0x1000, Level.DefaultObjectTiles.Length);

                //Green Charge Shot
                Array.Copy(SNES.rom, Const.MegaManGreenChargeShotTilesOffset[0], ObjectTiles, 0x400, 0x100);
                Array.Copy(SNES.rom, Const.MegaManGreenChargeShotTilesOffset[1], ObjectTiles, 0x600, 0x100);
            }
            DrawObjectTiles();
            if (MainWindow.window.tileE.objectFreshCheck.IsChecked == true)
                DrawPalette();
        }
        private void vramSelectToggleBtn_Click(object sender, RoutedEventArgs e)
        {

        }
        private void gridBtn_Click(object sender, RoutedEventArgs e)
        {
            if (objectTileGrid.ShowGridLines)
                objectTileGrid.ShowGridLines = false;
            else
                objectTileGrid.ShowGridLines = true;
        }
        #endregion Events
    }
}