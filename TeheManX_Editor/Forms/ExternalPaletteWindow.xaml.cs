using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace TeheManX_Editor.Forms
{
    /// <summary>
    /// Interaction logic for ExternalPaletteWindow.xaml
    /// </summary>
    public partial class ExternalPaletteWindow : Window
    {
        #region Properties
        int palId;
        int groupId;
        byte[] rom;
        string saveLocation;

        int paletteInfoOffset;
        int paletteBank;
        int paletteColorBank;

        int colorDataOffset; //to avoid copying code
        int colorAmount;
        #endregion Properties

        #region Constructors
        internal ExternalPaletteWindow(byte[] rom, Const.GameId gameId,string saveLocation)
        {
            InitializeComponent();
            this.rom = rom;
            this.saveLocation = saveLocation;

            for (int i = 0; i < 16; i++)
            {
                colorGrid.ColumnDefinitions.Add(new ColumnDefinition());
                colorGrid.RowDefinitions.Add(new RowDefinition());
            }
            for (int y = 0; y < 16; y++)
            {
                for (int x = 0; x < 16; x++)
                {
                    //Create Color
                    Rectangle rect = new Rectangle();
                    rect.Focusable = false;
                    rect.Width = 16;
                    rect.Height = 16;
                    rect.Fill = Brushes.Transparent;
                    rect.MouseDown += Color_Down;
                    rect.HorizontalAlignment = HorizontalAlignment.Stretch;
                    rect.VerticalAlignment = VerticalAlignment.Stretch;
                    Grid.SetColumn(rect, x);
                    Grid.SetRow(rect, y);
                    colorGrid.Children.Add(rect);
                }
            }

            if (gameId == Const.GameId.MegaManX)
            {
                paletteInfoOffset = Const.MegaManX.PaletteInfoOffset;
                paletteBank = Const.MegaManX.PaletteBank;
                paletteColorBank = Const.MegaManX.PaletteColorBank;
            }
            else if (gameId == Const.GameId.MegaManX2)
            {
                paletteInfoOffset = Const.MegaManX2.PaletteInfoOffset;
                paletteBank = Const.MegaManX2.PaletteBank;
                paletteColorBank = Const.MegaManX2.PaletteColorBank;
            }
            else
            {
                paletteInfoOffset = Const.MegaManX3.PaletteInfoOffset;
                paletteBank = Const.MegaManX3.PaletteBank;
                paletteColorBank = Const.MegaManX3.PaletteColorBank;
            }

            UpdateColorUI();
        }
        #endregion Constructors

        #region Methods
        private void UpdateColorUI()
        {
            for (int i = 0; i < 256; i++)
            {
                Rectangle rect = colorGrid.Children[i] as Rectangle;
                rect.Fill = Brushes.Transparent;
            }

            int infoOffset = SNES.CpuToOffset(BinaryPrimitives.ReadUInt16LittleEndian(rom.AsSpan(paletteInfoOffset + palId)), paletteBank);

            int group = 0;

            if (rom[infoOffset] != 0)
            {
                importBtn.IsEnabled = true;
                exportBtn.IsEnabled = true;

                //Find the max groups
                int maxGroups = -1;
                int copyInfoOffset = infoOffset;

                while (rom[copyInfoOffset] != 0)
                {
                    copyInfoOffset += 4;
                    maxGroups++;
                }

                while (rom[infoOffset] != 0)
                {
                    int colorCount = rom[infoOffset]; //how many colors are going to be dumped
                    colorAmount = colorCount;
                    byte colorIndex = rom[infoOffset + 3]; //which color index to start dumping at
                    int address = BinaryPrimitives.ReadUInt16LittleEndian(rom.AsSpan(infoOffset + 1)) + (paletteColorBank << 16);
                    int colorOffset = SNES.CpuToOffset(address); //where the colors are located
                    colorDataOffset = colorOffset;

                    if (group == groupId)
                    {
                        colorCounTxt.Text = $"Color Count: {colorCount:X}";
                        colorAddressTxt.Text = $"Color Address: {address:X6}";
                        colorIndexTxt.Text = $"Color Index: {colorIndex:X}";
                        groupInt.Maximum = maxGroups;
                        if (groupId > maxGroups)
                        {
                            groupId = maxGroups;
                            groupInt.Value = maxGroups;
                        }
                    }
                    else
                    {
                        infoOffset += 4;
                        group++;
                        continue;
                    }    

                    for (int c = 0; c < colorCount; c++)
                    {
                        if (c > 0xFF)
                            return;

                        ushort color = BinaryPrimitives.ReadUInt16LittleEndian(rom.AsSpan(colorOffset + c * 2));
                        byte R = (byte)(color % 32 * 8);
                        byte G = (byte)(color / 32 % 32 * 8);
                        byte B = (byte)(color / 1024 % 32 * 8);

                        Rectangle rect = colorGrid.Children[c] as Rectangle;
                        rect.Fill = new SolidColorBrush(Color.FromRgb(R, G, B));
                    }
                    infoOffset += 4;
                    group++;
                }
            }
            else
            {
                importBtn.IsEnabled = false;
                importBtn.IsEnabled = false;

                groupId = 0;
                groupInt.Value = 0;
                groupInt.Maximum = 0;
                colorCounTxt.Text = "Color Count: 0";
                colorAddressTxt.Text = "Color Address: ?";
                colorIndexTxt.Text = "Color Index: ?";
            }
        }
        #endregion Methods

        #region Events
        private void palId_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue == null || palId == (int)e.NewValue) return;
            int val = (int)e.NewValue;
            if ((val & 1) != 0)
                return; //only even values

            palId = val;
            groupId = 0;
            groupInt.Value = 0;
            UpdateColorUI();
        }
        private void groupInt_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue == null || groupId == (int)e.NewValue) return;
            if ((palId & 1) != 0)
                return; //only even values

            groupId = (int)e.NewValue;
            UpdateColorUI();
        }
        private void importBtn_Click(object sender, RoutedEventArgs e)
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
                        BinaryPrimitives.WriteUInt16LittleEndian(rom.AsSpan(colorDataOffset + i * 2), newC);
                    }

                    UpdateColorUI();
                    MessageBox.Show("Colors Imported!");
                }
            }
        }
        private void exportBtn_Click(object sender, RoutedEventArgs e)
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
                            Rectangle rect = colorGrid.Children[i] as Rectangle;
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
                            Rectangle rect = colorGrid.Children[i] as Rectangle;
                            SolidColorBrush brush = (SolidColorBrush)rect.Fill;
                            Color color = brush.Color;
                            lines.Add($"#{color.R:X2}{color.G:X2}{color.B:X2}");
                        }
                        File.WriteAllLines(file, lines.ToArray());
                    }
                    MessageBox.Show("Colors Exported!");
                }
            }
        }
        private void exportAllBtn_Click(object sender, RoutedEventArgs e)
        {
            Button exportAsPal = new Button();
            exportAsPal.Content = "Export All as .PAL Files";
            exportAsPal.Click += (s , ev) =>
            {
                var sfd = new Ookii.Dialogs.Wpf.VistaFolderBrowserDialog();
                sfd.Description = "Select Save Location";
                sfd.UseDescriptionForTitle = true;

                if ((bool)sfd.ShowDialog())
                {
                    for (int p = 0; p <= ((int)palIdInt.Maximum + 2); p += 2)
                    {
                        MemoryStream ms = new MemoryStream();
                        BinaryWriter bw = new BinaryWriter(ms);

                        int infoOffset = SNES.CpuToOffset(BinaryPrimitives.ReadUInt16LittleEndian(rom.AsSpan(paletteInfoOffset + p)), paletteBank);
                        while (rom[infoOffset] != 0)
                        {
                            int colorCount = rom[infoOffset]; //how many colors are going to be dumped
                            int address = BinaryPrimitives.ReadUInt16LittleEndian(rom.AsSpan(infoOffset + 1)) + (paletteColorBank << 16);
                            int colorOffset = SNES.CpuToOffset(address); //where the colors are located
                            for (int c = 0; c < colorCount; c++)
                            {
                                ushort color = BinaryPrimitives.ReadUInt16LittleEndian(rom.AsSpan(colorOffset + c * 2));
                                byte R = (byte)(color % 32 * 8);
                                byte G = (byte)(color / 32 % 32 * 8);
                                byte B = (byte)(color / 1024 % 32 * 8);
                                bw.Write(R);
                                bw.Write(G);
                                bw.Write(B);
                            }
                            infoOffset += 4;
                        }
                        bw.Close();
                        File.WriteAllBytes($"{sfd.SelectedPath}\\Palette_{p:X2}.pal", ms.ToArray());
                        ms.Close();
                    }
                    MessageBox.Show("All Palettes Exported!");
                }
            };
            Button exportAsTxt = new Button();
            exportAsTxt.Content = "Export All as .TXT Files";
            exportAsTxt.Click += (s , ev) =>             {
                var sfd = new Ookii.Dialogs.Wpf.VistaFolderBrowserDialog();
                sfd.Description = "Select Save Location";
                sfd.UseDescriptionForTitle = true;
                if ((bool)sfd.ShowDialog())
                {
                    for (int p = 0; p <= ((int)palIdInt.Maximum + 2); p += 2)
                    {
                        List<string> lines = new List<string>();
                        int infoOffset = SNES.CpuToOffset(BinaryPrimitives.ReadUInt16LittleEndian(rom.AsSpan(paletteInfoOffset + p)), paletteBank);
                        while (rom[infoOffset] != 0)
                        {
                            int colorCount = rom[infoOffset]; //how many colors are going to be dumped
                            int address = BinaryPrimitives.ReadUInt16LittleEndian(rom.AsSpan(infoOffset + 1)) + (paletteColorBank << 16);
                            int colorOffset = SNES.CpuToOffset(address); //where the colors are located
                            for (int c = 0; c < colorCount; c++)
                            {
                                ushort color = BinaryPrimitives.ReadUInt16LittleEndian(rom.AsSpan(colorOffset + c * 2));
                                byte R = (byte)(color % 32 * 8);
                                byte G = (byte)(color / 32 % 32 * 8);
                                byte B = (byte)(color / 1024 % 32 * 8);
                                lines.Add($"#{R:X2}{G:X2}{B:X2}");
                            }
                            infoOffset += 4;
                        }
                        File.WriteAllLines($"{sfd.SelectedPath}\\Palette_{p:X2}.txt", lines.ToArray());
                    }
                    MessageBox.Show("All Palettes Exported!");
                }
            };
            Grid.SetRow(exportAsTxt, 1);
            Grid grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition());
            grid.RowDefinitions.Add(new RowDefinition());
            grid.Children.Add(exportAsPal);
            grid.Children.Add(exportAsTxt);

            Window window = new Window() { SizeToContent = SizeToContent.WidthAndHeight , ResizeMode = ResizeMode.NoResize , WindowStartupLocation = WindowStartupLocation.CenterScreen , Title = "Export Palettes"};

            window.Content = grid;
            window.ShowDialog();
        }
        private void saveBtn_Click(object sender, RoutedEventArgs e)
        {
            File.WriteAllBytes(saveLocation, rom);
            MessageBox.Show("Changes Saved!");
        }
        private void Color_Down(object sender, MouseButtonEventArgs e)
        {
            if (((Rectangle)sender).Fill == Brushes.Transparent) return;

            //Get Current Color
            int c = Grid.GetColumn(sender as UIElement);
            int r = Grid.GetRow(sender as UIElement);

            int offset = colorDataOffset + c * 2 + r * 16;

            ushort oldC = BinaryPrimitives.ReadUInt16LittleEndian(rom.AsSpan(offset));

            ColorDialog colorDialog = new ColorDialog(oldC, c, r);
            colorDialog.Owner = Application.Current.MainWindow;
            colorDialog.ShowDialog();

            if (colorDialog.confirm)
            {
                ushort newC = (ushort)(colorDialog.canvas.SelectedColor.Value.B / 8 * 1024 + colorDialog.canvas.SelectedColor.Value.G / 8 * 32 + colorDialog.canvas.SelectedColor.Value.R / 8);
                BinaryPrimitives.WriteUInt16LittleEndian(rom.AsSpan(offset), newC);

                //Convert & Change Clut in GUI
                byte R = (byte)(newC % 32 * 8);
                byte G = (byte)(newC / 32 % 32 * 8);
                byte B = (byte)(newC / 1024 % 32 * 8);
                Color color = Color.FromRgb(R, G, B);
                ((Rectangle)sender).Fill = new SolidColorBrush(color);
            }
        }
        #endregion Events
    }
}
