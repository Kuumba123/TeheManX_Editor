using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using SkiaSharp;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;

namespace TeheManX_Editor.Forms;

public partial class TileEditor : UserControl
{
    #region Fields
    public static byte[] ObjectTiles = new byte[0x4000];
    public static Color[,] Palette = new Color[8, 16]; //Converted to 24-bit Color
    public static byte[] Bank7F = new byte[0x7E00];
    public static List<List<BGSetting>> BGSettings;
    public static List<List<ObjectSetting>> ObjectSettings;
    static SKBitmap vramTiles = new SKBitmap(128, 128, SKColorType.Bgra8888, SKAlphaType.Premul);

    public static int bgTileSetId;
    public static int bgEntrySlotId;
    public static int objectTileSetId;
    public static int objectTileSlotId;
    public static int palId = 1;
    public static int oamIndex;

    public static double scale = 4;
    internal static bool ShowTileGrid;
    #endregion Fields

    #region Properties
    private bool _suppressBgSrcBoxTextChanged;
    Rectangle selectSetRect = new Rectangle() { IsHitTestVisible = false, StrokeThickness = 2.5, StrokeDashArray = new Avalonia.Collections.AvaloniaList<double>() { 2.2 }, Stroke = Brushes.PapayaWhip };
    private bool suppressInts;
    #endregion Properties

    #region Constructors
    public TileEditor()
    {
        suppressInts = true;
        InitializeComponent();

        for (int col = 0; col < 16; col++)
            paletteGrid.ColumnDefinitions.Add(new ColumnDefinition());
        for (int row = 0; row < 8; row++)
            paletteGrid.RowDefinitions.Add(new RowDefinition());

        for (int y = 0; y < 8; y++)
        {
            for (int x = 0; x < 16; x++)
            {
                //Create Color
                Rectangle rect = new Rectangle();
                rect.Focusable = false;
                rect.Width = 16;
                rect.Height = 16;
                rect.PointerPressed += Color_Down;
                rect.Fill = Brushes.Transparent;
                rect.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch;
                rect.VerticalAlignment = Avalonia.Layout.VerticalAlignment.Stretch;
                Grid.SetColumn(rect, x);
                Grid.SetRow(rect, y);
                paletteGrid.Children.Add(rect);
            }
        }
        Grid.SetColumnSpan(selectSetRect, 16);
        paletteGrid.Children.Add(selectSetRect);
        //objectTilesImage.Source = vramTiles;
        suppressInts = false;
    }
    #endregion Constructors

