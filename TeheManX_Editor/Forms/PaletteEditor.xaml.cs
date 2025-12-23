using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
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
        #endregion Fields

        #region Properties
        public bool updateVramTiles;
        WriteableBitmap vramTiles = new WriteableBitmap(128, 512, 96, 96, PixelFormats.Bgra32, null);
        Rectangle selectSetRect = new Rectangle() { IsHitTestVisible = false, StrokeThickness = 2.5, StrokeDashArray = new DoubleCollection() { 2.2 }, CacheMode = null, Stroke = Brushes.PapayaWhip };
        public int selectedSet = 0;
        bool suspendSetInt;
        bool suspendColorInt;
        bool suspendAddrBox;
        #endregion Properties

        #region Constructors
        public PaletteEditor()
        {
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
        public void AssignLimits()
        {
            if (Level.PaletteColorAddress != -1)
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

            if (Level.Id >= Const.PlayableLevelsCount || (Const.Id == Const.GameId.MegaManX3 && Level.Id > 0xE))
            {
                MainWindow.window.paletteE.bgPalIdInt.IsEnabled = false;
                MainWindow.window.paletteE.paletteSetInt.IsEnabled = false;
                MainWindow.window.paletteE.colorIndexInt.IsEnabled = false;
                MainWindow.window.paletteE.dumpPalBtn.IsEnabled = false;
                return;
            }

            //Calculate the amount of Background Palette Swaps there are in the Stage
            int maxSwaps = 0;
            int maxLevels = (Const.Id == Const.GameId.MegaManX3) ? 0xF : Const.PlayableLevelsCount;

            ushort[] offsetList = new ushort[maxLevels];
            for (int i = 0; i < maxLevels; i++)
                offsetList[i] = BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(Const.BackgroundPaletteOffset + i * 2));

            int maxIndex = 0;
            for (int i = 0; i < offsetList.Length; i++)
            {
                if (offsetList[i] > offsetList[maxIndex])
                    maxIndex = i;
            }

            int tempOffset = Const.BackgroundPaletteOffset + maxLevels * 2;
            int endOffset = offsetList[maxIndex] + Const.BackgroundPaletteOffset;

            int lowestPointer = BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(tempOffset));

            while (tempOffset != endOffset)
            {
                int addr = BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(tempOffset));

                if (addr < lowestPointer)
                    lowestPointer = addr;
                tempOffset += 2;
            }
            suspendSetInt = true;
            suspendColorInt = true;
            suspendAddrBox = true;

            ushort currentOffset = BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(Const.BackgroundPaletteOffset + Level.Id * 2));
            Array.Sort(offsetList);
            int currentIndex = Array.IndexOf(offsetList, currentOffset);

            maxSwaps = 0; //TODO: Fix this calculation since it doesnt work for MegaMan X3
            /*if (currentIndex == maxIndex)
                maxSwaps = ((lowestPointer - currentOffset) / 2) - 1;
            else
                maxSwaps = ((offsetList[currentIndex + 1] - currentOffset) / 2) - 1;*/

            bgPalIdInt.Maximum = maxSwaps;
            if (MainWindow.window.paletteE.bgPalIdInt.Value > maxSwaps)
                MainWindow.window.paletteE.bgPalIdInt.Value = maxSwaps;

            paletteSetInt.Value = 0;
            SetupSwappablePaletteUI();
            suspendSetInt = false;
            suspendColorInt = false;
            suspendAddrBox = false;

            MainWindow.window.paletteE.bgPalIdInt.IsEnabled = true;
            MainWindow.window.paletteE.paletteSetInt.IsEnabled = true;
            MainWindow.window.paletteE.colorIndexInt.IsEnabled = true;
            MainWindow.window.paletteE.dumpPalBtn.IsEnabled = true;
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
            int listOffset = BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(Const.BackgroundPaletteOffset + Level.Id * 2)) + Const.BackgroundPaletteOffset;
            int offset = BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(listOffset + (int)MainWindow.window.paletteE.bgPalIdInt.Value * 2)) + Const.BackgroundPaletteOffset;

            ushort pointer = BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(offset));

            if (pointer == 0xFFFF)
            {
                return;
            }

            int colorIndex = SNES.rom[offset + 2];
            int set = 0;
            int destSet = (int)paletteSetInt.Value;
            ushort lastPointer = 0;
            do
            {
                lastPointer = BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(offset));
                if (lastPointer == 0xFFFF)
                    break;
                
                set++;

                if ((set - 1) == destSet)
                {
                    pointer = lastPointer;
                    colorIndex = SNES.rom[offset + 2];
                }
                offset += 3;
            } while (true);

            colorIndexInt.Value = colorIndex;
            colorAddressBox.Text = $"{pointer:X4}";
            paletteSetInt.Maximum = set - 1;
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
        private void EditPaletteSetProperty(bool updateAddr = true)
        {
            int listOffset = BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(Const.BackgroundPaletteOffset + Level.Id * 2)) + Const.BackgroundPaletteOffset;
            int offset = BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(listOffset + (int)MainWindow.window.paletteE.bgPalIdInt.Value * 2)) + Const.BackgroundPaletteOffset;
            int savedOffset = offset;

            int colorIndex = SNES.rom[offset + 2];
            int set = 0;
            int destSet = (int)paletteSetInt.Value;
            ushort lastPointer = 0;
            do
            {
                lastPointer = BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(offset));
                if (lastPointer == 0xFFFF)
                    break;

                set++;

                if ((set - 1) == destSet)
                {
                    if (updateAddr)
                        BinaryPrimitives.WriteUInt16LittleEndian(SNES.rom.AsSpan(offset), (ushort)int.Parse(colorAddressBox.Text, NumberStyles.HexNumber));
                    else
                        SNES.rom[offset + 2] = (byte)(int)colorIndexInt.Value;
                    return;
                }
                offset += 3;
            } while (true);
            SNES.edit = true;
            DrawSwappablePalette(savedOffset);
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
        private void bgPalIdInt_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue == null || SNES.rom == null) return;

            suspendSetInt = true;
            paletteSetInt.Value = 0;
            SetupSwappablePaletteUI();
            suspendSetInt = false;
        }
        private void paletteSetInt_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue == null || SNES.rom == null || suspendSetInt) return;

            SetupSwappablePaletteUI();
        }
        private void colorIndexInt_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue == null || SNES.rom == null || suspendColorInt) return;

            EditPaletteSetProperty(false);
        }
        private void colorAddressBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (suspendAddrBox || !int.TryParse(colorAddressBox.Text, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int result)) return;

            EditPaletteSetProperty();
        }
        private void DumpPaletteBtn_Click(object sender, RoutedEventArgs e)
        {
            if (!paletteSetInt.IsEnabled) return;

            int listOffset = BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(Const.BackgroundPaletteOffset + Level.Id * 2)) + Const.BackgroundPaletteOffset;
            int offset = BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(listOffset + (int)MainWindow.window.paletteE.bgPalIdInt.Value * 2)) + Const.BackgroundPaletteOffset;

            ushort lastPointer = 0;

            do
            {
                lastPointer = BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(offset));
                if (lastPointer == 0xFFFF)
                    break;

                int colorIndex = SNES.rom[offset + 2];
                int colorOffset = SNES.CpuToOffset(lastPointer, Const.PaletteColorBank);

                for (int c = 0; c < 16; c++)
                {
                    ushort color = BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(colorOffset + c * 2));
                    byte R = (byte)(color % 32 * 8);
                    byte G = (byte)(color / 32 % 32 * 8);
                    byte B = (byte)(color / 1024 % 32 * 8);

                    Level.Palette[((colorIndex + c) >> 4) & 0xF, (colorIndex + c) & 0xF] = Color.FromRgb(R, G, B);
                }

                offset += 3;
            } while (true);

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
        #endregion Events
    }
}
