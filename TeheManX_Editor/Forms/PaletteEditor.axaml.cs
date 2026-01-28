using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using SkiaSharp;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace TeheManX_Editor.Forms;

public partial class PaletteEditor : UserControl
{
    #region Fields
    public static double scale = 1;
    public static List<List<BGPalette>> BGPalettes;
    private static SKBitmap vramTiles = new SKBitmap(128, 512, SKColorType.Bgra8888, SKAlphaType.Premul);
    public static int bgPalIdId;
    public static int paletteSetId;
    public static int colorIndexId;
    private static bool supressInts;
    #endregion Fields

    #region Properties
    Rectangle selectSetRect = new Rectangle() { IsHitTestVisible = false, StrokeThickness = 2.5, StrokeDashArray = new Avalonia.Collections.AvaloniaList<double>() { 2.2 }, Stroke = Brushes.PapayaWhip };
    public int selectedSet;
    #endregion Properties

    #region Constructors
    public PaletteEditor()
    {
        supressInts = true;
        InitializeComponent();
        supressInts = false;

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
                uint RGBA = Level.Palette[y * 16 + x];
                rect.Fill = new SolidColorBrush(Color.FromRgb((byte)(RGBA >> 16), (byte)(RGBA >> 8), (byte)RGBA));
                rect.PointerPressed += Color_Down;
                rect.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch;
                rect.VerticalAlignment = Avalonia.Layout.VerticalAlignment.Stretch;
                Grid.SetColumn(rect, x);
                Grid.SetRow(rect, y);
                paletteGrid.Children.Add(rect);
            }
        }
        Grid.SetColumnSpan(selectSetRect, 16);
        paletteGrid.Children.Add(selectSetRect);

        for (int col = 0; col < 16; col++)
        {
            paletteGrid2.ColumnDefinitions.Add(new ColumnDefinition());

            //Create Color
            Rectangle rect = new Rectangle();
            rect.Focusable = false;
            rect.Width = 16;
            rect.Height = 16;
            //rect.Fill = new SolidColorBrush(Level.Palette[y, x]);
            //rect.MouseDown += Color_Down;
            rect.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch;
            rect.VerticalAlignment = Avalonia.Layout.VerticalAlignment.Stretch;
            Grid.SetColumn(rect, col);
            paletteGrid2.Children.Add(rect);
        }
    }
    #endregion Constructors

    #region Methods
    public void CollectData()
    {
        if (Level.Project.BGPalettes == null)
        {
            int bgStages = Const.Id == Const.GameId.MegaManX3 ? 0xF : Const.PlayableLevelsCount;
            int[] maxAmount = new int[bgStages];
            int[] shared = new int[bgStages];
            GetMaxPalettesFromRom(maxAmount, shared);
            BGPalettes = CollecBGPalettesFromRom(maxAmount, shared);
            Const.SwapPaletteColorBank = Const.PaletteColorBank;
        }
        else
        {
            BGPalettes = Level.Project.BGPalettes;
            Const.SwapPaletteColorBank = Level.Project.PaletteColorBank;
        }
    }
    public void AssignLimits()
    {
        if (Level.PaletteColorAddress != -1) //Also surves as a non playable level check
        {
            if (Const.Id != Const.GameId.MegaManX)
                colorAddressTxt.Text = $"Color Address: {Level.PaletteColorAddress:X}";
            else
                colorAddressTxt.Text = $"Color Address: {Level.PaletteColorAddress | 0x800000:X}";
        }
        else
            colorAddressTxt.Text = "Color Address: ...";
        UpdatePaletteText();
        DrawVramTiles();
        DrawPalette();

        /*
         * Now to take care of the Swapable BG Palette
         */

        if (Level.Id >= Const.PlayableLevelsCount || (Const.Id == Const.GameId.MegaManX3 && Level.Id > 0xE) || BGPalettes[Level.Id].Count == 0)
        {
            bgPalIdInt.IsEnabled = false;
            paletteSlotInt.IsEnabled = false;
            colorIndexInt.IsEnabled = false;
            dumpPalBtn.IsEnabled = false;
            return;
        }


        supressInts = true;
        int max = BGPalettes[Level.Id].Count - 1;
        bgPalIdInt.Maximum = max;
        if (bgPalIdInt.Value > max)
            bgPalIdInt.Value = max;
        bgPalIdId = bgPalIdInt.Value;
        paletteSetId = 0;
        paletteSlotInt.Value = 0;
        SetupSwappablePaletteUI();
        supressInts = false;

        bgPalIdInt.IsEnabled = true;
    }
    private void DrawSwappablePalette(int offset)
    {
        for (int i = 0; i < 16; i++)
        {
            ushort color = BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(offset + i * 2));
            byte R = (byte)(color % 32 * 8);
            byte G = (byte)(color / 32 % 32 * 8);
            byte B = (byte)(color / 1024 % 32 * 8);

            Rectangle rect = paletteGrid2.Children[i] as Rectangle;

            rect.Fill = new SolidColorBrush(Color.FromRgb(R, G, B));
        }
    }
    private void SetupSwappablePaletteUI()
    {
        int id = Level.Id;

        if (BGPalettes[id][bgPalIdId].Slots.Count == 0)
        {
            paletteSlotInt.IsEnabled = false;
            colorIndexInt.IsEnabled = false;
            colorAddressInt.IsEnabled = false;
            dumpPalBtn.IsEnabled = false;
            return;
        }
        paletteSlotInt.IsEnabled = true;
        colorIndexInt.IsEnabled = true;
        colorAddressInt.IsEnabled = true;
        dumpPalBtn.IsEnabled = true;

        byte colorIndex = BGPalettes[id][bgPalIdId].Slots[paletteSetId].ColorIndex;
        ushort pointer = BGPalettes[id][bgPalIdId].Slots[paletteSetId].Address;

        colorIndexInt.Value = colorIndex;
        colorAddressInt.Value = pointer;
        paletteSlotInt.Maximum = BGPalettes[id][bgPalIdId].Slots.Count - 1;
        DrawSwappablePalette(SNES.CpuToOffset(pointer, Const.SwapPaletteColorBank));
    }
    public unsafe void DrawVramTiles()
    {
        vramTileImage.InvalidateVisual();
    }
    public void DrawPalette()
    {
        foreach (var p in paletteGrid.Children)
        {
            var col = Grid.GetColumn(p as Control);
            var row = Grid.GetRow(p as Control);

            Rectangle rect = p as Rectangle;
            uint RGBA = Level.Palette[row * 16 + col];
            rect.Fill = new SolidColorBrush(Color.FromRgb((byte)(RGBA >> 16), (byte)(RGBA >> 8), (byte)RGBA));
        }
        selectSetRect.Fill = Brushes.Transparent;
    }
    public void UpdatePaletteText()
    {
        if (Level.PaletteId != -1)
            palTxt.Text = $"Palette Set: {selectedSet} Id: {Level.PaletteId:X}";
        else
            palTxt.Text = $"Palette Set: {selectedSet}";
    }
    public void UpdateCursor()
    {
        Grid.SetRow(selectSetRect, selectedSet);
    }
    public static void GetMaxPalettesFromRom(int[] destAmount, int[] shared = null)
    {
        int bgStages = Const.Id == Const.GameId.MegaManX3 ? 0xF : Const.PlayableLevelsCount;

        if (shared == null)
            shared = new int[bgStages];

        for (int i = 0; i < bgStages; i++)
            shared[i] = -1;

        ushort[] offsets = new ushort[bgStages];
        ushort[] sortedOffsets = new ushort[bgStages];
        Buffer.BlockCopy(SNES.rom, Const.BackgroundPaletteOffset, offsets, 0, bgStages * 2);
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
                int tempOffset = Const.BackgroundPaletteOffset + bgStages * 2;
                int endOffset = offsets[maxIndex] + Const.BackgroundPaletteOffset;

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
    public static byte[] CreateBGPalettesData(List<List<BGPalette>> sourceSettings, int[] sharedList)
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
                byte[] slotsData = new byte[sourceSettings[id][s].Slots.Count * 3 + 2];
                if (slotsData.Length == 2)
                    BinaryPrimitives.WriteUInt16LittleEndian(slotsData.AsSpan(0), 0xFFFF);
                else
                {
                    BinaryPrimitives.WriteUInt16LittleEndian(slotsData.AsSpan(slotsData.Length - 2), 0xFFFF);
                    for (int slot = 0; slot < sourceSettings[id][s].Slots.Count; slot++)
                    {
                        BinaryPrimitives.WriteUInt16LittleEndian(slotsData.AsSpan(slot * 3 + 0), sourceSettings[id][s].Slots[slot].Address);
                        slotsData[slot * 3 + 2] = sourceSettings[id][s].Slots[slot].ColorIndex;
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
    public static List<List<BGPalette>> CollecBGPalettesFromRom(int[] destAmount, int[] shared)
    {
        List<List<BGPalette>> sourceSettings = new List<List<BGPalette>>();
        int bgStages = Const.Id == Const.GameId.MegaManX3 ? 0xF : Const.PlayableLevelsCount;

        for (int i = 0; i < bgStages; i++)
        {
            List<BGPalette> bgSettings = new List<BGPalette>();

            for (int j = 0; j < destAmount[i]; j++)
            {
                int listOffset = BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(Const.BackgroundPaletteOffset + i * 2)) + Const.BackgroundPaletteOffset;
                int settingOffset = BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(listOffset + j * 2));
                if (settingOffset == 0xFFFF) //Dump X1 Offset non sense (Sigam 4)
                    continue;

                int offset = settingOffset + Const.BackgroundPaletteOffset;

                BGPalette setting = new BGPalette();

                while (true)
                {
                    ushort addr = BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(offset));

                    if (addr == 0xFFFF)
                        break;

                    BGPaletteSlot slot = new BGPaletteSlot();

                    slot.Address = addr;
                    slot.ColorIndex = SNES.rom[offset + 2];
                    setting.Slots.Add(slot);
                    offset += 3;
                }
                bgSettings.Add(setting);
            }
            sourceSettings.Add(bgSettings);
        }
        return sourceSettings;
    }
    #endregion Methods

    #region Events
    private async void Color_Down(object sender, PointerPressedEventArgs e)
    {
        if (e.Properties.IsRightButtonPressed) //Change Color
        {
            if (Level.Id < Const.PlayableLevelsCount)
            {
                //Get Current Color
                int c = Grid.GetColumn(sender as Control);
                int r = Grid.GetRow(sender as Control);

                int id;
                if (Const.Id == Const.GameId.MegaManX3 && Level.Id == 0xE) id = 0xB; //special case for MMX3 rekt version of dophler 2
                else if (Const.Id == Const.GameId.MegaManX3 && Level.Id > 0xE) id = (Level.Id - 0xF) + 2;
                else id = Level.Id;

                int infoOffset = SNES.CpuToOffset(BitConverter.ToUInt16(SNES.rom, Const.PaletteInfoOffset + id * 2 + Const.PaletteStageBase), Const.PaletteBank);

                if (SNES.rom[infoOffset] == 0)
                {
                    return;
                }

                int colorOffset = SNES.CpuToOffset(BitConverter.ToUInt16(SNES.rom, infoOffset + 1) + (Const.PaletteColorBank << 16)); //where the colors are located

                ushort oldC = BitConverter.ToUInt16(SNES.rom, colorOffset);

                ColorDialog colorDialog = new ColorDialog(oldC, c, r);
                await colorDialog.ShowDialog(MainWindow.window);

                if (colorDialog.confirm)
                {
                    ushort newC = (ushort)(colorDialog.view.Color.B / 8 * 1024 + colorDialog.view.Color.G / 8 * 32 + colorDialog.view.Color.R / 8);
                    BinaryPrimitives.WriteUInt16LittleEndian(SNES.rom.AsSpan(colorOffset), newC);

                    SNES.edit = true;

                    //Convert & Change Clut in GUI
                    byte R = (byte)(newC % 32 * 8);
                    byte G = (byte)(newC / 32 % 32 * 8);
                    byte B = (byte)(newC / 1024 % 32 * 8);
                    Color color = Color.FromRgb(R, G, B);
                    ((Rectangle)sender).Fill = new SolidColorBrush(color);
                    Level.Palette[r * 16 + c] = 0xFF000000 | (uint)((color.R << 16) | (color.G << 8) | color.B);
                    selectSetRect.Fill = Brushes.Transparent;

                    //Update VRAM Tiles
                    if (selectedSet == r)
                        DrawVramTiles();

                    //Layout Tab
                    MainWindow.window.layoutE.DrawLayout();
                    MainWindow.window.layoutE.DrawScreen();
                    //Screen Tab
                    MainWindow.window.screenE.DrawScreen();
                    MainWindow.window.screenE.DrawTiles();
                    MainWindow.window.screenE.DrawTile();
                    //32x32 Tab
                    MainWindow.window.tile32E.DrawTiles();
                    MainWindow.window.tile32E.Draw16xTiles();
                    MainWindow.window.tile32E.DrawTile();
                    //16x16 Tab
                    MainWindow.window.tile16E.Draw16xTiles();
                    MainWindow.window.tile16E.DrawVramTiles();
                    //Enemy Tab
                    MainWindow.window.enemyE.DrawLayout();
                }

            }
            else
            {
                await MessageBox.Show(MainWindow.window, "You can't edit palettes in this level!");
                return;
            }
        }
        else
        {
            selectedSet = Grid.GetRow(sender as Control);
            UpdatePaletteText();
            DrawVramTiles();
            UpdateCursor();
        }
    }
    private async void GearBtn_Click(object sender, RoutedEventArgs e)
    {
        if (Level.Id >= Const.PlayableLevelsCount)
        {
            await MessageBox.Show(MainWindow.window, "You can't edit palettes in this level!");
            return;
        }

        int id;
        if (Const.Id == Const.GameId.MegaManX3 && Level.Id == 0xE) id = 0xB; //special case for MMX3 rekt version of dophler 2
        else if (Const.Id == Const.GameId.MegaManX3 && Level.Id > 0xE) id = (Level.Id - 0xF) + 2;
        else id = Level.Id;

        int infoOffset = SNES.CpuToOffset(BitConverter.ToUInt16(SNES.rom, Const.PaletteInfoOffset + id * 2 + Const.PaletteStageBase), Const.PaletteBank);

        if (SNES.rom[infoOffset] == 0)
        {
            return;
        }

        int colorAmount = SNES.rom[infoOffset]; //how many colors are going to be dumped
        int colorDataOffset = SNES.CpuToOffset(BitConverter.ToUInt16(SNES.rom, infoOffset + 1), Const.PaletteColorBank); //where the colors are located

        Window window = new Window() { WindowStartupLocation = WindowStartupLocation.CenterScreen, SizeToContent = SizeToContent.WidthAndHeight, Title = "Palette Tools" };

        Button importBtn = new Button() { Content = "Import Palette Colors", Width = 210 };
        importBtn.Click += async (s, e) =>
        {
            IStorageProvider storageProvider = window.StorageProvider;
            IReadOnlyList<IStorageFile> result = await storageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Select an PAL or Gimp TXT File",
                FileTypeFilter = [new FilePickerFileType("YY-Pal") { Patterns = ["*.pal"] }, new FilePickerFileType("Gimp-TXT") { Patterns = ["*.txt"] }],
                AllowMultiple = false
            });

            IStorageFile? file = result.FirstOrDefault();
            if (file != null)
            {
                List<Color> colors = new List<Color>();

                if (System.IO.Path.GetExtension(file.Path.LocalPath).ToLower() == ".pal")
                {
                    byte[] data = await File.ReadAllBytesAsync(file.Path.LocalPath);
                    {
                        int i = 0;
                        while (true)
                        {
                            Color color = Color.FromRgb(data[i], data[i + 1], data[i + 2]);
                            colors.Add(color);
                            i += 3;
                            if (i >= data.Length)
                                break;
                        }
                    }
                }
                else
                {
                    string[] lines = File.ReadAllLines(file.Path.LocalPath);
                    foreach (var l in lines)
                    {
                        if (l.Trim() == "" || l.Trim() == "\n") continue;

                        uint val = Convert.ToUInt32(l.Replace("#", "").Trim(), 16);
                        Color color;
                        color = Color.FromRgb((byte)(val >> 16), (byte)((val >> 8) & 0xFF), (byte)(val & 0xFF));
                        colors.Add(color);
                    }
                }

                for (int i = 0; i < colors.Count; i++)
                {
                    if (i > (colorAmount - 1)) break;
                    ushort newC = (ushort)(colors[i].B / 8 * 1024 + colors[i].G / 8 * 32 + colors[i].R / 8);
                    BinaryPrimitives.WriteUInt16LittleEndian(SNES.rom.AsSpan(colorDataOffset + i * 2), newC);
                }
                SNES.edit = true;
                Level.AssignPallete();
                DrawVramTiles();
                DrawPalette();

                MainWindow.window.layoutE.DrawLayout();
                MainWindow.window.layoutE.DrawScreen();

                MainWindow.window.screenE.DrawScreen();
                MainWindow.window.screenE.DrawTiles();
                MainWindow.window.screenE.DrawTile();

                MainWindow.window.tile32E.DrawTiles();
                MainWindow.window.tile32E.DrawTile();
                MainWindow.window.tile32E.Draw16xTiles();

                MainWindow.window.tile16E.Draw16xTiles();
                MainWindow.window.tile16E.DrawVramTiles();

                MainWindow.window.enemyE.DrawLayout();
                await MessageBox.Show(MainWindow.window, "Colors Imported!");
                window.Close();
            }
        };

        Button exportBtn = new Button() { Content = "Export Palette Colors", Width = 210 };
        exportBtn.Click += async (s, e) =>
        {
            IStorageProvider storageProvider = window.StorageProvider;
            IStorageFile file = await storageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Select an PAL or Gimp TXT File",
                FileTypeChoices = [new FilePickerFileType("YY-Pal") { Patterns = ["*.pal"] }, new FilePickerFileType("Gimp-TXT") { Patterns = ["*.txt"] }],
                ShowOverwritePrompt = true
            });

            if (file != null)
            {
                if (file.Path.LocalPath.EndsWith(".pal", StringComparison.OrdinalIgnoreCase))
                {
                    MemoryStream ms = new MemoryStream();
                    BinaryWriter bw = new BinaryWriter(ms);
                    for (int i = 0; i < colorAmount; i++)
                    {
                        Rectangle rect = paletteGrid.Children[i] as Rectangle;
                        SolidColorBrush brush = (SolidColorBrush)rect.Fill;
                        Color color = brush.Color;
                        bw.Write(color.R);
                        bw.Write(color.G);
                        bw.Write(color.B);
                    }
                    bw.Close();
                    await File.WriteAllBytesAsync(file.Path.LocalPath, ms.ToArray());
                    ms.Close();
                }
                else
                {
                    List<string> lines = new List<string>();

                    for (int i = 0; i < colorAmount; i++)
                    {
                        Rectangle rect = paletteGrid.Children[i] as Rectangle;
                        SolidColorBrush brush = (SolidColorBrush)rect.Fill;
                        Color color = brush.Color;
                        lines.Add($"#{color.R:X2}{color.G:X2}{color.B:X2}");
                    }
                    await File.WriteAllLinesAsync(file.Path.LocalPath, lines.ToArray());
                }
            }
        };


        StackPanel stackPanel = new StackPanel();
        stackPanel.Children.Add(importBtn);
        stackPanel.Children.Add(exportBtn);

        window.Content = stackPanel;
        await window.ShowDialog(MainWindow.window);
    }
    private void zoomInBtn_Click(object sender, RoutedEventArgs e)
    {
        scale = Math.Clamp(scale + 1, 1, Const.MaxScaleUI);
        vramTileImage.Width = scale * 128;
    }
    private void zoomOutBtn_Click(object sender, RoutedEventArgs e)
    {
        scale = Math.Clamp(scale - 1, 1, Const.MaxScaleUI);
        vramTileImage.Width = scale * 128;
    }
    private void bgPalIdInt_ValueChanged(object sender, int e)
    {
        if (SNES.rom == null || supressInts) return;

        supressInts = true;
        bgPalIdId = e;
        paletteSlotInt.Value = 0;
        paletteSetId = 0;
        SetupSwappablePaletteUI();
        supressInts = false;
    }
    private void paletteSlotInt_ValueChanged(object sender, int e)
    {
        if (SNES.rom == null || supressInts) return;
        paletteSetId = e;
        supressInts = true;
        SetupSwappablePaletteUI();
        supressInts = false;
    }
    private void colorIndexInt_ValueChanged(object sender, int e)
    {
        if (SNES.rom == null || supressInts) return;

        int id = Level.Id;

        byte valueNew = (byte)e;

        if (BGPalettes[id][bgPalIdId].Slots[paletteSetId].ColorIndex == valueNew) return;

        BGPalettes[Level.Id][bgPalIdId].Slots[paletteSetId].ColorIndex = valueNew;
        SNES.edit = true;
    }
    private void colorAddressInt_ValueChanged(object sender, int e)
    {
        if (SNES.rom == null || supressInts) return;

        int id = Level.Id;

        ushort valueNew = (byte)e;

        if (BGPalettes[id][bgPalIdId].Slots[paletteSetId].ColorIndex == valueNew) return;

        BGPalettes[Level.Id][bgPalIdId].Slots[paletteSetId].Address = valueNew;
        SNES.edit = true;
    }
    private void DumpPaletteBtn_Click(object sender, RoutedEventArgs e)
    {
        if (!paletteSlotInt.IsEnabled) return;

        int id = Level.Id;

        for (int i = 0; i < BGPalettes[id][bgPalIdId].Slots.Count; i++)
        {
            int colorIndex = BGPalettes[id][bgPalIdId].Slots[i].ColorIndex;
            int colorOffset = SNES.CpuToOffset(BGPalettes[id][bgPalIdId].Slots[i].Address, Const.SwapPaletteColorBank);

            for (int c = 0; c < 16; c++)
            {
                ushort color = BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(colorOffset + c * 2));
                byte R = (byte)(color % 32 * 8);
                byte G = (byte)(color / 32 % 32 * 8);
                byte B = (byte)(color / 1024 % 32 * 8);

                Level.Palette[colorIndex + c] = (uint)(0xFF000000 | (R << 16) | (G << 8) | B);
            }
        }

        DrawVramTiles();
        DrawPalette();

        MainWindow.window.layoutE.DrawLayout();
        MainWindow.window.layoutE.DrawScreen();

        MainWindow.window.screenE.DrawScreen();
        MainWindow.window.screenE.DrawTiles();
        MainWindow.window.screenE.DrawTile();

        MainWindow.window.tile32E.DrawTiles();
        MainWindow.window.tile32E.DrawTile();
        MainWindow.window.tile32E.Draw16xTiles();

        MainWindow.window.tile16E.Draw16xTiles();
        MainWindow.window.tile16E.DrawVramTiles();

        MainWindow.window.enemyE.DrawLayout();
    }
    private async void EditPaletteCountBtn_Click(object sender, RoutedEventArgs e)
    {
        if (Level.Id >= Const.PlayableLevelsCount || (Const.Id == Const.GameId.MegaManX3 && Level.Id > 0xE))
            return;

        List<BGPalette> trueCopy = BGPalettes[Level.Id].Select(os => new BGPalette(os)).ToList();

        Window window = new Window() { WindowStartupLocation = WindowStartupLocation.CenterScreen, Title = "Palette Swap Settings" };
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

        ScrollViewer scrollViewer = new ScrollViewer() { AllowAutoHide = false};
        scrollViewer.Content = stackPanel;

        Button confirmBtn = new Button() { Content = "Confirm" };
        confirmBtn.Click += async (s, ev) =>
        {
            for (int i = 0; i < trueCopy.Count; i++)
            {
                int neededSlots = ((DataEntry)(stackPanel.Children[i])).slotCount;

                while (trueCopy[i].Slots.Count < neededSlots)
                {
                    BGPaletteSlot slot = new BGPaletteSlot();
                    slot.Address = 0x8000;
                    slot.ColorIndex = 0;
                    trueCopy[i].Slots.Add(slot);
                }
                while (trueCopy[i].Slots.Count > neededSlots)
                    trueCopy[i].Slots.RemoveAt(trueCopy[i].Slots.Count - 1);
            }

            List<BGPalette> uneditedList = BGPalettes[Level.Id];
            BGPalettes[Level.Id] = trueCopy;

            int bgStages = Const.Id == Const.GameId.MegaManX3 ? 0xF : Const.PlayableLevelsCount;

            int[] maxAmount = new int[bgStages];
            int[] shared = new int[bgStages];

            if (Level.Project.BGPalettes != null) //no stages share data when using json
            {
                for (int i = 0; i < bgStages; i++)
                    shared[i] = -1;
            }
            else
                GetMaxPalettesFromRom(maxAmount, shared);

            int length = CreateBGPalettesData(BGPalettes, shared).Length;

            if (length > Const.BackgroundPaletteInfoLength && Level.Project.BGSettings == null)
            {
                BGPalettes[Level.Id] = uneditedList;
                await MessageBox.Show(MainWindow.window, $"The new BG Tile Info length exceeds the maximum allowed space in the ROM (0x{length:X} vs max of 0x{Const.BackgroundPaletteInfoLength:X}). Please lower some counts for this or another stage.");
                return;
            }

            AssignLimits();
            SNES.edit = true;
            await MessageBox.Show(MainWindow.window, "Palette Swap counts updated!");
            window.Close();
        };
        Grid.SetRow(confirmBtn, 2);

        Button addBtn = new Button() { Content = "Add Setting" };
        addBtn.Click += (s, e) =>
        {
            int newIndex = trueCopy.Count;
            BGPaletteSlot slot = new BGPaletteSlot();
            slot.Address = 0x8000;
            slot.ColorIndex = 0;

            BGPalette bgSetting = new BGPalette();
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
    private void vramTileImage_MeasureEvent(object? sender, Size e)
    {
        vramTileImage.MeasuredSize = new Size(scale * 128, scale * 512);
    }
    private void vramTileImage_RenderEvent(object? sender, SkiaCanvasEventArgs e)
    {
        unsafe
        {
            byte* buffer = (byte*)vramTiles.GetPixels();
            int stride = vramTiles.RowBytes;

            int set = selectedSet;

            // Pin source data to avoid bounds checks
            fixed (byte* tilesPtr = Level.DecodedTiles)
            fixed (uint* palettePtr = Level.Palette)
            {
                // Base pointer for the active palette (set * 16 colors)
                uint* palBase = palettePtr + (set << 4);

                /*
                 * Draw 0x200 (512) tiles from VRAM
                 * Layout: 16 tiles per row × 64 rows
                 * Each tile is 8×8 pixels
                 */
                for (int ty = 0; ty < 64; ty++)
                {
                    // Precompute Y position for this tile row
                    int tileY = ty << 3; // ty * 8

                    for (int tx = 0; tx < 16; tx++)
                    {
                        int id = tx + (ty << 4); // ty * 16
                        int tileOffset = id << 6; // id * 64 bytes per decoded tile

                        // Precompute X position for this tile
                        int tileX = tx << 3; // tx * 8

                        // Pointer to the top-left pixel of this tile in the destination
                        byte* dstTileBase = buffer + (tileY * stride) + (tileX << 2);

                        // Pointer to the source tile data
                        byte* srcTile = tilesPtr + tileOffset;

                        // Draw 8 rows
                        for (int row = 0; row < 8; row++)
                        {
                            // Destination row pointer
                            byte* dst = dstTileBase + row * stride;

                            // Source row pointer (8 bytes per row)
                            byte* src = srcTile + (row << 3);

                            // Draw 8 pixels
                            for (int col = 0; col < 8; col++)
                            {
                                byte index = src[col];

                                *(uint*)dst = palBase[index];
                                dst += 4; // advance one pixel (32-bit)
                            }
                        }
                    }
                }
            }
        }
        SKCanvas canvas = e.Canvas;
        canvas.DrawBitmap(vramTiles, new SKRect(0, 0, (float)(scale * 128), (float)(scale * 512)));
    }
    #endregion Events
}