    #region Methods
    public void CollectData()
    {
        CollectBGData();
        CollectOBJData();

        suppressInts = true;

        //Set Max Compressed Tiles
        if (Const.Id == Const.GameId.MegaManX)
            compressTileInt.Maximum = Const.MegaManX.CompressedTilesAmount - 1;
        else if (Const.Id == Const.GameId.MegaManX2)
            compressTileInt.Maximum = Const.MegaManX2.CompressedTilesAmount - 1;
        else
            compressTileInt.Maximum = Const.MegaManX3.CompressedTilesAmount - 1;
        compressTileInt.Value = 0;

        suppressInts = false;
    }
    public void CollectBGData()
    {
        if (Level.Project.BGSettings == null)
        {
            int bgStages = Const.Id == Const.GameId.MegaManX3 ? 0xF : Const.PlayableLevelsCount;
            int[] maxAmount = new int[bgStages];
            int[] shared = new int[bgStages];
            GetMaxBGSettingsFromRom(maxAmount, shared);
            BGSettings = CollecBGSettingsFromRom(maxAmount, shared);
        }
        else
            BGSettings = Level.Project.BGSettings;
    }
    public void CollectOBJData()
    {
        if (Level.Project.ObjectSettings == null)
        {
            int objectStages = Const.Id == Const.GameId.MegaManX ? 0x24 : Const.Id == Const.GameId.MegaManX2 ? 0xF : 0x12;
            int[] maxAmount = new int[objectStages];
            int[] shared = new int[objectStages];
            GetMaxObjectSettingsFromRom(maxAmount, shared);
            ObjectSettings = CollecObjectSettingsFromRom(maxAmount, shared);
        }
        else
            ObjectSettings = Level.Project.ObjectSettings;
    }
    public void AssignLimits()
    {
        if (Level.Id >= Const.PlayableLevelsCount || (Const.Id == Const.GameId.MegaManX3 && Level.Id > 0xE))
        {
            // Disable UI
            bgTileSetInt.IsEnabled = false;
            bgEntrySlotInt.IsEnabled = false;
            bgLengthInt.IsEnabled = false;
            bgAddressInt.IsEnabled = false;
            bgSrcBox.IsEnabled = false;
            bgPalInt.IsEnabled = false;

            objTileSetInt.IsEnabled = false;
            objectSlotInt.IsEnabled = false;
            compressTileInt.IsEnabled = false;
            vramLocationInt.IsEnabled = false;
            palSetInt.IsEnabled = false;
            dumpInt.IsEnabled = false;
            oam1Btn.IsEnabled = false;
            oam2Btn.IsEnabled = false;
            return;
        }
        suppressInts = true;

        /*
         *  Background Tile Settings
         */

        if (BGSettings[Level.Id].Count == 0)
        {
            //...
            bgTileSetInt.IsEnabled = false;
            bgEntrySlotInt.IsEnabled = false;
            bgLengthInt.IsEnabled = false;
            bgAddressInt.IsEnabled = false;
            bgSrcBox.IsEnabled = false;
            bgPalInt.IsEnabled = false;
        }
        else
        {
            //Get Max Amount of BG Tile Settings
            int max = BGSettings[Level.Id].Count - 1;
            bgTileSetInt.Maximum = max;
            if (bgTileSetInt.Value > max)
                bgTileSetInt.Value = max;
            bgTileSetId = bgTileSetInt.Value;
            bgEntrySlotId = 0;
            bgEntrySlotInt.Value = 0;
            bgTileSetInt.IsEnabled = true;
            SetBackgroundEntryValues();
        }

        /*
         *  Object Tile Settings
         */

        if (ObjectSettings[Level.Id].Count == 0)
        {
            //...
            objTileSetInt.IsEnabled = false;
            objectSlotInt.IsEnabled = false;
            compressTileInt.IsEnabled = false;
            vramLocationInt.IsEnabled = false;
            palSetInt.IsEnabled = false;
            dumpInt.IsEnabled = false;
            oam1Btn.IsEnabled = false;
            oam2Btn.IsEnabled = false;
        }
        else
        {
            //Get Max Amount of Object Tile Settings
            int max = ObjectSettings[Level.Id].Count - 1;
            objTileSetInt.Maximum = max;
            if (objTileSetInt.Value > max)
                objTileSetInt.Value = max;
            objectTileSetId = objTileSetInt.Value;
            objectTileSlotId = 0;
            objectSlotInt.Value = 0;
            objectSlotInt.Maximum = ObjectSettings[Level.Id][objectTileSetId].Slots.Count - 1;
            objTileSetInt.IsEnabled = true;
            oam1Btn.IsEnabled = true;
            oam2Btn.IsEnabled = true;
            SetObjectSlotValues();
            LoadDefaultObjectTiles();
            DrawObjectTiles();
            DrawPalette();
            UpdateCursor();
        }

        suppressInts = false;
    }
    private void SetBackgroundEntryValues()
    {
        int id = Level.Id;
        if (BGSettings[id][bgTileSetId].Slots.Count == 0)
        {
            bgEntrySlotInt.IsEnabled = false;
            bgLengthInt.IsEnabled = false;
            bgAddressInt.IsEnabled = false;
            bgSrcBox.IsEnabled = false;
            bgPalInt.IsEnabled = false;
            return;
        }

        int length = BGSettings[id][bgTileSetId].Slots[bgEntrySlotId].Length;
        int dest = BGSettings[id][bgTileSetId].Slots[bgEntrySlotId].VramAddress;
        int srcAddr = BGSettings[id][bgTileSetId].Slots[bgEntrySlotId].CpuAddress;
        int palId = BGSettings[id][bgTileSetId].Slots[bgEntrySlotId].PaletteId;

        if (romOffsetCheck.IsChecked == true)
            srcAddr = SNES.CpuToOffset(srcAddr);

        bgEntrySlotInt.IsEnabled = true;

        bgLengthInt.Value = length;
        bgAddressInt.Value = dest;
        _suppressBgSrcBoxTextChanged = true;
        bgSrcBox.Text = srcAddr.ToString("X6");
        _suppressBgSrcBoxTextChanged = false;
        bgPalInt.Value = palId;

        bgLengthInt.IsEnabled = true;
        bgAddressInt.IsEnabled = true;
        bgSrcBox.IsEnabled = true;
        bgPalInt.IsEnabled = true;
        bgEntrySlotInt.Maximum = BGSettings[id][bgTileSetId].Slots.Count - 1;
    }
    public unsafe void DrawObjectTiles()
    {
        // MMX3 special cases
        int id = (Const.Id == Const.GameId.MegaManX3 && Level.Id > 0xE) ? (Level.Id - 0xF) + 2 : Level.Id;

        if (ObjectSettings[id][objectTileSetId].Slots.Count != 0)
        {

            for (int i = 0; i < ObjectSettings[id][objectTileSetId].Slots.Count; i++)
            {
                //Load Object Tiles
                ushort relativeVramAddr = ObjectSettings[id][objectTileSetId].Slots[i].VramAddress;

                byte compressedTileId = ObjectSettings[id][objectTileSetId].Slots[i].TileId;

                int specOffset = BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(Const.CompressedTilesSwapInfoOffset + compressedTileId * 2)) + Const.CompressedTilesSwapInfoOffset;
                int srcOffset = 0;

                Level.DecompressTiles2(compressedTileId, Bank7F, 0);

                if (relativeVramAddr != 0x8000 || Const.Id == Const.GameId.MegaManX)
                {
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
                }

                //Load Object Palette
                ushort palId = ObjectSettings[id][objectTileSetId].Slots[i].PaletteId;
                byte dumpLocation = ObjectSettings[id][objectTileSetId].Slots[i].PaletteDestination;

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
        }
        objectTilesImage.InvalidateVisual();
    }
    public void DrawPalette()
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
        int id = Level.Id;
        if (ObjectSettings[id][objectTileSetId].Slots.Count == 0)
        {
            objectSlotInt.IsEnabled = false;
            compressTileInt.IsEnabled = false;
            vramLocationInt.IsEnabled = false;
            palSetInt.IsEnabled = false;
            dumpInt.IsEnabled = false;
            return;
        }
        compressTileInt.Value = ObjectSettings[id][objectTileSetId].Slots[objectTileSlotId].TileId;
        vramLocationInt.Value = ObjectSettings[id][objectTileSetId].Slots[objectTileSlotId].VramAddress;
        palSetInt.Value = ObjectSettings[id][objectTileSetId].Slots[objectTileSlotId].PaletteId;
        dumpInt.Value = ObjectSettings[id][objectTileSetId].Slots[objectTileSlotId].PaletteDestination;

