using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace TeheManX_Editor.Forms
{
    /// <summary>
    /// Interaction logic for PaletteEditor.xaml
    /// </summary>
    public partial class PaletteEditor : UserControl
    {
        #region Fields
        public static double scale = 1;
        public static List<List<BGPalette>> BGPalettes;

        public static int paletteSetId;
        public static int bgPalIdId;
        public static int colorIndexId;
        #endregion Fields

        #region Properties
        public bool updateVramTiles;
        WriteableBitmap vramTiles = new WriteableBitmap(128, 512, 96, 96, PixelFormats.Bgra32, null);
        Rectangle selectSetRect = new Rectangle() { IsHitTestVisible = false, StrokeThickness = 2.5, StrokeDashArray = new DoubleCollection() { 2.2 }, CacheMode = null, Stroke = Brushes.PapayaWhip };
        public int selectedSet = 0;
        bool supressInts;
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
                    rect.Fill = new SolidColorBrush(Level.Palette[y, x]);
                    rect.MouseDown += Color_Down;
                    rect.HorizontalAlignment = HorizontalAlignment.Stretch;
                    rect.VerticalAlignment = VerticalAlignment.Stretch;
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
                rect.HorizontalAlignment = HorizontalAlignment.Stretch;
                rect.VerticalAlignment = VerticalAlignment.Stretch;
                Grid.SetColumn(rect, col);
                paletteGrid2.Children.Add(rect);
            }


            vramTileImage.Source = vramTiles;
        }
        #endregion Constructors

        #region Methods
        public void CollectData()
        {
            int bgStages = Const.Id == Const.GameId.MegaManX3 ? 0xF : Const.PlayableLevelsCount;
            int[] maxAmount = new int[bgStages];
            int[] shared = new int[bgStages];
            GetMaxPalettesFromRom(maxAmount, shared);
            BGPalettes = CollecBGPalettesFromRom(maxAmount, shared);
        }
        public void AssignLimits()
        {
            if (Level.PaletteColorAddress != -1) //Also surves as a non playable level check
            {
                if (Const.Id != Const.GameId.MegaManX)
                    MainWindow.window.paletteE.colorAddressTxt.Text = $"Color Address: {Level.PaletteColorAddress:X}";
                else
                    MainWindow.window.paletteE.colorAddressTxt.Text = $"Color Address: {Level.PaletteColorAddress | 0x800000:X}";
            }
            else
                MainWindow.window.paletteE.colorAddressTxt.Text = "Color Address: ...";
            UpdatePaletteText();
            DrawVramTiles();
            DrawPalette();

            /*
             * Now to take care of the Swapable BG Palette
             */

            if (Level.Id >= Const.PlayableLevelsCount || (Const.Id == Const.GameId.MegaManX3 && Level.Id > 0xE))
            {
                MainWindow.window.paletteE.bgPalIdInt.IsEnabled = false;
                MainWindow.window.paletteE.paletteSetInt.IsEnabled = false;
                MainWindow.window.paletteE.colorIndexInt.IsEnabled = false;
                MainWindow.window.paletteE.dumpPalBtn.IsEnabled = false;
                return;
            }


            supressInts = true;
            bgPalIdId = 0;
            bgPalIdInt.Value = 0;
            bgPalIdInt.Maximum = BGPalettes[Level.Id].Count - 1;
            paletteSetId = 0;
            paletteSetInt.Value = 0;
            SetupSwappablePaletteUI();
            supressInts = false;

            MainWindow.window.paletteE.bgPalIdInt.IsEnabled = true;
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
                MainWindow.window.paletteE.paletteSetInt.IsEnabled = false;
                MainWindow.window.paletteE.colorIndexInt.IsEnabled = false;
                MainWindow.window.paletteE.colorAddressInt.IsEnabled = false;
                MainWindow.window.paletteE.dumpPalBtn.IsEnabled = false;
                return;
            }
            MainWindow.window.paletteE.paletteSetInt.IsEnabled = true;
            MainWindow.window.paletteE.colorIndexInt.IsEnabled = true;
            MainWindow.window.paletteE.colorAddressInt.IsEnabled = true;
            MainWindow.window.paletteE.dumpPalBtn.IsEnabled = true;

            byte colorIndex = BGPalettes[id][bgPalIdId].Slots[paletteSetId].ColorIndex;
            ushort pointer = BGPalettes[id][bgPalIdId].Slots[paletteSetId].Address;

            colorIndexInt.Value = colorIndex;
            colorAddressInt.Value = pointer;
            paletteSetInt.Maximum = BGPalettes[id][bgPalIdId].Slots.Count - 1;
            DrawSwappablePalette(SNES.CpuToOffset(pointer, Const.PaletteColorBank));
        }
        public unsafe void PaintVramTiles()
        {
            updateVramTiles = false;
            vramTiles.Lock();
            byte* buffer = (byte*)vramTiles.BackBuffer;
            int set = selectedSet;

            /*
             *  Draw 0x200 tiles from VRAM
             */

            for (int y = 0; y < 64; y++)
            {
                for (int x = 0; x < 16; x++)
                {
                    int id = x + (y * 16);
                    int tileOffset = (id & 0x3FF) * 0x20; // 32 bytes per tile

                    for (int row = 0; row < 8; row++)
                    {
                        int base1 = tileOffset + (row * 2);
                        int base2 = tileOffset + 0x10 + (row * 2);

                        for (int col = 0; col < 8; col++)
                        {
                            int bit = 7 - col; // leftmost pixel = bit7
                            int p0 = (Level.Tiles[base1] >> bit) & 1;
                            int p1 = (Level.Tiles[base1 + 1] >> bit) & 1;
                            int p2 = (Level.Tiles[base2] >> bit) & 1;
                            int p3 = (Level.Tiles[base2 + 1] >> bit) & 1;

                            byte index = (byte)(p0 | (p1 << 1) | (p2 << 2) | (p3 << 3));

                            Color color = Level.Palette[set, index];

                            uint pixel = color.B | ((uint)color.G << 8) | ((uint)color.R << 16) | 0xFF000000;

                            *(uint*)(buffer + (x * 8 + col) * 4 + (y * 8 + row) * vramTiles.BackBufferStride) = pixel;
                        }
                    }
                }
            }
            vramTiles.AddDirtyRect(new Int32Rect(0, 0, 128, 512));
            vramTiles.Unlock();
        }
        public unsafe void DrawVramTiles()
        {
            updateVramTiles = true;
        }
        public void DrawPalette()
        {
            foreach (var p in paletteGrid.Children)
            {
                var col = Grid.GetColumn(p as UIElement);
                var row = Grid.GetRow(p as UIElement);

                Rectangle rect = p as Rectangle;
                rect.Fill = new SolidColorBrush(Level.Palette[row, col]);
            }
            selectSetRect.Fill = Brushes.Transparent;
        }
        public void UpdatePaletteText()
        {
            if (Level.PaletteId != -1)
                MainWindow.window.paletteE.palTxt.Text = $"Palette Set: {selectedSet} Id: {Level.PaletteId:X}";
            else
                MainWindow.window.paletteE.palTxt.Text = $"Palette Set: {selectedSet}";
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
        private void Color_Down(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Right) //Change Color
            {
                if (Level.Id < Const.PlayableLevelsCount)
                {
                    //Get Current Color
                    int c = Grid.GetColumn(sender as UIElement);
                    int r = Grid.GetRow(sender as UIElement);

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
                    colorDialog.Owner = Application.Current.MainWindow;
                    colorDialog.ShowDialog();

                    if (colorDialog.confirm)
                    {
                        ushort newC = (ushort)(colorDialog.canvas.SelectedColor.Value.B / 8 * 1024 + colorDialog.canvas.SelectedColor.Value.G / 8 * 32 + colorDialog.canvas.SelectedColor.Value.R / 8);
                        BinaryPrimitives.WriteUInt16LittleEndian(SNES.rom.AsSpan(colorOffset), newC);

                        SNES.edit = true;

                        //Convert & Change Clut in GUI
                        byte R = (byte)(newC % 32 * 8);
                        byte G = (byte)(newC / 32 % 32 * 8);
                        byte B = (byte)(newC / 1024 % 32 * 8);
                        Color color = Color.FromRgb(R, G, B);
                        ((Rectangle)sender).Fill = new SolidColorBrush(color);
                        Level.Palette[r, c] = color;
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
                    MessageBox.Show("You can't edit palettes in this level!");
                    return;
                }
            }
            else
            {
                selectedSet = Grid.GetRow(sender as UIElement);
                UpdatePaletteText();
                DrawVramTiles();
                UpdateCursor();
            }
        }
        private void GearBtn_Click(object sender, RoutedEventArgs e)
        {
            if (Level.Id >= Const.PlayableLevelsCount)
            {
                MessageBox.Show("You can't edit palettes in this level!");
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

            Window window = new Window() { ResizeMode = ResizeMode.NoResize , WindowStartupLocation = WindowStartupLocation.CenterScreen , SizeToContent = SizeToContent.WidthAndHeight , Title = "Palette Tools"};

            Button importBtn = new Button() { Content = "Import Palette Colors" , Width = 210};
            importBtn.Click += (s, e) =>
            {
                using (var fd = new System.Windows.Forms.OpenFileDialog())
                {
                    fd.Filter = "YYCHR PAL or TXT|*.pal;*.txt";
                    fd.Title = "Select an PAL or Gimp TXT File";

                    List<Color> colors = new List<Color>();

                    if (fd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    {
                        string file = fd.FileName;
                        if (System.IO.Path.GetExtension(file).ToLower() == ".pal")
                        {
                            byte[] data = File.ReadAllBytes(file);
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
                            string[] lines = File.ReadAllLines(file);
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
                        MessageBox.Show("Colors Imported!");
                        window.Close();
                    }
                }
            };

            Button exportBtn = new Button() { Content = "Export Palette Colors" , Width = 210 };
            exportBtn.Click += (s, e) =>
            {
                using (var fd = new System.Windows.Forms.SaveFileDialog())
                {
                    fd.Filter =
                        "YYCHR PAL (*.pal)|*.pal|" +
                        "Text File (*.txt)|*.txt";

                    fd.Title = "Save as an PAL or Gimp TXT File";

                    if (fd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    {
                        string file = fd.FileName;
                        if (fd.FilterIndex == 1)
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
                            File.WriteAllBytes(file, ms.ToArray());
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
                            File.WriteAllLines(file, lines.ToArray());
                        }
                        MessageBox.Show("Colors Exported!");
                        window.Close();
                    }
                }
            };


            StackPanel stackPanel = new StackPanel();
            stackPanel.Children.Add(importBtn);
            stackPanel.Children.Add(exportBtn);

            window.Content = stackPanel;
            window.ShowDialog();
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
        private void bgPalIdInt_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue == null || SNES.rom == null) return;

            supressInts = true;
            bgPalIdId = (int)e.NewValue;
            paletteSetInt.Value = 0;
            paletteSetId = 0;
            SetupSwappablePaletteUI();
            supressInts = false;
        }
        private void paletteSetInt_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue == null || SNES.rom == null || supressInts) return;
            paletteSetId = (int)e.NewValue;
            SetupSwappablePaletteUI();
        }
        private void colorIndexInt_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue == null || SNES.rom == null || supressInts) return;

            int id = Level.Id;

            byte valueNew = (byte)(int)e.NewValue;

            if (BGPalettes[id][bgPalIdId].Slots[paletteSetId].ColorIndex == valueNew) return;

            BGPalettes[Level.Id][bgPalIdId].Slots[paletteSetId].ColorIndex = valueNew;
            SNES.edit = true;
        }
        private void colorAddressInt_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue == null || SNES.rom == null || supressInts) return;

            int id = Level.Id;

            ushort valueNew = (byte)(int)e.NewValue;

            if (BGPalettes[id][bgPalIdId].Slots[paletteSetId].ColorIndex == valueNew) return;

            BGPalettes[Level.Id][bgPalIdId].Slots[paletteSetId].Address = valueNew;
            SNES.edit = true;
        }
        private void DumpPaletteBtn_Click(object sender, RoutedEventArgs e)
        {
            if (!paletteSetInt.IsEnabled) return;

            int id = Level.Id;

            for (int i = 0; i < BGPalettes[id][bgPalIdId].Slots.Count; i++)
            {
                int colorIndex = BGPalettes[id][bgPalIdId].Slots[i].ColorIndex;
                int colorOffset = SNES.CpuToOffset(BGPalettes[id][bgPalIdId].Slots[i].Address, Const.PaletteColorBank);

                for (int c = 0; c < 16; c++)
                {
                    ushort color = BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(colorOffset + c * 2));
                    byte R = (byte)(color % 32 * 8);
                    byte G = (byte)(color / 32 % 32 * 8);
                    byte B = (byte)(color / 1024 % 32 * 8);

                    Level.Palette[((colorIndex + c) >> 4) & 0xF, (colorIndex + c) & 0xF] = Color.FromRgb(R, G, B);
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
        private void EditPaletteCountBtn_Click(object sender, RoutedEventArgs e)
        {
            if (Level.Id >= Const.PlayableLevelsCount || (Const.Id == Const.GameId.MegaManX3 && Level.Id > 0xE))
                return;

            List<BGPalette> trueCopy = BGPalettes[Level.Id].Select(os => new BGPalette(os)).ToList();

            Window window = new Window() { WindowStartupLocation = WindowStartupLocation.CenterScreen, Title = "Palette Swap Settings", ResizeMode = ResizeMode.CanMinimize };
            window.Width = 310;
            window.MinWidth = 310;
            window.MaxWidth = 310;
            window.Height = 760;

            StackPanel stackPanel = new StackPanel();

            for (int i = 0; i < trueCopy.Count; i++)
            {
                DataEntry entry = new DataEntry(trueCopy, i);
                stackPanel.Children.Add(entry);
            }

            ScrollViewer scrollViewer = new ScrollViewer();
            scrollViewer.Content = stackPanel;

            Button confirmBtn = new Button() { Content = "Confirm" };
            confirmBtn.Click += (s, ev) =>
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
                GetMaxPalettesFromRom(maxAmount, shared);

                if (false) //no stages share data when using json
                {
                    for (int i = 0; i < bgStages; i++)
                        shared[i] = -1;
                }

                int length = CreateBGPalettesData(BGPalettes, shared).Length;

                if (length > Const.BackgroundPaletteInfoLength) //TODO: get max for each
                {
                    BGPalettes[Level.Id] = uneditedList;
                    MessageBox.Show($"The new BG Tile Info length exceeds the maximum allowed space in the ROM (0x{length:X} vs max of 0x{Const.BackgroundPaletteInfoLength:X}). Please lower some counts for this or another stage.");
                    return;
                }

                AssignLimits();
                SNES.edit = true;
                MessageBox.Show("Palette Swap counts updated!");
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

                DataEntry entry = new DataEntry(trueCopy, newIndex);
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
            window.ShowDialog();
        }
        #endregion Events
    }
}
