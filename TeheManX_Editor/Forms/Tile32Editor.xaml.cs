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
    /// Interaction logic for Tile32Editor.xaml
    /// </summary>
    public partial class Tile32Editor : UserControl
    {
        #region Properties
        WriteableBitmap x16BMP = new WriteableBitmap(256, 256, 96, 96, PixelFormats.Rgb24, null);
        WriteableBitmap tileBMP = new WriteableBitmap(256, 1024, 96, 96, PixelFormats.Rgb24, null);
        WriteableBitmap tileBMP_S = new WriteableBitmap(32, 32, 96, 96, PixelFormats.Rgb24, null);
        Button past;
        Button past2; //X16
        public int page = 0;    //x32
        public int page2 = 0;   //x16
        public int selectedTile = 0;
        public int selectedInnerTile = 0;
        #endregion Properties

        #region Constructors
        public Tile32Editor()
        {
            InitializeComponent();

            x16Image.Source = x16BMP;
            tileImage.Source = tileBMP;
            tileImageS.Source = tileBMP_S;
        }
        #endregion Constructors
        
        #region Methods
        public void AssignLimits()
        {
            int tile32Amount = Const.Tile32Count[Level.Id, Level.BG] - 1;
            MainWindow.window.tile32E.tileInt.Maximum = tile32Amount;
            if (selectedTile > tile32Amount)
                MainWindow.window.tile32E.tileInt.Value = tile32Amount;

            int offset = SNES.CpuToOffset(BitConverter.ToInt32(SNES.rom, Const.Tile32DataPointersOffset[Level.BG] + Level.Id * 3));

            int max16 = Const.Tile16Count[Level.Id, Level.BG] - 1;

            MainWindow.window.tile32E.topLeftInt.Value = BitConverter.ToUInt16(SNES.rom, offset + selectedTile * 8 + 0);
            MainWindow.window.tile32E.topLeftInt.Maximum = max16;
            MainWindow.window.tile32E.topRightInt.Value = BitConverter.ToUInt16(SNES.rom, offset + selectedTile * 8 + 2);
            MainWindow.window.tile32E.topRightInt.Maximum = max16;
            MainWindow.window.tile32E.bottomLeftInt.Value = BitConverter.ToUInt16(SNES.rom, offset + selectedTile * 8 + 4);
            MainWindow.window.tile32E.bottomLeftInt.Maximum = max16;
            MainWindow.window.tile32E.bottomRightInt.Value = BitConverter.ToUInt16(SNES.rom, offset + selectedTile * 8 + 6);
            MainWindow.window.tile32E.bottomRightInt.Maximum = max16;

            UpdateTile16SelectionUI();
            DrawTiles();
            Draw16xTiles();
            DrawTile();
        }
        public void DrawTiles()
        {
            int tile32Offset = SNES.CpuToOffset(BitConverter.ToInt32(SNES.rom, Const.Tile32DataPointersOffset[Level.BG] + Level.Id * 3));
            tileBMP.Lock();

            for (int y = 0; y < 32; y++)
            {
                for (int x = 0; x < 8; x++)
                {
                    int tileId32 = x + (y * 8) + (page * 0x100);
                    if (tileId32 > (Const.Tile32Count[Level.Id, Level.BG] - 1))
                    {
                        unsafe
                        {
                            byte* buffer = (byte*)tileBMP.BackBuffer;
                            for (int r = 0; r < 32; r++)
                            {
                                for (int c = 0; c < 32; c++)
                                {
                                    buffer[(x * 32 + c) * 3 + (y * 32 + r) * tileBMP.BackBufferStride + 0] = 0;
                                    buffer[(x * 32 + c) * 3 + (y * 32 + r) * tileBMP.BackBufferStride + 1] = 0;
                                    buffer[(x * 32 + c) * 3 + (y * 32 + r) * tileBMP.BackBufferStride + 2] = 0;
                                }
                            }
                        }
                        continue;
                    }
                    Level.Draw16xTile(BitConverter.ToUInt16(SNES.rom, tile32Offset + (tileId32 * 8)), x * 32, y * 32, tileBMP.BackBufferStride, tileBMP.BackBuffer);
                    Level.Draw16xTile(BitConverter.ToUInt16(SNES.rom, tile32Offset + (tileId32 * 8) + 2), x * 32 + 16, y * 32, tileBMP.BackBufferStride, tileBMP.BackBuffer);
                    Level.Draw16xTile(BitConverter.ToUInt16(SNES.rom, tile32Offset + (tileId32 * 8) + 4), x * 32, y * 32 + 16, tileBMP.BackBufferStride, tileBMP.BackBuffer);
                    Level.Draw16xTile(BitConverter.ToUInt16(SNES.rom, tile32Offset + (tileId32 * 8) + 6), x * 32 + 16, y * 32 + 16, tileBMP.BackBufferStride, tileBMP.BackBuffer);
                }
            }
            tileBMP.AddDirtyRect(new Int32Rect(0, 0, 256, 1024));
            tileBMP.Unlock();
        }
        public void Draw16xTiles()
        {
            x16BMP.Lock();
            for (int y = 0; y < 16; y++)
            {
                for (int x = 0; x < 16; x++)
                {
                    int id = x + (y * 16) + (page2 * 0x100);
                    if (id > (Const.Tile16Count[Level.Id, Level.BG] - 1))
                    {
                        unsafe
                        {
                            byte* buffer = (byte*)x16BMP.BackBuffer;
                            for (int r = 0; r < 16; r++)
                            {
                                for (int c = 0; c < 16; c++)
                                {
                                    buffer[(x * 16 + c) * 3 + (y * 16 + r) * x16BMP.BackBufferStride + 0] = 0;
                                    buffer[(x * 16 + c) * 3 + (y * 16 + r) * x16BMP.BackBufferStride + 1] = 0;
                                    buffer[(x * 16 + c) * 3 + (y * 16 + r) * x16BMP.BackBufferStride + 2] = 0;
                                }
                            }
                        }
                        continue;
                    }
                    Level.Draw16xTile(id, x * 16, y * 16, 768, x16BMP.BackBuffer);
                }
            }
            x16BMP.AddDirtyRect(new Int32Rect(0, 0, 256, 256));
            x16BMP.Unlock();
        }
        public void DrawTile()
        {
            tileBMP_S.Lock();
            int tile32Offset = SNES.CpuToOffset(BitConverter.ToInt32(SNES.rom, Const.Tile32DataPointersOffset[Level.BG] + Level.Id * 3));
            Level.Draw16xTile(BitConverter.ToUInt16(SNES.rom, tile32Offset + (selectedTile * 8)), 0, 0, tileBMP_S.BackBufferStride, tileBMP_S.BackBuffer);
            Level.Draw16xTile(BitConverter.ToUInt16(SNES.rom, tile32Offset + (selectedTile * 8) + 2), 16, 0, tileBMP_S.BackBufferStride, tileBMP_S.BackBuffer);
            Level.Draw16xTile(BitConverter.ToUInt16(SNES.rom, tile32Offset + (selectedTile * 8) + 4), 0, 16, tileBMP_S.BackBufferStride, tileBMP_S.BackBuffer);
            Level.Draw16xTile(BitConverter.ToUInt16(SNES.rom, tile32Offset + (selectedTile * 8) + 6), 16, 16, tileBMP_S.BackBufferStride, tileBMP_S.BackBuffer);
            tileBMP_S.AddDirtyRect(new Int32Rect(0, 0, 32, 32));
            tileBMP_S.Unlock();
        }
        private void ChangePageTxt()
        {
            //pageBtn.Content = Convert.ToString(page).PadRight(3, '0') + "-" + Convert.ToString(page).PadRight(3, 'F');
        }
        public void UpdateTile32SelectionUI() //update the cursor in the 32x32 tile selection area
        {
            if (page != ((selectedTile >> 8) & 0xFF))
                tile32Cursor.Visibility = Visibility.Hidden;
            else
            {
                Grid.SetColumn(tile32Cursor, selectedTile & 0x7);
                Grid.SetRow(tile32Cursor, (selectedTile >> 3) & 0x1F);
                tile32Cursor.Visibility = Visibility.Visible;
            }
        }
        public void UpdateTile16SelectionUI() //update the cursor in the 16x16 tile selection area
        {
            int offset = SNES.CpuToOffset(BitConverter.ToInt32(SNES.rom, Const.Tile32DataPointersOffset[Level.BG] + Level.Id * 3));

            ushort tile16 = BitConverter.ToUInt16(SNES.rom, offset + selectedTile * 8 + (selectedInnerTile * 2));

            if (page2 != ((tile16 >> 8) & 0xFF))
                tile16Cursor.Visibility = Visibility.Hidden;
            else
            {
                Grid.SetColumn(tile16Cursor, tile16 & 0x0F);
                Grid.SetRow(tile16Cursor, (tile16 >> 4) & 0x0F);
                tile16Cursor.Visibility = Visibility.Visible;
            }
        }
        #endregion Methods

        #region Events
        private void Tile32Button_Click(object sender, RoutedEventArgs e) //For x32 Page Selection
        {
            Button b = (Button)sender;
            int i = Convert.ToInt32(b.Content.ToString().Trim(), 16);
            if (page == i)
                return;
            page = i;
            if (past != null)
            {
                //Old Color
                past.Background = Brushes.Black;
                past.Foreground = Brushes.White;
            }
            //New Color
            b.Background = Brushes.LightBlue;
            b.Foreground = Brushes.Black;
            past = b;
            UpdateTile32SelectionUI();
            ChangePageTxt();
            DrawTiles();
        }
        private void Tile16Button_Click(object sender, RoutedEventArgs e) //For x16 Page Selction
        {
            Button b = (Button)sender;
            int i = Convert.ToInt32(b.Content.ToString().Trim(), 16);
            if (page2 == i)
                return;
            page2 = i;
            if (past2 != null)
            {
                past2.Background = Brushes.Black;
                past2.Foreground = Brushes.White;
            }
            b.Background = Brushes.LightBlue;
            b.Foreground = Brushes.Black;
            past2 = b;
            UpdateTile16SelectionUI();
            Draw16xTiles();
        }
        private void tileImage_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var p = e.GetPosition(MainWindow.window.tile32E.tileImage);
            int x = (int)p.X;
            int y = (int)p.Y;
            int cX = SNES.GetSelectedTile(x, MainWindow.window.tile32E.tileImage.ActualWidth, 8);
            int cY = SNES.GetSelectedTile(y, MainWindow.window.tile32E.tileImage.ActualHeight, 32);
            int id = cX + (cY * 8);
            if ((uint)id > 0xFF)
                id = 0xFF;
            id += page * 0x100;
            if (id > (Const.Tile32Count[Level.Id,Level.BG]) - 1)
                return;
            //New Valid Tile
            selectedTile = id;

            int offset = SNES.CpuToOffset(BitConverter.ToInt32(SNES.rom, Const.Tile32DataPointersOffset[Level.BG] + Level.Id * 3));

            MainWindow.window.tile32E.topLeftInt.Value = BitConverter.ToUInt16(SNES.rom, offset + selectedTile * 8 + 0);
            MainWindow.window.tile32E.topRightInt.Value = BitConverter.ToUInt16(SNES.rom, offset + selectedTile * 8 + 2);
            MainWindow.window.tile32E.bottomLeftInt.Value = BitConverter.ToUInt16(SNES.rom, offset + selectedTile * 8 + 4);
            MainWindow.window.tile32E.bottomRightInt.Value = BitConverter.ToUInt16(SNES.rom, offset + selectedTile * 8 + 6);
            DrawTile();
            tileInt.Value = selectedTile;
            ChangePageTxt();
            UpdateTile32SelectionUI();
            UpdateTile16SelectionUI();
        }
        private void x16Image_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var p = e.GetPosition(MainWindow.window.tile32E.x16Image);
            int x = (int)p.X;
            int y = (int)p.Y;
            int cX = SNES.GetSelectedTile(x, MainWindow.window.tile32E.x16Image.ActualWidth,16);
            int cY = SNES.GetSelectedTile(y, MainWindow.window.tile32E.x16Image.ActualHeight, 16);
            int id = cX + (cY * 16);
            if ((uint)id > 0xFF)
                id = 0xFF;
            id += page2 * 0x100;
            if (id > (Const.Tile16Count[Level.Id, Level.BG]) - 1)
                return;
            //New Valid Inner Tile
            if (e.ChangedButton == MouseButton.Right)
            {
                MainWindow.window.tile16E.tileInt.Value = id; //select Tile in 16x16 Tile Editor
                return;
            }
            int offset = SNES.CpuToOffset(BitConverter.ToInt32(SNES.rom, Const.Tile32DataPointersOffset[Level.BG] + Level.Id * 3));
            BinaryPrimitives.WriteUInt16LittleEndian(SNES.rom.AsSpan(offset + selectedTile * 8 + (selectedInnerTile * 2)), (ushort)id);

            SNES.edit = true;

            if (selectedInnerTile == 0)
                MainWindow.window.tile32E.topLeftInt.Value = (ushort)id;
            else if (selectedInnerTile == 1)
                MainWindow.window.tile32E.topRightInt.Value = (ushort)id;
            else if (selectedInnerTile == 2)
                MainWindow.window.tile32E.bottomLeftInt.Value = (ushort)id;
            else if (selectedInnerTile == 3)
                MainWindow.window.tile32E.bottomRightInt.Value = (ushort)id;
            DrawTile();
            UpdateTile16SelectionUI();

            MainWindow.window.layoutE.DrawLayout();
            MainWindow.window.layoutE.DrawScreen();
            MainWindow.window.enemyE.DrawLayout();
            MainWindow.window.screenE.DrawScreen();
            MainWindow.window.screenE.DrawTiles();
            MainWindow.window.screenE.DrawTile();
        }
        private void TileGridBtn_Click(object sender, RoutedEventArgs e)
        {
            if (MainWindow.window.tile32E.tileGrid.ShowGridLines)
                MainWindow.window.tile32E.tileGrid.ShowGridLines = false;
            else
                MainWindow.window.tile32E.tileGrid.ShowGridLines = true;
        }
        private void x16GridBtn_Click(object sender, RoutedEventArgs e)
        {
            if (MainWindow.window.tile32E.x16grid.ShowGridLines)
                MainWindow.window.tile32E.x16grid.ShowGridLines = false;
            else
                MainWindow.window.tile32E.x16grid.ShowGridLines = true;
        }
        private void tileInt_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue == null || SNES.rom == null)
                return;
            if (selectedTile == (int)e.NewValue)
                return;

            selectedTile = (int)e.NewValue;

            int offset = SNES.CpuToOffset(BitConverter.ToInt32(SNES.rom, Const.Tile32DataPointersOffset[Level.BG] + Level.Id * 3));

            MainWindow.window.tile32E.topLeftInt.Value = BitConverter.ToUInt16(SNES.rom, offset + selectedTile * 8 + 0);
            MainWindow.window.tile32E.topRightInt.Value = BitConverter.ToUInt16(SNES.rom, offset + selectedTile * 8 + 2);
            MainWindow.window.tile32E.bottomLeftInt.Value = BitConverter.ToUInt16(SNES.rom, offset + selectedTile * 8 + 4);
            MainWindow.window.tile32E.bottomRightInt.Value = BitConverter.ToUInt16(SNES.rom, offset + selectedTile * 8 + 6);
            DrawTile();
            ChangePageTxt();
            UpdateTile32SelectionUI();
            UpdateTile16SelectionUI();
        }
        private void topLeftInt_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue == null || SNES.rom == null)
                return;
            int offset = SNES.CpuToOffset(BitConverter.ToInt32(SNES.rom, Const.Tile32DataPointersOffset[Level.BG] + Level.Id * 3));
            if ((ushort)(int)e.NewValue == BitConverter.ToUInt16(SNES.rom, offset + selectedTile * 8 + 0))
                return;

            BinaryPrimitives.WriteUInt16LittleEndian(SNES.rom.AsSpan(offset + selectedTile * 8 + 0), (ushort)(int)e.NewValue);
            SNES.edit = true;
            DrawTile();
            UpdateTile16SelectionUI();

            MainWindow.window.layoutE.DrawLayout();
            MainWindow.window.layoutE.DrawScreen();
            MainWindow.window.enemyE.DrawLayout();
            MainWindow.window.screenE.DrawScreen();
            MainWindow.window.screenE.DrawTiles();
            MainWindow.window.screenE.DrawTile();
        }
        private void topRightInt_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue == null || SNES.rom == null)
                return;
            int offset = SNES.CpuToOffset(BitConverter.ToInt32(SNES.rom, Const.Tile32DataPointersOffset[Level.BG] + Level.Id * 3));
            if ((ushort)(int)e.NewValue == BitConverter.ToUInt16(SNES.rom, offset + selectedTile * 8 + 2))
                return;

            BinaryPrimitives.WriteUInt16LittleEndian(SNES.rom.AsSpan(offset + selectedTile * 8 + 2), (ushort)(int)e.NewValue);
            SNES.edit = true;
            DrawTile();
            UpdateTile16SelectionUI();

            MainWindow.window.layoutE.DrawLayout();
            MainWindow.window.layoutE.DrawScreen();
            MainWindow.window.enemyE.DrawLayout();
            MainWindow.window.screenE.DrawScreen();
            MainWindow.window.screenE.DrawTiles();
            MainWindow.window.screenE.DrawTile();
        }
        private void bottomLeftInt_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue == null || SNES.rom == null)
                return;
            int offset = SNES.CpuToOffset(BitConverter.ToInt32(SNES.rom, Const.Tile32DataPointersOffset[Level.BG] + Level.Id * 3));
            if ((ushort)(int)e.NewValue == BitConverter.ToUInt16(SNES.rom, offset + selectedTile * 8 + 4))
                return;

            BinaryPrimitives.WriteUInt16LittleEndian(SNES.rom.AsSpan(offset + selectedTile * 8 + 4), (ushort)(int)e.NewValue);
            SNES.edit = true;
            DrawTile();
            UpdateTile16SelectionUI();

            MainWindow.window.layoutE.DrawLayout();
            MainWindow.window.layoutE.DrawScreen();
            MainWindow.window.enemyE.DrawLayout();
            MainWindow.window.screenE.DrawScreen();
            MainWindow.window.screenE.DrawTiles();
            MainWindow.window.screenE.DrawTile();
        }
        private void bottomRightInt_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue == null || SNES.rom == null)
                return;
            int offset = SNES.CpuToOffset(BitConverter.ToInt32(SNES.rom, Const.Tile32DataPointersOffset[Level.BG] + Level.Id * 3));
            if ((ushort)(int)e.NewValue == BitConverter.ToUInt16(SNES.rom, offset + selectedTile * 8 + 6))
                return;

            BinaryPrimitives.WriteUInt16LittleEndian(SNES.rom.AsSpan(offset + selectedTile * 8 + 6), (ushort)(int)e.NewValue);
            SNES.edit = true;
            DrawTile();
            UpdateTile16SelectionUI();

            MainWindow.window.layoutE.DrawLayout();
            MainWindow.window.layoutE.DrawScreen();
            MainWindow.window.enemyE.DrawLayout();
            MainWindow.window.screenE.DrawScreen();
            MainWindow.window.screenE.DrawTiles();
            MainWindow.window.screenE.DrawTile();
        }
        private void tileImageS_MouseUp(object sender, MouseButtonEventArgs e) //select 16x16 tile within the selected 32x32 tile
        {
            var p = e.GetPosition(MainWindow.window.tile32E.tileImageS);
            int x = (int)p.X;
            int y = (int)p.Y;
            int cX = SNES.GetSelectedTile(x, MainWindow.window.tile32E.tileImageS.ActualWidth, 2);
            int cY = SNES.GetSelectedTile(y, MainWindow.window.tile32E.tileImageS.ActualHeight, 2);

            if (e.ChangedButton == MouseButton.Right) //TODO: make it select the 16x16 tile into the 16x16 tile editor
            {

            }
            else
            {
                selectedInnerTile = cX + (cY * 2);
                Grid.SetColumn(innerCursor, cX);
                Grid.SetRow(innerCursor, cY);
                UpdateTile16SelectionUI();
            }
        }
        #endregion Events
    }
}