        objectSlotInt.IsEnabled = true;
        compressTileInt.IsEnabled = true;
        vramLocationInt.IsEnabled = true;
        palSetInt.IsEnabled = true;
        dumpInt.IsEnabled = true;
    }
    public void UpdateCursor()
    {
        Grid.SetRow(selectSetRect, palId);
    }
    public static byte[] CreateObjectSettingsData(List<List<ObjectSetting>> sourceSettings, int[] sharedList)
    {
        Dictionary<byte[], int> dict = new Dictionary<byte[], int>(ByteArrayComparer.Default);

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
            kvp.Key.CopyTo(exportData.AsSpan(nextOffset));
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
    public static byte[] CreateBGSettingsData(List<List<BGSetting>> sourceSettings, int[] sharedList)
    {
        Dictionary<byte[], int> dict = new Dictionary<byte[], int>(ByteArrayComparer.Default);

        /*
         * Step 1. Create a dictionary of unique object settings data & keep track of stage keys
         */

        int nextKey = 0; //used as an offset into the background settings data table

        List<List<int>> keyList = new List<List<int>>(sourceSettings.Count);

        foreach (var innerList in sourceSettings)
            keyList.Add(Enumerable.Repeat(0, innerList.Count).ToList());


        for (int id = 0; id < sourceSettings.Count; id++)
        {
            if (sharedList[id] != -1)
                continue;
            for (int s = 0; s < sourceSettings[id].Count; s++)
            {
                byte[] slotsData = new byte[sourceSettings[id][s].Slots.Count * 9 + 2];
                if (slotsData.Length == 2)
                    ;
                else
                {
                    if (sourceSettings[id][s].Slots.Count == 0)
                        slotsData = new byte[2]; //Single empty slot optimization
                    else
                    {
                        BinaryPrimitives.WriteUInt16LittleEndian(slotsData.AsSpan(slotsData.Length - 2), 0);
                        for (int slot = 0; slot < sourceSettings[id][s].Slots.Count; slot++)
                        {
                            BinaryPrimitives.WriteUInt16LittleEndian(slotsData.AsSpan(slot * 9 + 0), sourceSettings[id][s].Slots[slot].Length);
                            BinaryPrimitives.WriteUInt16LittleEndian(slotsData.AsSpan(slot * 9 + 2), sourceSettings[id][s].Slots[slot].VramAddress);
                            BinaryPrimitives.WriteUInt16LittleEndian(slotsData.AsSpan(slot * 9 + 4), (ushort)sourceSettings[id][s].Slots[slot].CpuAddress);
                            slotsData[slot * 9 + 6] = (byte)(sourceSettings[id][s].Slots[slot].CpuAddress >> 16);
                            BinaryPrimitives.WriteUInt16LittleEndian(slotsData.AsSpan(slot * 9 + 7), sourceSettings[id][s].Slots[slot].PaletteId);
                        }
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
        //Fix the background setting pointers
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
         * Step 4. Copy the unique background settings data
         */
        nextOffset = totalPointersLength;
        foreach (var kvp in dict)
        {
            kvp.Key.CopyTo(exportData.AsSpan(nextOffset));
            nextOffset += kvp.Key.Length;
        }

        // Done
        return exportData;
    }
    public static void GetMaxBGSettingsFromRom(int[] destAmount, int[] shared = null)
    {
        int bgStages = Const.Id == Const.GameId.MegaManX3 ? 0xF : Const.PlayableLevelsCount;

        if (shared == null)
            shared = new int[bgStages];

        for (int i = 0; i < bgStages; i++)
            shared[i] = -1;

        ushort[] offsets = new ushort[bgStages];
        ushort[] sortedOffsets = new ushort[bgStages];
        Buffer.BlockCopy(SNES.rom, Const.BackgroundTileInfoOffset, offsets, 0, bgStages * 2);
        Array.Copy(offsets, sortedOffsets, bgStages);
        Array.Sort(sortedOffsets);

        for (int i = 0; i < bgStages; i++)
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

        for (int i = 0; i < bgStages; i++)
        {
            if (shared[i] != -1)
            {
                maxAmounts[i] = maxAmounts[shared[i]];
                continue;
            }

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
                int tempOffset = Const.BackgroundTileInfoOffset + bgStages * 2;
                int endOffset = offsets[maxIndex] + Const.BackgroundTileInfoOffset;

                int lowestPointer = BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(tempOffset));

                while (tempOffset != endOffset)
                {
                    int addr = BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(tempOffset));

                    if (addr < lowestPointer)
                        lowestPointer = addr;
                    tempOffset += 2;
                }
                ushort currentOffset = offsets[i];
                int max = ((lowestPointer - currentOffset) / 2);

                for (int j = 0; j < max; j++)
                {
                    if (BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(currentOffset + j * 2)) == 0)
                    {
                        max = j;
                        break;
                    }
                }

                maxAmounts[i] = max;
            }
        }
    }
    public static List<List<BGSetting>> CollecBGSettingsFromRom(int[] destAmount, int[] shared)
    {
        List<List<BGSetting>> sourceSettings = new List<List<BGSetting>>();
        int bgStages = Const.Id == Const.GameId.MegaManX3 ? 0xF : Const.PlayableLevelsCount;

        for (int i = 0; i < bgStages; i++)
        {
            List<BGSetting> bgSettings = new List<BGSetting>();
            for (int j = 0; j < destAmount[i]; j++)
            {
                int listOffset = BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(Const.BackgroundTileInfoOffset + i * 2)) + Const.BackgroundTileInfoOffset;
                int offset = BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(listOffset + j * 2)) + Const.BackgroundTileInfoOffset;

                BGSetting setting = new BGSetting();

                while (true)
                {
                    ushort length = BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(offset));

                    if (length == 0)
                        break;

                    BGSlot slot = new BGSlot();

                    slot.Length = BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(offset));
                    slot.VramAddress = BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(offset + 2));
                    slot.CpuAddress = BinaryPrimitives.ReadInt32LittleEndian(SNES.rom.AsSpan(offset + 4)) & 0xFFFFFF;
                    slot.PaletteId = BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(offset + 7));
                    setting.Slots.Add(slot);
                    offset += 9;
                }
                bgSettings.Add(setting);
            }
            sourceSettings.Add(bgSettings);
        }
        return sourceSettings;
    }
    private void LoadDefaultObjectTiles()
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
    #endregion Methods

    #region Events
    private async void RedrawBtn_Click(object sender, RoutedEventArgs e)
    {
        if (Level.Id >= Const.PlayableLevelsCount || bgTileSetInt.Value == null || !bgTileSetInt.IsEnabled)
            return;

        Level.TileSet = bgTileSetInt.Value;

        if (bgLengthInt.IsEnabled)
        {
            if (freshCheck.IsChecked == true)
                await Level.LoadLevelTiles();
            else
                await Level.LoadDynamicBackgroundTiles();
        }
        else
            await Level.LoadLevelTiles();
        Level.DecodeAllTiles();

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

        //MainWindow.window.paletteE.DrawPalette();
        //MainWindow.window.paletteE.DrawVramTiles();
    }
    private void bgTileSetInt_ValueChanged(object sender, int newValue)
    {
        if (Level.Id >= Const.PlayableLevelsCount || SNES.rom == null || suppressInts)
            return;
        suppressInts = true;
        bgTileSetId = newValue;
        bgEntrySlotInt.Value = 0;
        bgEntrySlotId = 0;
        SetBackgroundEntryValues();
        suppressInts = false;
    }
    private void bgEntrySlotInt_ValueChanged(object sender, int newValue)
    {
        if (Level.Id >= Const.PlayableLevelsCount || SNES.rom == null || suppressInts)
            return;
        suppressInts = true;
        bgEntrySlotId = newValue;
        SetBackgroundEntryValues();
        suppressInts = false;
    }
    private void bgLengthInt_ValueChanged(object sender, int newValue)
    {
        if (Level.Id >= Const.PlayableLevelsCount || SNES.rom == null || suppressInts)
            return;
        int id = Level.Id;
        ushort val = (ushort)newValue;
        if (val == BGSettings[id][bgTileSetId].Slots[bgEntrySlotId].Length)
            return;
        BGSettings[id][bgTileSetId].Slots[bgEntrySlotId].Length = val;
        SNES.edit = true;
    }
    private void bgAddressInt_ValueChanged(object sender, int newValue)
    {
        if (Level.Id >= Const.PlayableLevelsCount || SNES.rom == null || suppressInts)
            return;
        int id = Level.Id;
        ushort val = (ushort)newValue;
        if (val == BGSettings[id][bgTileSetId].Slots[bgEntrySlotId].VramAddress)
            return;
        BGSettings[id][bgTileSetId].Slots[bgEntrySlotId].VramAddress = val;
        SNES.edit = true;
    }
    private void bgSrcBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (Level.Id >= Const.PlayableLevelsCount || SNES.rom == null || _suppressBgSrcBoxTextChanged || suppressInts)
            return;

        int id;
        if (Const.Id == Const.GameId.MegaManX3 && Level.Id > 0xE) id = (Level.Id - 0xF) + 2; //Buffalo or Beetle
        else id = Level.Id;

        int srcAddr = 0;
        try
        {
            srcAddr = int.Parse(bgSrcBox.Text, System.Globalization.NumberStyles.HexNumber) & 0xFFFFFF;
            if (romOffsetCheck.IsChecked == true)
                srcAddr = SNES.OffsetToCpu(srcAddr);
        }
        catch (Exception)
        {
            return;
        }

        if ((srcAddr & 0x7FFFFF) == (BGSettings[id][bgTileSetId].Slots[bgEntrySlotId].CpuAddress & 0x7FFFFF))
            return;

        if (Const.Id == Const.GameId.MegaManX == false)
            srcAddr |= 0x800000;

        BGSettings[id][bgTileSetId].Slots[bgEntrySlotId].CpuAddress = srcAddr;
        SNES.edit = true;
    }
    private void bgPalInt_ValueChanged(object sender, int newValue)
    {
        if (Level.Id >= Const.PlayableLevelsCount || SNES.rom == null || suppressInts)
            return;
        int id = Level.Id;
        ushort val = (ushort)newValue;
        if (val == BGSettings[id][bgTileSetId].Slots[bgEntrySlotId].PaletteId)
            return;
        BGSettings[id][bgTileSetId].Slots[bgEntrySlotId].PaletteId = val;
        SNES.edit = true;
    }
    private void romOffsetCheck_Changed(object sender, RoutedEventArgs e)
    {
        if (Level.Id >= Const.PlayableLevelsCount || SNES.rom == null || suppressInts)
            return;

        int id;
        if (Const.Id == Const.GameId.MegaManX3 && Level.Id > 0xE) id = (Level.Id - 0xF) + 2; //Buffalo or Beetle
        else id = Level.Id;

        if (MainWindow.window.tileE.romOffsetCheck.IsChecked == true)
            bgSrcText.Text = "ROM Offset:";
        else
            bgSrcText.Text = "CPU Address:";

        if (!MainWindow.window.tileE.bgLengthInt.IsEnabled) return;

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
    private async void EditBGSlotCountBtn_Click(object sender, RoutedEventArgs e)
    {
        if (SNES.rom == null || !MainWindow.window.tileE.bgTileSetInt.IsEnabled || Level.Id >= Const.PlayableLevelsCount)
            return;

        List<BGSetting> trueCopy = BGSettings[Level.Id].Select(os => new BGSetting(os)).ToList();

        Window window = new Window() { WindowStartupLocation = WindowStartupLocation.CenterScreen, Title = "BG Tiles Settings" };
        window.Width = 310;
        window.MinWidth = 310;
        window.MaxWidth = 310;
        window.Height = 760;

        StackPanel stackPanel = new StackPanel();

        for (int i = 0; i < trueCopy.Count; i++)
        {
            DataEntry entry = new DataEntry(window, trueCopy, i);
            stackPanel.Children.Add(entry);
        }

        ScrollViewer scrollViewer = new ScrollViewer() { AllowAutoHide = false };
        scrollViewer.Content = stackPanel;

        Button confirmBtn = new Button() { Content = "Confirm" };
        confirmBtn.Click += async (s, ev) =>
        {
            for (int i = 0; i < trueCopy.Count; i++)
            {
                int neededSlots = ((DataEntry)(stackPanel.Children[i])).slotCount;

                while (trueCopy[i].Slots.Count < neededSlots)
                {
                    BGSlot slot = new BGSlot();
                    slot.Length = 32;
                    slot.VramAddress = 0x1100;
                    int address = Const.Id == Const.GameId.MegaManX ? 0x808000 : 0x8000;
                    slot.CpuAddress = address;
                    slot.PaletteId = 0xC;
                    trueCopy[i].Slots.Add(slot);
                }
                while (trueCopy[i].Slots.Count > neededSlots)
                    trueCopy[i].Slots.RemoveAt(trueCopy[i].Slots.Count - 1);
            }

            List<BGSetting> uneditedList = BGSettings[Level.Id];
            BGSettings[Level.Id] = trueCopy;

            int bgStages = Const.Id == Const.GameId.MegaManX3 ? 0xF : Const.PlayableLevelsCount;

            int[] maxAmount = new int[bgStages];
            int[] shared = new int[bgStages];

            if (Level.Project.BGSettings != null) //no stages share data when using json
            {
                for (int i = 0; i < bgStages; i++)
                    shared[i] = -1;
            }
            else
                GetMaxBGSettingsFromRom(maxAmount, shared);

            int length = CreateBGSettingsData(BGSettings, shared).Length;

            if (length > Const.BackgroundTileInfoLength && Level.Project.BGSettings == null)
            {
                BGSettings[Level.Id] = uneditedList;
                await MessageBox.Show(MainWindow.window, $"The new BG Tile Info length exceeds the maximum allowed space in the ROM (0x{length:X} vs max of 0x{Const.BackgroundTileInfoLength:X}). Please lower some counts for this or another stage.");
                return;
            }

            AssignLimits();
            SNES.edit = true;
            await MessageBox.Show(MainWindow.window, "BG Slot counts updated!");
            window.Close();
        };
        Grid.SetRow(confirmBtn, 2);

        Button addBtn = new Button() { Content = "Add Setting" };
        addBtn.Click += (s, e) =>
        {
            int newIndex = trueCopy.Count;
            BGSlot slot = new BGSlot();
            slot.Length = 32;
            slot.VramAddress = 0x1100;
            int address = Const.Id == Const.GameId.MegaManX ? 0x808000 : 0x8000;
            slot.CpuAddress = address;
            slot.PaletteId = 0xC;

            BGSetting bgSetting = new BGSetting();
            bgSetting.Slots.Add(slot);
            trueCopy.Add(bgSetting);

            DataEntry entry = new DataEntry(window, trueCopy, newIndex);
            stackPanel.Children.Add(entry);
        };
        Grid.SetRow(addBtn, 1);

        Grid grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition());
        grid.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });
        grid.Children.Add(scrollViewer);
        grid.Children.Add(confirmBtn);
        grid.Children.Add(addBtn);
        grid.Background = Brushes.Black;
        window.Content = grid;
        await window.ShowDialog(MainWindow.window);
    }
    private async void LoadBtn_Click(object sender, RoutedEventArgs e)
    {
        LoadWindow loadWindow = new LoadWindow();
        await loadWindow.ShowDialog(MainWindow.window);
    }
    private void Color_Down(object sender, PointerPressedEventArgs e)
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
        if (SNES.rom == null || suppressInts)
            return;
        oamIndex = oam1Btn.IsChecked != false ? 0x0000 : 0x2000;
        DrawObjectTiles();
    }
    private void objectTileSetInt_ValueChanged(object sender, int newValue)
    {
        if (SNES.rom == null || suppressInts)
            return;
        suppressInts = true;
        objectTileSetId = newValue;
        objectTileSlotId = 0;
        objectSlotInt.Value = 0;

        if (MainWindow.window.tileE.objectFreshCheck.IsChecked == true)
            LoadDefaultObjectTiles();

        objectSlotInt.Maximum = ObjectSettings[Level.Id][objectTileSetId].Slots.Count - 1;
        SetObjectSlotValues();
        DrawObjectTiles();
        DrawPalette();
        UpdateCursor();
        suppressInts = false;
    }
    private void objectSlotInt_ValueChanged(object sender, int newValue)
    {
        if (SNES.rom == null || suppressInts)
            return;
        suppressInts = true;
        objectTileSlotId = newValue;
        SetObjectSlotValues();
        suppressInts = false;
    }
    private void compressTileId_ValueChanged(object sender, int newValue)
    {
        if (SNES.rom == null || suppressInts)
            return;
        int id = Level.Id;
        byte val = (byte)newValue;
        if (val == ObjectSettings[id][objectTileSetId].Slots[objectTileSlotId].TileId)
            return;
        ObjectSettings[id][objectTileSetId].Slots[objectTileSlotId].TileId = val;
        SNES.edit = true;
    }
    private void vramLocationInt_ValueChanged(object sender, int newValue)
    {
        if (SNES.rom == null || suppressInts)
            return;
        int id = Level.Id;
        ushort val = (ushort)newValue;
        if (val == ObjectSettings[id][objectTileSetId].Slots[objectTileSlotId].VramAddress)
            return;
        ObjectSettings[id][objectTileSetId].Slots[objectTileSlotId].VramAddress = val;
        SNES.edit = true;
    }
    private void palSetInt_ValueChanged(object sender, int newValue)
    {
        if (SNES.rom == null || suppressInts)
            return;
        int id = Level.Id;
        ushort val = (ushort)newValue;
        if (val == ObjectSettings[id][objectTileSetId].Slots[objectTileSlotId].PaletteId)
            return;
        ObjectSettings[id][objectTileSetId].Slots[objectTileSlotId].PaletteId = val;
        SNES.edit = true;
    }
    private void dumpInt_ValueChanged(object sender, int newValue)
    {
        if (SNES.rom == null || suppressInts)
            return;
        int id = Level.Id;
        byte val = (byte)newValue;
        if (val == ObjectSettings[id][objectTileSetId].Slots[objectTileSlotId].PaletteDestination)
            return;
        ObjectSettings[id][objectTileSetId].Slots[objectTileSlotId].PaletteDestination = val;
        SNES.edit = true;
    }
    private void objectTilesImage_PointerPressed(object sender, PointerPressedEventArgs e)
    {
        if (SNES.rom == null || suppressInts || !objTileSetInt.IsEnabled)
            return;
        Point pos = e.GetPosition(objectTilesImage);
        int cX = SNES.GetSelectedTile((int)pos.X, objectTilesImage.Width, 16);
        int cY = SNES.GetSelectedTile((int)pos.Y, objectTilesImage.Height, 16);
        int oamBase = (oam1Btn.IsChecked != false) ? 0 : 0x100;
        int selectedTile = cX + (cY * 16) + oamBase;
        vramLocationInt.Value = selectedTile * 16;
    }
    private void objectTilesImage_RenderEvent(object? sender, SkiaCanvasEventArgs e)
    {
        int stride = vramTiles.RowBytes;
        int set = palId;
        unsafe
        {
            byte* buffer = (byte*)vramTiles.GetPixels();

            /*
            *  Draw 0x200 tiles from VRAM
            */

            int readBase = oamIndex;

            fixed (byte* objectTilesPtr = ObjectTiles)
            {
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
                                int p0 = (*(objectTilesPtr + base1) >> bit) & 1;
                                int p1 = (*(objectTilesPtr + base1 + 1) >> bit) & 1;
                                int p2 = (*(objectTilesPtr + base2) >> bit) & 1;
                                int p3 = (*(objectTilesPtr + base2 + 1) >> bit) & 1;

                                byte index = (byte)(p0 | (p1 << 1) | (p2 << 2) | (p3 << 3));

                                // compute pixel position once and write 32-bit BGRA in a single store
                                int px = x * 8 + col;
                                int py = y * 8 + row;
                                int baseIdx = px * 4 + py * stride;
                                Color colStruct = Palette[set, index];
                                uint bgra = (0xFFu << 24) | ((uint)colStruct.R << 16) | ((uint)colStruct.G << 8) | (uint)colStruct.B;
                                *(uint*)(buffer + baseIdx) = bgra;
                            }
                        }
                    }
                }
            }
        }
        SKCanvas canvas = e.Canvas;
        SKRect destRect = new SKRect(0, 0, (float)(scale * 128), (float)(scale * 128));
        canvas.DrawBitmap(vramTiles, destRect);

        if (ShowTileGrid)
            MainWindow.DrawGrid(canvas, destRect.Width, destRect.Height, 16, 16);
    }
    private void ReDrawVramBtn_Click(object sender, RoutedEventArgs e)
    {
        if (SNES.rom == null || !MainWindow.window.tileE.objTileSetInt.IsEnabled)
            return;
        if (MainWindow.window.tileE.objectFreshCheck.IsChecked == true)
            LoadDefaultObjectTiles();

        DrawObjectTiles();

        if (MainWindow.window.tileE.objectFreshCheck.IsChecked == true)
            DrawPalette();
    }
    private void vramSelectToggleBtn_Click(object sender, RoutedEventArgs e)
    {

    }
    private void gridBtn_Click(object sender, RoutedEventArgs e)
    {
        ShowTileGrid = !ShowTileGrid;
        objectTilesImage.InvalidateVisual();
    }
    private void zoomInBtn_Click(object sender, RoutedEventArgs e)
    {
        scale = Math.Clamp(scale + 1, 1, Const.MaxScaleUI);
        objectTilesImage.Width = scale * 128;
        objectTilesImage.Height = scale * 128;
    }
    private void zoomOutBtn_Click(object sender, RoutedEventArgs e)
    {
        scale = Math.Clamp(scale - 1, 1, Const.MaxScaleUI);
        objectTilesImage.Width = scale * 128;
        objectTilesImage.Height = scale * 128;
    }
    private async void EditObjectSlotCountBtn_Click(object sender, RoutedEventArgs e)
    {
        if (SNES.rom == null || !MainWindow.window.tileE.objTileSetInt.IsEnabled || Level.Id >= Const.PlayableLevelsCount)
            return;

        List<ObjectSetting> trueCopy = ObjectSettings[Level.Id].Select(os => new ObjectSetting(os)).ToList();

        Window window = new Window() { WindowStartupLocation = WindowStartupLocation.CenterScreen, Title = "Object Tiles Settings" };
        window.Width = 310;
        window.MinWidth = 310;
        window.MaxWidth = 310;
        window.Height = 760;

        StackPanel stackPanel = new StackPanel();

        for (int i = 0; i < trueCopy.Count; i++)
        {
            DataEntry entry = new DataEntry(window, trueCopy, i);
            stackPanel.Children.Add(entry);
        }

        ScrollViewer scrollViewer = new ScrollViewer() { AllowAutoHide = false };
        scrollViewer.Content = stackPanel;

        Button confirmBtn = new Button() { Content = "Confirm" };
        confirmBtn.Click += async (s, ev) =>
        {
            for (int i = 0; i < trueCopy.Count; i++)
            {
                int neededSlots = ((DataEntry)(stackPanel.Children[i])).slotCount;

                while (trueCopy[i].Slots.Count < neededSlots)
                {
                    ObjectSlot slot = new ObjectSlot(); //Default is a Heart Tank
                    slot.TileId = 0x36;
                    slot.VramAddress = 0x700;
                    slot.PaletteId = 0x98;
                    slot.PaletteDestination = 0x40;
                    trueCopy[i].Slots.Add(slot);
                }
                while (trueCopy[i].Slots.Count > neededSlots)
                    trueCopy[i].Slots.RemoveAt(trueCopy[i].Slots.Count - 1);
            }

            List<ObjectSetting> uneditedList = ObjectSettings[Level.Id];
            ObjectSettings[Level.Id] = trueCopy;

            int objectStages = Const.Id == Const.GameId.MegaManX ? 0x24 : Const.Id == Const.GameId.MegaManX2 ? 0xF : 0x12;

            int[] maxAmount = new int[objectStages];
            int[] shared = new int[objectStages];

            if (Level.Project.ObjectSettings != null) //no stages share data when using json (NOTE: X1 should probably have some extra logic because of the non playable stages)
            {
                for (int i = 0; i < objectStages; i++)
                    shared[i] = -1;
            }
            else
                GetMaxObjectSettingsFromRom(maxAmount, shared);

            int length = CreateObjectSettingsData(ObjectSettings, shared).Length;

            if (length > Const.ObjectTileInfoLength && Level.Project.ObjectSettings == null)
            {
                ObjectSettings[Level.Id] = uneditedList;
                await MessageBox.Show(MainWindow.window,$"The new Object Tile Info length exceeds the maximum allowed space in the ROM (0x{length:X} vs max of 0x{Const.ObjectTileInfoLength:X}). Please lower some counts for this or another stage.");
                return;
            }

            AssignLimits();
            SNES.edit = true;
            await MessageBox.Show(MainWindow.window,"Object Slot counts updated!");
            window.Close();
        };
        Grid.SetRow(confirmBtn, 2);

        Button addBtn = new Button() { Content = "Add Setting" };
        addBtn.Click += (s, e) =>
        {
            int newIndex = trueCopy.Count;
            ObjectSlot slot = new ObjectSlot(); //Default is a Heart Tank
            slot.TileId = 0x36;
            slot.VramAddress = 0x700;
            slot.PaletteId = 0x98;
            slot.PaletteDestination = 0x40;

            ObjectSetting objectSetting = new ObjectSetting();
            objectSetting.Slots.Add(slot);
            trueCopy.Add(objectSetting);

            DataEntry entry = new DataEntry(window, trueCopy, newIndex);
            stackPanel.Children.Add(entry);
        };
        Grid.SetRow(addBtn, 1);

        Grid grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition());
        grid.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });
        grid.Children.Add(scrollViewer);
        grid.Children.Add(confirmBtn);
        grid.Children.Add(addBtn);
        grid.Background = Brushes.Black;
        window.Content = grid;
        await window.ShowDialog(MainWindow.window);
    }
    private async void ObjectTileInfoBtn_Click(object sender, RoutedEventArgs e)
    {
        if (SNES.rom == null || suppressInts || !objTileSetInt.IsEnabled)
            return;

        int id = (Const.Id == Const.GameId.MegaManX3 && Level.Id > 0xE) ? (Level.Id - 0xF) + 2 : Level.Id; // Buffalo or Beetle

        if (ObjectSettings[id][objectTileSetId].Slots.Count != 0)
        {
            int i = objectTileSlotId;
            ushort relativeVramAddr = ObjectSettings[id][objectTileSetId].Slots[i].VramAddress;
            byte compressedTileId = ObjectSettings[id][objectTileSetId].Slots[i].TileId;

            int specOffset = BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(Const.CompressedTilesSwapInfoOffset + compressedTileId * 2)) + Const.CompressedTilesSwapInfoOffset;

            int totalLength = 0;

            if (relativeVramAddr != 0x8000 || Const.Id == Const.GameId.MegaManX)
            {
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
                    totalLength += length * 16;


                    byte vramBaseHigh = SNES.rom[specOffset + 1];


                    if ((vramBaseHigh & 0x80) != 0)
                        break;
                    specOffset += 2;
                }
            }
            await MessageBox.Show(MainWindow.window,$"The total amount of bytes this object slot transfers to VRAM is 0x{totalLength:X} bytes.");
        }
    }
    #endregion Events
}