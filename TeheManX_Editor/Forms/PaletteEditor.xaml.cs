using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Windows;
using System.Windows.Shapes;
using System.Windows.Input;
using System;
using System.Buffers.Binary;

namespace TeheManX_Editor.Forms
{
    /// <summary>
    /// Interaction logic for PaletteEditor.xaml
    /// </summary>
    public partial class PaletteEditor : UserControl
    {
        #region Properties
        WriteableBitmap vramTiles = new WriteableBitmap(128, 512, 96, 96, PixelFormats.Bgra32, null);
        Rectangle selectSetRect = new Rectangle() { IsHitTestVisible = false, StrokeThickness = 2.5, StrokeDashArray = new DoubleCollection() { 2.2 }, CacheMode = null, Stroke = Brushes.PapayaWhip };
        public int palId = 0;
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

            vramTileImage.Source = vramTiles;
        }
        #endregion Constructors

        #region Methods
        public void AssignLimits()
        {
            DrawVramTiles();
            DrawPalette();
        }
        public unsafe void DrawVramTiles()
        {
            vramTiles.Lock();
            byte* buffer = (byte*)vramTiles.BackBuffer;
            int set = palId;

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
            MainWindow.window.paletteE.palTxt.Text = $"Palette: {palId}";
        }
        public void UpdateCursor()
        {
            Grid.SetRow(selectSetRect, palId);
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
                    if (Const.Id == Const.GameId.MegaManX3 && Level.Id == 0xE)
                        id = 0xB; //special case for MMX3 rekt version of dophler 2
                    else
                        id = Level.Id;

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
                        if (palId == r)
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
                palId = Grid.GetRow(sender as UIElement);
                UpdatePaletteText();
                DrawVramTiles();
                UpdateCursor();
            }
        }
        #endregion Events
    }
}
