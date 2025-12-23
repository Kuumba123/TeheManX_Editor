using System;
using System.Buffers.Binary;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace TeheManX_Editor.Forms
{
    /// <summary>
    /// Interaction logic for Tile16Editor.xaml
    /// </summary>
    public partial class Tile16Editor : UserControl
    {
        #region Fields
        public static double scale = 1;
        #endregion Fields

        #region Properties
        public bool updateVramTiles;
        public bool updateTiles;
        public bool updateTile;
        WriteableBitmap x16BMP = new WriteableBitmap(256, 256, 96, 96, PixelFormats.Bgra32, null);
        WriteableBitmap vramTiles = new WriteableBitmap(128, 512, 96, 96, PixelFormats.Bgra32, null);
        WriteableBitmap tileBMP_S = new WriteableBitmap(16, 16, 96, 96, PixelFormats.Bgra32, null);
        public int page = 0;
        public int palId = 0;
        public int selectedTile = 0;
        public int selectedInnerTile = 0;
        Button past;    //for X16 Page buttons
        Button past2;   //for Palette buttons
        #endregion Properties

        #region Constructors
        public Tile16Editor()
        {
            InitializeComponent();

            // 64 rows
            for (int row = 0; row < 64; row++)
            {
                vramGrid.RowDefinitions.Add(new RowDefinition());
            }

            // 16 columns
            for (int col = 0; col < 16; col++)
            {
                vramGrid.ColumnDefinitions.Add(new ColumnDefinition());
            }


            x16Image.Source = x16BMP;
            vramTileImage.Source = vramTiles;
            tileImageS.Source = tileBMP_S;
        }
        #endregion Constructors

        #region Methods
        public void AssignLimits()
        {
            int Id;
            if (Const.Id == Const.GameId.MegaManX3 && Level.Id == 0xE) Id = 0x10; //special case for MMX3 rekt version of dophler 2
            else if (Const.Id == Const.GameId.MegaManX3 && Level.Id > 0xE) Id = (Level.Id - 0xF) + 0xE; //Buffalo or Beetle
            else Id = Level.Id;

            int max16 = Const.Tile16Count[Level.Id, Level.BG] - 1;
            MainWindow.window.tile16E.tileInt.Maximum = max16;
            if (selectedTile > max16)
                MainWindow.window.tile16E.tileInt.Value = max16;
            int offset = SNES.CpuToOffset(BitConverter.ToInt32(SNES.rom, Const.TileCollisionDataPointersOffset + Id * 3));
            collisionInt.Value = SNES.rom[offset + selectedTile];

            Draw16xTiles();
            DrawVramTiles();
            DrawTile();
            UpdateTile16SelectionUI();
            UpdateTile8SelectionUI();
            UpdateTileAttributeUI();
        }
        public unsafe void PaintVramTiles()
        {
            updateVramTiles = false;
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

                            // compute pixel position once and write 32-bit BGRA in a single store
                            int px = x * 8 + col;
                            int py = y * 8 + row;
                            int baseIdx = px * 4 + py * vramTiles.BackBufferStride;
                            Color colStruct = Level.Palette[set, index];
                            uint bgra = (0xFFu << 24) | ((uint)colStruct.R << 16) | ((uint)colStruct.G << 8) | (uint)colStruct.B;
                            *(uint*)(buffer + baseIdx) = bgra;
                        }
                    }
                }
            }
            vramTiles.AddDirtyRect(new Int32Rect(0, 0, 128, 512));
            vramTiles.Unlock();
        }
        public unsafe void PaintTiles()
        {
            updateTiles = false;
            x16BMP.Lock();

            int Id;
            if (Const.Id == Const.GameId.MegaManX3 && Level.Id == 0xE) Id = 0x10; //special case for MMX3 rekt version of dophler 2
            else if (Const.Id == Const.GameId.MegaManX3 && Level.Id > 0xE) Id = (Level.Id - 0xF) + 0xE; //Buffalo or Beetle
            else Id = Level.Id;

            for (int y = 0; y < 16; y++)
            {
                for (int x = 0; x < 16; x++)
                {
                    int id = x + (y * 16) + (page * 0x100);
                    if (id > (Const.Tile16Count[Level.Id, Level.BG]) - 1)
                    {
                        unsafe
                        {
                            byte* buffer = (byte*)x16BMP.BackBuffer;
                            uint val = 0xFF000000;
                            for (int r = 0; r < 16; r++)
                            {
                                for (int c = 0; c < 16; c++)
                                {
                                    *(uint*)(buffer + (x * 16 + c) * 4 + (y * 16 + r) * x16BMP.BackBufferStride) = val;
                                }
                            }
                        }
                        continue;
                    }
                    Level.Draw16xTile(id, x * 16, y * 16, x16BMP.BackBufferStride, x16BMP.BackBuffer);
                }
            }
            x16BMP.AddDirtyRect(new Int32Rect(0, 0, 256, 256));
            x16BMP.Unlock();
        }
        public unsafe void PaintTile()
        {
            updateTile = false;
            tileBMP_S.Lock();
            Level.Draw16xTile(selectedTile, 0, 0, tileBMP_S.BackBufferStride, tileBMP_S.BackBuffer);
            tileBMP_S.AddDirtyRect(new Int32Rect(0, 0, 16, 16));
            tileBMP_S.Unlock();
        }
        public unsafe void DrawVramTiles()
        {
            updateVramTiles = true;
        }
        public void Draw16xTiles()
        {
            updateTiles = true;
        }
        public void DrawTile()
        {
            updateTile = true;
        }
        private void UpdateTile16SelectionUI()
        {
            ushort tile16 = (ushort)selectedTile;

            if (page != ((tile16 >> 8) & 0xFF))
                tile16Cursor.Visibility = Visibility.Hidden;
            else
            {
                Grid.SetColumn(tile16Cursor, tile16 & 0x0F);
                Grid.SetRow(tile16Cursor, (tile16 >> 4) & 0x0F);
                tile16Cursor.Visibility = Visibility.Visible;
            }
        }
        public void UpdateTile8SelectionUI()
        {
            int Id;
            if (Const.Id == Const.GameId.MegaManX3 && Level.Id == 0xE) Id = 0x10; //special case for MMX3 rekt version of dophler 2
            else if (Const.Id == Const.GameId.MegaManX3 && Level.Id > 0xE) Id = (Level.Id - 0xF) + 0xE; //Buffalo or Beetle
            else Id = Level.Id;

            int offset = SNES.CpuToOffset(BitConverter.ToInt32(SNES.rom, Const.Tile16DataPointersOffset[Level.BG] + Id * 3));
            offset += selectedTile * 8 + selectedInnerTile * 2;

            ushort val = BitConverter.ToUInt16(SNES.rom, offset);

            Grid.SetColumn(tile8Cursor, val & 0xF);
            Grid.SetRow(tile8Cursor, (val >> 4) & 0x3F);
        }
        public void UpdateTileAttributeUI()
        {
            int Id;
            if (Const.Id == Const.GameId.MegaManX3 && Level.Id == 0xE) Id = 0x10; //special case for MMX3 rekt version of dophler 2
            else if (Const.Id == Const.GameId.MegaManX3 && Level.Id > 0xE) Id = (Level.Id - 0xF) + 0xE; //Buffalo or Beetle
            else Id = Level.Id;

            int offset = SNES.CpuToOffset(BitConverter.ToInt32(SNES.rom, Const.Tile16DataPointersOffset[Level.BG] + Id * 3));

            ushort val = BitConverter.ToUInt16(SNES.rom, offset + selectedTile * 8 + selectedInnerTile * 2);

            vramInt.Value = (val & 0x3FF);
            palInt.Value = (val >> 10) & 7;
            priorityCheck.IsChecked = (val & 0x2000) != 0;
            flipHCheck.IsChecked = (val & 0x4000) != 0;
            flipVCheck.IsChecked = (val & 0x8000) != 0;
        }
        #endregion Methods

        #region Events
        private void x16Image_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var p = e.GetPosition(MainWindow.window.tile16E.x16Image);
            int x = (int)p.X;
            int y = (int)p.Y;
            int cX = SNES.GetSelectedTile(x, MainWindow.window.tile16E.x16Image.ActualWidth, 16);
            int cY = SNES.GetSelectedTile(y, MainWindow.window.tile16E.x16Image.ActualHeight, 16);
            int id = cX + (cY * 16);
            if ((uint)id > 0xFF)
                id = 0xFF;

            int Id;
            if (Const.Id == Const.GameId.MegaManX3 && Level.Id == 0xE) Id = 0x10; //special case for MMX3 rekt version of dophler 2
            else if (Const.Id == Const.GameId.MegaManX3 && Level.Id > 0xE) Id = (Level.Id - 0xF) + 0xE; //Buffalo or Beetle
            else Id = Level.Id;

            id += page * 0x100;
            if (id > (Const.Tile16Count[Id, Level.BG]) - 1)
                return;

            selectedTile = id;
            tileInt.Value = selectedTile;
            int offset = SNES.CpuToOffset(BitConverter.ToInt32(SNES.rom, Const.TileCollisionDataPointersOffset + Id * 3));
            collisionInt.Value = SNES.rom[offset + selectedTile];

            DrawTile();
            UpdateTile16SelectionUI();
            UpdateTile8SelectionUI();
            UpdateTileAttributeUI();
        }
        private void Clut_Click(object sender, RoutedEventArgs e)
        {
            Button b = (Button)sender;
            int i = Convert.ToInt32(b.Content.ToString().Trim(), 16);
            if (palId == i)
                return;
            palId = i;
            if (past2 != null)
            {
                past2.Background = Brushes.Black;
                past2.Foreground = Brushes.White;
            }
            b.Background = Brushes.LightBlue;
            b.Foreground = Brushes.Black;
            past2 = b;
            DrawVramTiles();
            DrawTile();
            UpdateTile8SelectionUI();
        }
        private void Page_Click(object sender, RoutedEventArgs e)
        {
            Button b = (Button)sender;
            int i = Convert.ToInt32(b.Content.ToString().Trim(), 16);
            if (page == i)
                return;
            page = i;
            if (past != null)
            {
                past.Background = Brushes.Black;
                past.Foreground = Brushes.White;
            }
            b.Background = Brushes.LightBlue;
            b.Foreground = Brushes.Black;
            past = b;
            Draw16xTiles();
            UpdateTile16SelectionUI();
        }
        private void tileImageS_MouseUp(object sender, MouseButtonEventArgs e)
        {
            var p = e.GetPosition(MainWindow.window.tile16E.tileImageS);
            int x = (int)p.X;
            int y = (int)p.Y;
            int cX = SNES.GetSelectedTile(x, MainWindow.window.tile16E.tileImageS.ActualWidth, 2);
            int cY = SNES.GetSelectedTile(y, MainWindow.window.tile16E.tileImageS.ActualHeight, 2);

            if (e.ChangedButton == MouseButton.Left)
            {
                selectedInnerTile = cX + (cY * 2);
                Grid.SetColumn(innerCursor, cX);
                Grid.SetRow(innerCursor, cY);
                UpdateTile8SelectionUI();
                UpdateTileAttributeUI();
            }
        }
        private void vramTileImage_MouseUp(object sender, MouseButtonEventArgs e)
        {
            var p = e.GetPosition(MainWindow.window.tile16E.vramTileImage);
            int x = (int)p.X;
            int y = (int)p.Y;
            int cX = SNES.GetSelectedTile(x, MainWindow.window.tile16E.vramTileImage.ActualWidth, 16);
            int cY = SNES.GetSelectedTile(y, MainWindow.window.tile16E.vramTileImage.ActualHeight, 64);

            int Id;
            if (Const.Id == Const.GameId.MegaManX3 && Level.Id == 0xE) Id = 0x10; //special case for MMX3 rekt version of dophler 2
            else if (Const.Id == Const.GameId.MegaManX3 && Level.Id > 0xE) Id = (Level.Id - 0xF) + 0xE; //Buffalo or Beetle
            else Id = Level.Id;

            if (e.ChangedButton == MouseButton.Left)
            {
                int offset = SNES.CpuToOffset(BitConverter.ToInt32(SNES.rom, Const.Tile16DataPointersOffset[Level.BG] + Id * 3));
                if (MainWindow.undos.Count == Const.MaxUndo)
                    MainWindow.undos.RemoveAt(0);
                MainWindow.undos.Add(Undo.CreateTile16Undo((ushort)selectedTile, BinaryPrimitives.ReadUInt64LittleEndian(SNES.rom.AsSpan(offset + selectedTile * 8))));
                offset += selectedTile * 8 + selectedInnerTile * 2;

                ushort val = BitConverter.ToUInt16(SNES.rom, offset);
                ushort newVal = (ushort)((val & 0xFC00) | (cX + (cY * 16)));
                BinaryPrimitives.WriteUInt16LittleEndian(SNES.rom.AsSpan(offset), newVal);
                SNES.edit = true;
                DrawTile();
                Draw16xTiles();
                UpdateTile8SelectionUI();
                UpdateTileAttributeUI();

                MainWindow.window.layoutE.DrawLayout();
                MainWindow.window.layoutE.DrawScreen();
                MainWindow.window.enemyE.DrawLayout();
                MainWindow.window.screenE.DrawScreen();
                MainWindow.window.screenE.DrawTiles();
                MainWindow.window.screenE.DrawTile();
                MainWindow.window.tile32E.DrawTiles();
                MainWindow.window.tile32E.Draw16xTiles();
                MainWindow.window.tile32E.DrawTile();
            }
        }
        private void x16GridBtn_Click(object sender, RoutedEventArgs e)
        {
            if (MainWindow.window.tile16E.x16grid.ShowGridLines)
                MainWindow.window.tile16E.x16grid.ShowGridLines = false;
            else
                MainWindow.window.tile16E.x16grid.ShowGridLines = true;
        }
        private void x8GridBtn_Click(object sender, RoutedEventArgs e)
        {
            if (MainWindow.window.tile16E.vramGrid.ShowGridLines)
                MainWindow.window.tile16E.vramGrid.ShowGridLines = false;
            else
                MainWindow.window.tile16E.vramGrid.ShowGridLines = true;
        }
        private void tileInt_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue == null || SNES.rom == null)
                return;
            if (selectedTile == (int)e.NewValue)
                return;

            int Id;
            if (Const.Id == Const.GameId.MegaManX3 && Level.Id == 0xE) Id = 0x10; //special case for MMX3 rekt version of dophler 2
            else if (Const.Id == Const.GameId.MegaManX3 && Level.Id > 0xE) Id = (Level.Id - 0xF) + 0xE; //Buffalo or Beetle
            else Id = Level.Id;

            selectedTile = (int)e.NewValue;
            int offset = SNES.CpuToOffset(BitConverter.ToInt32(SNES.rom, Const.TileCollisionDataPointersOffset + Id * 3));
            collisionInt.Value = SNES.rom[offset + selectedTile];
            UpdateTile16SelectionUI();
            UpdateTile8SelectionUI();
            DrawTile();
        }
        private void collisionInt_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue == null || SNES.rom == null)
                return;

            int Id;
            if (Const.Id == Const.GameId.MegaManX3 && Level.Id == 0xE) Id = 0x10; //special case for MMX3 rekt version of dophler 2
            else if (Const.Id == Const.GameId.MegaManX3 && Level.Id > 0xE) Id = (Level.Id - 0xF) + 0xE; //Buffalo or Beetle
            else Id = Level.Id;

            int offset = SNES.CpuToOffset(BitConverter.ToInt32(SNES.rom, Const.TileCollisionDataPointersOffset + Id * 3));
            offset += selectedTile;

            byte val = SNES.rom[offset];
            if (val == (byte)(int)e.NewValue)
                return;

            if (MainWindow.undos.Count == Const.MaxUndo)
                MainWindow.undos.RemoveAt(0);
            MainWindow.undos.Add(Undo.CreateCollisionUndo((ushort)selectedTile, SNES.rom[offset]));

            SNES.rom[offset] = (byte)(int)e.NewValue;
            SNES.edit = true;
        }
        private void vramInt_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue == null || SNES.rom == null)
                return;

            int Id;
            if (Const.Id == Const.GameId.MegaManX3 && Level.Id == 0xE) Id = 0x10; //special case for MMX3 rekt version of dophler 2
            else if (Const.Id == Const.GameId.MegaManX3 && Level.Id > 0xE) Id = (Level.Id - 0xF) + 0xE; //Buffalo or Beetle
            else Id = Level.Id;

            int offset = SNES.CpuToOffset(BitConverter.ToInt32(SNES.rom, Const.Tile16DataPointersOffset[Level.BG] + Id * 3));
            int tileBase = offset + selectedTile * 8;
            offset += selectedTile * 8 + selectedInnerTile * 2;

            ushort val = BitConverter.ToUInt16(SNES.rom, offset);
            if ((val & 0x3FF) == (int)e.NewValue)
                return;

            if (MainWindow.undos.Count == Const.MaxUndo)
                MainWindow.undos.RemoveAt(0);
            MainWindow.undos.Add(Undo.CreateTile16Undo((ushort)selectedTile, BinaryPrimitives.ReadUInt64LittleEndian(SNES.rom.AsSpan(tileBase))));

            BinaryPrimitives.WriteUInt16LittleEndian(SNES.rom.AsSpan(offset), (ushort)((val & 0xFC00) | ((int)e.NewValue)));
            SNES.edit = true;
            DrawTile();
            Draw16xTiles();

            MainWindow.window.layoutE.DrawLayout();
            MainWindow.window.layoutE.DrawScreen();
            MainWindow.window.enemyE.DrawLayout();
            MainWindow.window.screenE.DrawScreen();
            MainWindow.window.screenE.DrawTiles();
            MainWindow.window.screenE.DrawTile();
            MainWindow.window.tile32E.DrawTiles();
            MainWindow.window.tile32E.Draw16xTiles();
            MainWindow.window.tile32E.DrawTile();
        }
        private void palInt_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue == null || SNES.rom == null)
                return;

            int Id;
            if (Const.Id == Const.GameId.MegaManX3 && Level.Id == 0xE) Id = 0x10; //special case for MMX3 rekt version of dophler 2
            else if (Const.Id == Const.GameId.MegaManX3 && Level.Id > 0xE) Id = (Level.Id - 0xF) + 0xE; //Buffalo or Beetle
            else Id = Level.Id;

            int offset = SNES.CpuToOffset(BitConverter.ToInt32(SNES.rom, Const.Tile16DataPointersOffset[Level.BG] + Id * 3));
            int tileBase = offset + selectedTile * 8;
            offset += selectedTile * 8 + selectedInnerTile * 2;

            ushort val = BitConverter.ToUInt16(SNES.rom, offset);
            if (((val >> 10) & 7) == (int)e.NewValue)
                return;

            if (MainWindow.undos.Count == Const.MaxUndo)
                MainWindow.undos.RemoveAt(0);
            MainWindow.undos.Add(Undo.CreateTile16Undo((ushort)selectedTile, BinaryPrimitives.ReadUInt64LittleEndian(SNES.rom.AsSpan(tileBase))));

            BinaryPrimitives.WriteUInt16LittleEndian(SNES.rom.AsSpan(offset), (ushort)((val & 0xE3FF) | ((int)e.NewValue << 10)));
            SNES.edit = true;
            DrawTile();
            Draw16xTiles();

            MainWindow.window.layoutE.DrawLayout();
            MainWindow.window.layoutE.DrawScreen();
            MainWindow.window.enemyE.DrawLayout();
            MainWindow.window.screenE.DrawScreen();
            MainWindow.window.screenE.DrawTiles();
            MainWindow.window.screenE.DrawTile();
            MainWindow.window.tile32E.DrawTiles();
            MainWindow.window.tile32E.Draw16xTiles();
            MainWindow.window.tile32E.DrawTile();
        }
        private void priorityCheck_CheckChange(object sender, RoutedEventArgs e)
        {
            if (SNES.rom == null)
                return;

            int Id;
            if (Const.Id == Const.GameId.MegaManX3 && Level.Id == 0xE) Id = 0x10; //special case for MMX3 rekt version of dophler 2
            else if (Const.Id == Const.GameId.MegaManX3 && Level.Id > 0xE) Id = (Level.Id - 0xF) + 0xE; //Buffalo or Beetle
            else Id = Level.Id;

            int offset = SNES.CpuToOffset(BitConverter.ToInt32(SNES.rom, Const.Tile16DataPointersOffset[Level.BG] + Id * 3));
            int tileBase = offset + selectedTile * 8;
            offset += selectedTile * 8 + selectedInnerTile * 2;

            ushort val = BitConverter.ToUInt16(SNES.rom, offset);

            if ((((val & 0x2000) != 0) ? 1 : 0) == ((priorityCheck.IsChecked == true) ? 1 : 0))
                return;

            if (MainWindow.undos.Count == Const.MaxUndo)
                MainWindow.undos.RemoveAt(0);
            MainWindow.undos.Add(Undo.CreateTile16Undo((ushort)selectedTile, BinaryPrimitives.ReadUInt64LittleEndian(SNES.rom.AsSpan(tileBase))));

            BinaryPrimitives.WriteUInt16LittleEndian(SNES.rom.AsSpan(offset), (ushort)(val ^ 0x2000));
            SNES.edit = true;
            DrawTile();
            Draw16xTiles();

            MainWindow.window.layoutE.DrawLayout();
            MainWindow.window.layoutE.DrawScreen();
            MainWindow.window.enemyE.DrawLayout();
            MainWindow.window.screenE.DrawScreen();
            MainWindow.window.screenE.DrawTiles();
            MainWindow.window.screenE.DrawTile();
            MainWindow.window.tile32E.DrawTiles();
            MainWindow.window.tile32E.Draw16xTiles();
            MainWindow.window.tile32E.DrawTile();
        }
        private void flipHCheck_CheckChange(object sender, RoutedEventArgs e)
        {
            if (SNES.rom == null)
                return;

            int Id;
            if (Const.Id == Const.GameId.MegaManX3 && Level.Id == 0xE) Id = 0x10; //special case for MMX3 rekt version of dophler 2
            else if (Const.Id == Const.GameId.MegaManX3 && Level.Id > 0xE) Id = (Level.Id - 0xF) + 0xE; //Buffalo or Beetle
            else Id = Level.Id;

            int offset = SNES.CpuToOffset(BitConverter.ToInt32(SNES.rom, Const.Tile16DataPointersOffset[Level.BG] + Id * 3));
            int tileBase = offset + selectedTile * 8;
            offset += selectedTile * 8 + selectedInnerTile * 2;

            ushort val = BitConverter.ToUInt16(SNES.rom, offset);

            if ((((val & 0x4000) != 0) ? 1 : 0) == ((flipHCheck.IsChecked == true) ? 1 : 0))
                return;

            if (MainWindow.undos.Count == Const.MaxUndo)
                MainWindow.undos.RemoveAt(0);
            MainWindow.undos.Add(Undo.CreateTile16Undo((ushort)selectedTile, BinaryPrimitives.ReadUInt64LittleEndian(SNES.rom.AsSpan(tileBase))));

            BinaryPrimitives.WriteUInt16LittleEndian(SNES.rom.AsSpan(offset), (ushort)(val ^ 0x4000));
            SNES.edit = true;
            DrawTile();
            Draw16xTiles();

            MainWindow.window.layoutE.DrawLayout();
            MainWindow.window.layoutE.DrawScreen();
            MainWindow.window.enemyE.DrawLayout();
            MainWindow.window.screenE.DrawScreen();
            MainWindow.window.screenE.DrawTiles();
            MainWindow.window.screenE.DrawTile();
            MainWindow.window.tile32E.DrawTiles();
            MainWindow.window.tile32E.Draw16xTiles();
            MainWindow.window.tile32E.DrawTile();
        }
        private void flipVCheck_CheckChange(object sender, RoutedEventArgs e)
        {
            if (SNES.rom == null)
                return;

            int Id;
            if (Const.Id == Const.GameId.MegaManX3 && Level.Id == 0xE) Id = 0x10; //special case for MMX3 rekt version of dophler 2
            else if (Const.Id == Const.GameId.MegaManX3 && Level.Id > 0xE) Id = (Level.Id - 0xF) + 0xE; //Buffalo or Beetle
            else Id = Level.Id;

            int offset = SNES.CpuToOffset(BitConverter.ToInt32(SNES.rom, Const.Tile16DataPointersOffset[Level.BG] + Id * 3));
            int tileBase = offset + selectedTile * 8;
            offset += selectedTile * 8 + selectedInnerTile * 2;

            ushort val = BitConverter.ToUInt16(SNES.rom, offset);

            if ((((val & 0x8000) != 0) ? 1 : 0) == ((flipVCheck.IsChecked == true) ? 1 : 0))
                return;

            if (MainWindow.undos.Count == Const.MaxUndo)
                MainWindow.undos.RemoveAt(0);
            MainWindow.undos.Add(Undo.CreateTile16Undo((ushort)selectedTile, BinaryPrimitives.ReadUInt64LittleEndian(SNES.rom.AsSpan(tileBase))));

            BinaryPrimitives.WriteUInt16LittleEndian(SNES.rom.AsSpan(offset), (ushort)(val ^ 0x8000));
            SNES.edit = true;
            DrawTile();
            Draw16xTiles();

            MainWindow.window.layoutE.DrawLayout();
            MainWindow.window.layoutE.DrawScreen();
            MainWindow.window.enemyE.DrawLayout();
            MainWindow.window.screenE.DrawScreen();
            MainWindow.window.screenE.DrawTiles();
            MainWindow.window.screenE.DrawTile();
            MainWindow.window.tile32E.DrawTiles();
            MainWindow.window.tile32E.Draw16xTiles();
            MainWindow.window.tile32E.DrawTile();
        }
        #endregion Events
    }
}
