using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace TeheManX_Editor.Forms
{
    /// <summary>
    /// Interaction logic for ScreenEditor.xaml
    /// </summary>
    public partial class ScreenEditor : UserControl
    {
        #region Properties
        public bool updateScreen;
        public bool updateTiles;
        public bool updateTile;
        WriteableBitmap screenBMP = new WriteableBitmap(256, 256, 96, 96, PixelFormats.Bgra32, null); //for both modes
        WriteableBitmap tileBMP = new WriteableBitmap(256, 1024, 96, 96, PixelFormats.Bgra32, null);
        WriteableBitmap tileBMP_S = new WriteableBitmap(32, 32, 96, 96, PixelFormats.Bgra32, null);
        Button past;
        bool screenDown = false; //used in both modes
        public int page = 0;
        public int selectedTile = 0; //32x
        public int screenId = 1; //used in both modes

        /*16x16 Mode Properties*/
        public bool mode16 = false; //is 16x16 mode active
        public byte[] screenData16 = null; //holds the screen data for 16x16 mode
        HashSet<ulong> tiles32 = new HashSet<ulong>(); //the 32x32 tiles that are based off the data in screenData16
        int pastTiles32Count = -1;
        WriteableBitmap tileBMP16 = new WriteableBitmap(256, 256, 96, 96, PixelFormats.Bgra32, null);
        WriteableBitmap tileBMP_S16 = new WriteableBitmap(16, 16, 96, 96, PixelFormats.Bgra32, null);
        Button past16;
        public int page16 = 0;
        public int screenSelect16 = -1;
        public int startCol16 = 0;
        public int startRow16 = 0;
        public int selectedTile16 = 0; //16x
        bool tilesDown16 = false;
        #endregion Properties

        #region Constructors
        public ScreenEditor()
        {
            InitializeComponent();

            screenImage.Source = screenBMP;
            tileImage.Source = tileBMP;
            tileImageS.Source = tileBMP_S;

            screenImage16.Source = screenBMP;
            tileImage16.Source = tileBMP16;
            tileImageS16.Source = tileBMP_S16;
        }
        #endregion Constructors

        #region Methods
        public void AssignLimits()
        {
            int screenAmount = Const.ScreenCount[Level.Id, Level.BG] - 1;
            MainWindow.window.screenE.screenInt.Maximum = screenAmount;
            if (MainWindow.window.screenE.screenInt.Value > screenAmount)
                MainWindow.window.screenE.screenInt.Value = screenAmount;

            int tile32Amount = Const.Tile32Count[Level.Id, Level.BG] - 1;
            MainWindow.window.screenE.tile32Int.Maximum = tile32Amount;
            if (selectedTile > tile32Amount)
                MainWindow.window.screenE.tile32Int.Value = tile32Amount;

            DrawScreen();
            DrawTiles();
            DrawTile();
        }
        public void PaintScreen()
        {
            updateScreen = false;
            if (mode16)
            {
                screenBMP.Lock();
                for (int y = 0; y < 16; y++)
                {
                    for (int x = 0; x < 16; x++)
                    {
                        ushort id = BitConverter.ToUInt16(screenData16, x * 2 + y * 32 + screenId * 0x200);
                        Level.Draw16xTile(id, x * 16, y * 16, screenBMP.BackBufferStride, screenBMP.BackBuffer);
                    }
                }
                screenBMP.AddDirtyRect(new Int32Rect(0, 0, 256, 256));
                screenBMP.Unlock();
            }
            else
            {
                screenBMP.Lock();
                Level.DrawScreen(screenId, screenBMP.BackBufferStride, screenBMP.BackBuffer);
                screenBMP.AddDirtyRect(new Int32Rect(0, 0, 256, 256));
                screenBMP.Unlock();
            }
        }
        public void PaintTiles()
        {
            updateTiles = false;
            int Id;
            if (Const.Id == Const.GameId.MegaManX3 && Level.Id == 0xE) Id = 0x10; //special case for MMX3 rekt version of dophler 2
            else if (Const.Id == Const.GameId.MegaManX3 && Level.Id > 0xE) Id = (Level.Id - 0xF) + 0xE; //Buffalo or Beetle
            else Id = Level.Id;

            int tile32Offset = SNES.CpuToOffset(BinaryPrimitives.ReadInt32LittleEndian(SNES.rom.AsSpan(Const.Tile32DataPointersOffset[Level.BG] + Id * 3)));
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
                                    buffer[(x * 32 + c) * 4 + (y * 32 + r) * tileBMP.BackBufferStride + 0] = 0;
                                    buffer[(x * 32 + c) * 4 + (y * 32 + r) * tileBMP.BackBufferStride + 1] = 0;
                                    buffer[(x * 32 + c) * 4 + (y * 32 + r) * tileBMP.BackBufferStride + 2] = 0;
                                    buffer[(x * 32 + c) * 4 + (y * 32 + r) * tileBMP.BackBufferStride + 3] = 0xFF;
                                }
                            }
                        }
                        continue;
                    }
                    Level.Draw16xTile(BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(tile32Offset + (tileId32 * 8))), x * 32, y * 32, tileBMP.BackBufferStride, tileBMP.BackBuffer);
                    Level.Draw16xTile(BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(tile32Offset + (tileId32 * 8) + 2)), x * 32 + 16, y * 32, tileBMP.BackBufferStride, tileBMP.BackBuffer);
                    Level.Draw16xTile(BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(tile32Offset + (tileId32 * 8) + 4)), x * 32, y * 32 + 16, tileBMP.BackBufferStride, tileBMP.BackBuffer);
                    Level.Draw16xTile(BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(tile32Offset + (tileId32 * 8) + 6)), x * 32 + 16, y * 32 + 16, tileBMP.BackBufferStride, tileBMP.BackBuffer);
                }
            }
            tileBMP.AddDirtyRect(new Int32Rect(0, 0, 256, 1024));
            tileBMP.Unlock();
        }
        public void PaintTiles16()
        {
            updateTiles = false;
            tileBMP16.Lock();
            for (int y = 0; y < 16; y++)
            {
                for (int x = 0; x < 16; x++)
                {
                    int id = x + (y * 16) + (page16 * 0x100);
                    if (id > (Const.Tile16Count[Level.Id, Level.BG] - 1))
                    {
                        unsafe
                        {
                            byte* buffer = (byte*)tileBMP16.BackBuffer;
                            for (int r = 0; r < 16; r++)
                            {
                                for (int c = 0; c < 16; c++)
                                {
                                    buffer[(x * 16 + c) * 4 + (y * 16 + r) * tileBMP16.BackBufferStride + 0] = 0;
                                    buffer[(x * 16 + c) * 4 + (y * 16 + r) * tileBMP16.BackBufferStride + 1] = 0;
                                    buffer[(x * 16 + c) * 4 + (y * 16 + r) * tileBMP16.BackBufferStride + 2] = 0;
                                    buffer[(x * 16 + c) * 4 + (y * 16 + r) * tileBMP16.BackBufferStride + 3] = 0xFF;
                                }
                            }
                        }
                        continue;
                    }
                    Level.Draw16xTile(id, x * 16, y * 16, tileBMP16.BackBufferStride, tileBMP16.BackBuffer);
                }
            }
            tileBMP16.AddDirtyRect(new Int32Rect(0, 0, 256, 256));
            tileBMP16.Unlock();
        }
        public void PaintTile()
        {
            updateTile = false;
            tileBMP_S.Lock();

            int Id;
            if (Const.Id == Const.GameId.MegaManX3 && Level.Id == 0xE) Id = 0x10; //special case for MMX3 rekt version of dophler 2
            else if (Const.Id == Const.GameId.MegaManX3 && Level.Id > 0xE) Id = (Level.Id - 0xF) + 0xE; //Buffalo or Beetle
            else Id = Level.Id;

            int tile32Offset = SNES.CpuToOffset(BinaryPrimitives.ReadInt32LittleEndian(SNES.rom.AsSpan(Const.Tile32DataPointersOffset[Level.BG] + Id * 3)));
            Level.Draw16xTile(BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(tile32Offset + (selectedTile * 8))), 0, 0, tileBMP_S.BackBufferStride, tileBMP_S.BackBuffer);
            Level.Draw16xTile(BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(tile32Offset + (selectedTile * 8) + 2)), 16, 0, tileBMP_S.BackBufferStride, tileBMP_S.BackBuffer);
            Level.Draw16xTile(BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(tile32Offset + (selectedTile * 8) + 4)), 0, 16, tileBMP_S.BackBufferStride, tileBMP_S.BackBuffer);
            Level.Draw16xTile(BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(tile32Offset + (selectedTile * 8) + 6)), 16, 16, tileBMP_S.BackBufferStride, tileBMP_S.BackBuffer);
            tileBMP_S.AddDirtyRect(new Int32Rect(0, 0, 32, 32));
            tileBMP_S.Unlock();
        }
        public void PaintTile16()
        {
            updateTile = false;
            tileBMP_S16.Lock();
            Level.Draw16xTile(selectedTile16, 0, 0, tileBMP_S16.BackBufferStride, tileBMP_S16.BackBuffer);
            tileBMP_S16.AddDirtyRect(new Int32Rect(0, 0, 16, 16));
            tileBMP_S16.Unlock();
        }
        public void DrawScreen()
        {
            updateScreen = true;
        }
        public void DrawTiles()
        {
            updateTiles = true;
        }
        public void DrawTile()
        {
            updateTile = true;
        }
        private void ChangePageTxt()
        {
            //pageBtn.Content = Convert.ToString(page).PadRight(3, '0') + "-" + Convert.ToString(page).PadRight(3, 'F');
        }
        public void UpdateTile32SelectionUI()
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
        public void DeleteScreen()
        {
            if (mode16)
            {
                Array.Clear(screenData16, screenId * 0x200, 0x200);
                DrawScreen16();
            }
            else
            {
                int Id;
                if (Const.Id == Const.GameId.MegaManX3 && Level.Id == 0xE) Id = 0x10; //special case for MMX3 rekt version of dophler 2
                else if (Const.Id == Const.GameId.MegaManX3 && Level.Id > 0xE) Id = (Level.Id - 0xF) + 0xE; //Buffalo or Beetle
                else Id = Level.Id;

                int offset = SNES.CpuToOffset(BinaryPrimitives.ReadInt32LittleEndian(SNES.rom.AsSpan(Const.ScreenDataPointersOffset[Level.BG] + Id * 3))) + screenId * 0x80;
                Array.Clear(SNES.rom, offset, 0x80);
                DrawScreen();

            }
            SNES.edit = true;
            MainWindow.window.layoutE.DrawLayout();
            MainWindow.window.layoutE.DrawScreen();
            MainWindow.window.enemyE.DrawLayout();
        }
        /*
        *  Mode 16x16 GUI Methods
        */
        public void DrawScreen16()
        {
            updateScreen = true;
        }
        private void DrawTiles16()
        {
            updateTiles = true;
        }
        private void DrawTile16()
        {
            updateTile = true;
        }
        private void UpdateCursor16() //witch tile is selected in 16x16 mode
        {
            Grid.SetColumnSpan(cursor16, 1);
            Grid.SetRowSpan(cursor16, 1);
            if (page16 == selectedTile16 >> 8)
            {
                cursor16.Visibility = Visibility.Visible;
                Grid.SetColumn(cursor16, selectedTile16 & 0xF);
                Grid.SetRow(cursor16, (selectedTile16 & 0xF0) >> 4);
            }
            else
                cursor16.Visibility = Visibility.Hidden;
        }
        public void UpdateScreenCursor()
        {
            if (screenSelect16 == -1) return;

            if (screenSelect16 == screenId)
                screenCursor16.Visibility = Visibility.Visible;
            else
                screenCursor16.Visibility = Visibility.Hidden;
        }
        public void ResetScreenCursor16()
        {
            Grid.SetColumnSpan(screenCursor16, 1);
            Grid.SetRowSpan(screenCursor16, 1);
            screenCursor16.Visibility = Visibility.Hidden;
        }
        public void Update32x32TileList(bool allScreen = false) //Get the 16x16 Tile Screen Data and Create a list of 32x32 Tiles
        {
            tiles32.Clear();

            byte[] data = screenData16;
            int screens = Const.ScreenCount[Level.Id, Level.BG];

            for (int screen = 0; screen < screens; screen++)
            {
                int screenBase = screen * 0x200;

                for (int y = 0; y < 8; y++)
                {
                    int rowBase = screenBase + y * 64;

                    for (int x = 0; x < 8; x++)
                    {
                        int baseOffset = rowBase + x * 4;

                        ushort TL = (ushort)(data[baseOffset + 0] | (data[baseOffset + 1] << 8));
                        ushort TR = (ushort)(data[baseOffset + 2] | (data[baseOffset + 3] << 8));
                        ushort BL = (ushort)(data[baseOffset + 32] | (data[baseOffset + 33] << 8));
                        ushort BR = (ushort)(data[baseOffset + 34] | (data[baseOffset + 35] << 8));

                        ulong key = ((ulong)TL)
                                  | ((ulong)TR << 16)
                                  | ((ulong)BL << 32)
                                  | ((ulong)BR << 48);

                        tiles32.Add(key);
                    }
                }
            }
        }
        public void Update32x32TileCountText()
        {
            if (tiles32.Count == pastTiles32Count) return;
            tile32CountTextBlock.Text = $"32x32 Tile Count:{tiles32.Count:X3}";
            pastTiles32Count = tiles32.Count;
        }
        #endregion Methods

        #region Events
        private void screenInt_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (screenId == (int)e.NewValue || SNES.rom == null)
                return;
            screenId = (int)e.NewValue;
            DrawScreen();
        }
        private void Tile32PageButton_Click(object sender, RoutedEventArgs e)
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
            ChangePageTxt();
            UpdateTile32SelectionUI();
            DrawTiles();
        }
        private void screenImage_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var p = e.GetPosition(MainWindow.window.screenE.screenImage);
            int x = (int)p.X;
            int y = (int)p.Y;
            int cX = SNES.GetSelectedTile(x, MainWindow.window.screenE.screenImage.ActualWidth, 8);
            int cY = SNES.GetSelectedTile(y, MainWindow.window.screenE.screenImage.ActualHeight, 8);

            int Id;
            if (Const.Id == Const.GameId.MegaManX3 && Level.Id == 0xE) Id = 0x10; //special case for MMX3 rekt version of dophler 2
            else if (Const.Id == Const.GameId.MegaManX3 && Level.Id > 0xE) Id = (Level.Id - 0xF) + 0xE; //Buffalo or Beetle
            else Id = Level.Id;

            if (e.ChangedButton == MouseButton.Right)
            {
                int offset = SNES.CpuToOffset(BitConverter.ToInt32(SNES.rom, Const.ScreenDataPointersOffset[Level.BG] + Id * 3));
                selectedTile = BitConverter.ToUInt16(SNES.rom, offset + (screenId * 0x80) + (cX * 2) + (cY * 16));
                tile32Int.Value = selectedTile;
                DrawTile();
                UpdateTile32SelectionUI();
            }
            else
            {
                int offset = SNES.CpuToOffset(BitConverter.ToInt32(SNES.rom, Const.ScreenDataPointersOffset[Level.BG] + Id * 3));
                screenDown = true;
                ushort tileId = BitConverter.ToUInt16(SNES.rom, offset + screenId * 0x80 + cX * 2 + cY * 16);
                if (tileId == selectedTile)
                    return;

                if (MainWindow.undos.Count == Const.MaxUndo)
                    MainWindow.undos.RemoveAt(0);
                MainWindow.undos.Add(Undo.CreateScreenUndo((byte)screenId, (byte)cX, (byte)cY));

                BinaryPrimitives.WriteUInt16LittleEndian(SNES.rom.AsSpan(offset + screenId * 0x80 + cX * 2 + cY * 16), (ushort)selectedTile);

                SNES.edit = true;
                DrawScreen();

                MainWindow.window.layoutE.DrawLayout();
                MainWindow.window.layoutE.DrawScreen();
                MainWindow.window.enemyE.DrawLayout();
            }
        }
        private void screenImage_MouseMove(object sender, MouseEventArgs e)
        {
            Point p = e.GetPosition(screenImage);
            if (p.X > MainWindow.window.screenE.screenImage.ActualWidth || p.X < 0) return;
            if (p.Y > MainWindow.window.screenE.screenImage.ActualHeight || p.Y < 0) return;
            if (e.LeftButton == MouseButtonState.Pressed && screenDown)
            {
                HitTestResult result = VisualTreeHelper.HitTest(MainWindow.window.screenE.screenImage, p);

                if (result != null)
                {
                    //Get Cords
                    int x = (int)p.X;
                    int y = (int)p.Y;
                    int cX = SNES.GetSelectedTile(x, screenImage.ActualWidth, 8);
                    int cY = SNES.GetSelectedTile(y, screenImage.ActualHeight, 8);
                    int cord = (cX * 2) + (cY * 16);

                    int Id;
                    if (Const.Id == Const.GameId.MegaManX3 && Level.Id == 0xE) Id = 0x10; //special case for MMX3 rekt version of dophler 2
                    else if (Const.Id == Const.GameId.MegaManX3 && Level.Id > 0xE) Id = (Level.Id - 0xF) + 0xE; //Buffalo or Beetle
                    else Id = Level.Id;

                    int offset = SNES.CpuToOffset(BinaryPrimitives.ReadInt32LittleEndian(SNES.rom.AsSpan(Const.ScreenDataPointersOffset[Level.BG] + Id * 3))) + screenId * 0x80;

                    ushort tileId = BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(offset + cord));
                    if (tileId == selectedTile)
                        return;

                    if (MainWindow.undos.Count == Const.MaxUndo)
                        MainWindow.undos.RemoveAt(0);
                    MainWindow.undos.Add(Undo.CreateScreenUndo((byte)screenId, (byte)cX, (byte)cY));

                    BinaryPrimitives.WriteUInt16LittleEndian(SNES.rom.AsSpan(offset + cord), (ushort)selectedTile);
                    SNES.edit = true;
                    DrawScreen();

                    MainWindow.window.layoutE.DrawLayout();
                    MainWindow.window.layoutE.DrawScreen();
                    MainWindow.window.enemyE.DrawLayout();
                }
            }
        }
        private void screenImage_MouseUp(object sender, MouseButtonEventArgs e)
        {
            screenDown = false;
        }
        private void ScreenGridBtn_Click(object sender, RoutedEventArgs e)
        {
            if (MainWindow.window.screenE.screenGrid.ShowGridLines)
                MainWindow.window.screenE.screenGrid.ShowGridLines = false;
            else
                MainWindow.window.screenE.screenGrid.ShowGridLines = true;
        }
        private void TileGridBtn_Click(object sender, RoutedEventArgs e)
        {
            if (MainWindow.window.screenE.tileGrid.ShowGridLines)
                MainWindow.window.screenE.tileGrid.ShowGridLines = false;
            else
                MainWindow.window.screenE.tileGrid.ShowGridLines = true;
        }
        private void tileImage_MouseUp(object sender, MouseButtonEventArgs e)
        {
            var p = e.GetPosition(MainWindow.window.screenE.tileImage);
            int x = (int)p.X;
            int y = (int)p.Y;
            int cX = SNES.GetSelectedTile(x, MainWindow.window.screenE.tileImage.ActualWidth, 8);
            int cY = SNES.GetSelectedTile(y, MainWindow.window.screenE.tileImage.ActualHeight, 32);
            int id = cX + (cY * 8);
            if ((uint)id > 0xFF)
                id = 0xFF;
            id += page * 0x100;
            if (id > (Const.Tile32Count[Level.Id,Level.BG]) - 1)
                return;
            //New Valid Tile
            if (e.ChangedButton == MouseButton.Right)
            {
                MainWindow.window.tile32E.tileInt.Value = id; //select Tile in 32x32 Tile Editor
                return;
            }
            selectedTile = id;
            tile32Int.Value = selectedTile;
            UpdateTile32SelectionUI();
            DrawTile();
        }
        private void tile32Int_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue == null || SNES.rom == null)
                return;
            if (selectedTile == (int)e.NewValue)
                return;
            selectedTile = (int)e.NewValue;
            UpdateTile32SelectionUI();
            DrawTile();
        }
        private void tileImage_MouseWheel(object sender, MouseWheelEventArgs e)
        {
        }
        private void Mode16x16Button_Click(object sender, RoutedEventArgs e)
        {
            int newSize = Const.ScreenCount[Level.Id, Level.BG] * 0x200;

            if (screenData16 == null || screenData16.Length != newSize)
                screenData16 = GC.AllocateUninitializedArray<byte>(newSize, pinned: false);

            int Id;
            if (Const.Id == Const.GameId.MegaManX3 && Level.Id == 0xE) Id = 0x10; //special case for MMX3 rekt version of dophler 2
            else if (Const.Id == Const.GameId.MegaManX3 && Level.Id > 0xE) Id = (Level.Id - 0xF) + 0xE; //Buffalo or Beetle
            else Id = Level.Id;

            int screenDataBaseOffset = SNES.CpuToOffset(BinaryPrimitives.ReadInt32LittleEndian(SNES.rom.AsSpan(Const.ScreenDataPointersOffset[Level.BG] + Id * 3)));
            int tile32BaseOffset = SNES.CpuToOffset(BinaryPrimitives.ReadInt32LittleEndian(SNES.rom.AsSpan(Const.Tile32DataPointersOffset[Level.BG] + Id * 3)));

            for (int screen = 0; screen < Const.ScreenCount[Level.Id, Level.BG]; screen++)
            {
                for (int y = 0; y < 8; y++)
                {
                    for (int x = 0; x < 8; x++)
                    {
                        ushort tile32Id = BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(screenDataBaseOffset + screen * 0x80 + x * 2 + y * 16));

                        int srcBaseOffset = tile32BaseOffset + tile32Id * 8;
                        int dstBaseOffset = screen * 0x200;
                        //Write TL,TR,BL,BR 16x16 Tiles
                        BinaryPrimitives.WriteUInt16LittleEndian(screenData16.AsSpan(dstBaseOffset + ((x * 2) + 0 + y * 32 + 0) * 2, 2), BitConverter.ToUInt16(SNES.rom, srcBaseOffset + 0));
                        BinaryPrimitives.WriteUInt16LittleEndian(screenData16.AsSpan(dstBaseOffset + ((x * 2) + 1 + y * 32 + 0) * 2, 2), BitConverter.ToUInt16(SNES.rom, srcBaseOffset + 2));
                        BinaryPrimitives.WriteUInt16LittleEndian(screenData16.AsSpan(dstBaseOffset + ((x * 2) + 0 + y * 32 + 16) * 2, 2), BitConverter.ToUInt16(SNES.rom, srcBaseOffset + 4));
                        BinaryPrimitives.WriteUInt16LittleEndian(screenData16.AsSpan(dstBaseOffset + ((x * 2) + 1 + y * 32 + 16) * 2, 2), BitConverter.ToUInt16(SNES.rom, srcBaseOffset + 6));

                    }
                }
            }

            //Clear Screen Undos of 32x32 Mode
            if (MainWindow.undos.Count != 0)
            {
                for (int i = (MainWindow.undos.Count - 1); i != -1; i--)
                {
                    if (MainWindow.undos[i].type == TeheManX_Editor.Undo.UndoType.Screen)
                        MainWindow.undos.RemoveAt(i);
                }
            }

            tiles32.Clear();
            Update32x32TileList(true);
            Update32x32TileCountText();

            //Update 16x16 Mode UI before swapping Modes
            DrawTiles16();
            screenInt16.Value = screenId;
            screenInt16.Maximum = Const.ScreenCount[Level.Id, Level.BG] - 1;

            tile16Int.Maximum = Const.Tile16Count[Level.Id, Level.BG] - 1;
            DrawTile16();
            ResetScreenCursor16();

            tile32ModeGrid.Visibility = Visibility.Collapsed;
            tile16ModeGrid.Visibility = Visibility.Visible;
            mode16 = true;
        }
        /*
         *  Mode 16x16 GUI Events
         */
        private void screenImage16_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var p = e.GetPosition(MainWindow.window.screenE.screenImage16);
            int x = (int)p.X;
            int y = (int)p.Y;
            int cX = SNES.GetSelectedTile(x, MainWindow.window.screenE.screenImage16.ActualWidth, 16);
            int cY = SNES.GetSelectedTile(y, MainWindow.window.screenE.screenImage16.ActualHeight, 16);
            int cord = (cX * 2) + (cY * 16 * 2);
            if (e.ChangedButton == MouseButton.Right) // Copy
            {
                if (Keyboard.IsKeyDown(Key.LeftShift)) // Multi-Select
                {
                    if (screenCursor16.Visibility != Visibility.Visible)
                    {
                        Grid.SetColumnSpan(screenCursor16, 1);
                        Grid.SetRowSpan(screenCursor16, 1);
                        Grid.SetColumn(screenCursor16, cX);
                        Grid.SetRow(screenCursor16, cY);
                        startCol16 = cX;
                        startRow16 = cY;
                        screenCursor16.Visibility = Visibility.Visible;
                        screenSelect16 = screenId;
                    }
                    else
                    {
                        if (cX > startCol16)
                        {
                            Grid.SetColumn(screenCursor16, startCol16);
                            Grid.SetColumnSpan(screenCursor16, cX - startCol16 + 1);
                        }
                        else if (cX < startCol16)
                        {
                            Grid.SetColumn(screenCursor16, cX);
                            Grid.SetColumnSpan(screenCursor16, startCol16 - cX + 1);
                        }
                        else
                        {
                            Grid.SetColumn(screenCursor16, startCol16);
                            Grid.SetColumnSpan(screenCursor16, 1);
                        }

                        if (cY > startRow16)
                        {
                            Grid.SetRow(screenCursor16, startRow16);
                            Grid.SetRowSpan(screenCursor16, cY - startRow16 + 1);
                        }
                        else if (cY < startRow16)
                        {
                            Grid.SetRow(screenCursor16, cY);
                            Grid.SetRowSpan(screenCursor16, startRow16 - cY + 1);
                        }
                        else
                        {
                            Grid.SetRow(screenCursor16, startRow16);
                            Grid.SetRowSpan(screenCursor16, 1);
                        }
                    }
                }
                else
                {
                    selectedTile16 = BitConverter.ToUInt16(screenData16, cord + screenId * 0x200);
                    tile16Int.Value = selectedTile16;
                    ResetScreenCursor16();
                    UpdateCursor16();
                    DrawTile16();
                }
            }
            else // Paste
            {
                if (Keyboard.IsKeyDown(Key.LeftShift))
                {
                    if (Grid.GetColumnSpan(screenCursor16) != 1 || Grid.GetRowSpan(screenCursor16) != 1) //Paste From other Screen
                    {
                        if (MainWindow.undos.Count == Const.MaxUndo)
                            MainWindow.undos.RemoveAt(0);
                        MainWindow.undos.Add(Undo.CreateGroupScreenUndo16((byte)screenId, (byte)cX, (byte)cY, (byte)Grid.GetColumnSpan(screenCursor16), (byte)Grid.GetRowSpan(screenCursor16)));
                        for (int r = 0; r < Grid.GetRowSpan(screenCursor16); r++)
                        {
                            for (int c = 0; c < Grid.GetColumnSpan(screenCursor16); c++)
                            {
                                if (cX + c > 15)
                                    continue;
                                if (cY + r > 15)
                                    continue;
                                int dest = cord + c * 2 + r * 32 + (screenId * 0x200);

                                int srcCol = Grid.GetColumn(screenCursor16) + c;
                                int srcRow = Grid.GetRow(screenCursor16) + r;
                                ushort val = BinaryPrimitives.ReadUInt16LittleEndian(screenData16.AsSpan(screenSelect16 * 0x200 + srcCol * 2 + srcRow * 32));
                                BinaryPrimitives.WriteUInt16LittleEndian(screenData16.AsSpan(dest), val);
                            }
                        }
                        //End of Loops
                        SNES.edit = true;
                        Update32x32TileList();
                        Update32x32TileCountText();
                        MainWindow.window.layoutE.DrawScreen();
                        MainWindow.window.layoutE.DrawLayout();
                        MainWindow.window.enemyE.DrawLayout();
                        DrawScreen16();
                    }
                    return;
                }

                //Tile Paste
                screenDown = true;

                if (Grid.GetColumnSpan(cursor16) != 1 || Grid.GetRowSpan(cursor16) != 1) //Multi-Select
                {
                    int tileAmount = Const.Tile16Count[Level.Id, Level.BG] - 1;
                    int rowSrc = Grid.GetRow(cursor16);
                    int colSrc = Grid.GetColumn(cursor16);

                    if (MainWindow.undos.Count == Const.MaxUndo)
                        MainWindow.undos.RemoveAt(0);
                    MainWindow.undos.Add(Undo.CreateGroupScreenUndo16((byte)screenId, (byte)cX, (byte)cY, (byte)Grid.GetColumnSpan(cursor16), (byte)Grid.GetRowSpan(cursor16)));

                    for (int r = 0; r < Grid.GetRowSpan(cursor16); r++)
                    {
                        for (int c = 0; c < Grid.GetColumnSpan(cursor16); c++)
                        {
                            if (cX + c > 15)
                                continue;
                            if (cY + r > 15)
                                continue;
                            int id = c + colSrc + (page16 << 8) + (r + rowSrc) * 16;

                            if (id > tileAmount)
                                id = 0;
                            int offset = cord + c * 2 + r * 32 + (screenId * 0x200);
                            BinaryPrimitives.WriteUInt16LittleEndian(screenData16.AsSpan(offset), (ushort)id);
                        }
                    }
                    //End of Loops
                    SNES.edit = true;
                    Update32x32TileList();
                    Update32x32TileCountText();
                    MainWindow.window.layoutE.DrawScreen();
                    MainWindow.window.layoutE.DrawLayout();
                    MainWindow.window.enemyE.DrawLayout();
                    DrawScreen16();
                    return;
                }
                //Normal Paste
                if (MainWindow.undos.Count == Const.MaxUndo)
                    MainWindow.undos.RemoveAt(0);
                MainWindow.undos.Add(Undo.CreateScreenUndo16((byte)screenId, (byte)cX, (byte)cY));
                BinaryPrimitives.WriteUInt16LittleEndian(screenData16.AsSpan(cord + screenId * 0x200), (ushort)selectedTile16);
                SNES.edit = true;
                Update32x32TileList();
                Update32x32TileCountText();
                MainWindow.window.layoutE.DrawScreen();
                MainWindow.window.layoutE.DrawLayout();
                MainWindow.window.enemyE.DrawLayout();
                DrawScreen16();
            }
        }
        private void screenImage16_MouseMove(object sender, MouseEventArgs e)
        {
            Point p = e.GetPosition(screenImage16);
            if (p.X > MainWindow.window.screenE.screenImage16.ActualWidth || p.X < 0) return;
            if (p.Y > MainWindow.window.screenE.screenImage16.ActualHeight || p.Y < 0) return;
            if (e.LeftButton == MouseButtonState.Pressed && screenDown)
            {
                HitTestResult result = VisualTreeHelper.HitTest(MainWindow.window.screenE.screenImage16, p);

                if (result != null)
                {
                    //Get Cords
                    int x = (int)p.X;
                    int y = (int)p.Y;
                    int cX = SNES.GetSelectedTile(x, screenImage16.ActualWidth, 16);
                    int cY = SNES.GetSelectedTile(y, screenImage16.ActualHeight, 16);
                    int cord = (cX * 2) + (cY * 32);

                    if (BinaryPrimitives.ReadUInt16LittleEndian(screenData16.AsSpan(screenId * 0x200 + cord)) == (ushort)selectedTile16)
                        return;

                    if (MainWindow.undos.Count == Const.MaxUndo)
                        MainWindow.undos.RemoveAt(0);
                    MainWindow.undos.Add(Undo.CreateScreenUndo16((byte)screenId, (byte)cX, (byte)cY));
                    BinaryPrimitives.WriteUInt16LittleEndian(screenData16.AsSpan(screenId * 0x200 + cord), (ushort)selectedTile16);
                    SNES.edit = true;
                    Update32x32TileList();
                    Update32x32TileCountText();
                    MainWindow.window.layoutE.DrawScreen();
                    MainWindow.window.layoutE.DrawLayout();
                    MainWindow.window.enemyE.DrawLayout();
                    DrawScreen16();
                }
            }
        }
        private void screenImage16_MouseUp(object sender, MouseButtonEventArgs e)
        {
            screenDown = false;
        }
        private void Tile16PageButton_Click(object sender, RoutedEventArgs e)
        {
            Button b = sender as Button;
            int i = Convert.ToInt32(b.Content.ToString().Trim(), 16);
            if (page16 == i)
                return;
            page16 = i;
            if (past16 != null)
            {
                past16.Background = Brushes.Black;
                past16.Foreground = Brushes.White;
            }
            b.Background = Brushes.LightBlue;
            b.Foreground = Brushes.Black;
            past16 = b;
            UpdateCursor16();
            DrawTiles16();
        }
        private void tileImage16_MouseUp(object sender, MouseButtonEventArgs e)
        {
            tilesDown16 = false;
            screenDown = false;
        }
        private void tileImage16_MouseDown(object sender, MouseButtonEventArgs e)
        {
            int tileAmount = Const.Tile16Count[Level.Id, Level.BG] - 1;
            Point p = e.GetPosition(tileImage16);
            int x = (int)p.X;
            int y = (int)p.Y;
            int cX = SNES.GetSelectedTile(x, tileImage16.ActualWidth, 16);
            int cY = SNES.GetSelectedTile(y, tileImage16.ActualHeight, 16);
            int id = cX + (cY * 16);
            ResetScreenCursor16();
            if (!tilesDown16)
            {
                if ((uint)id > 0xFF)
                    id = 0xFF;
                id += page16 * 0x100;

                if (id > tileAmount)
                {
                    id = tileAmount;
                }
                selectedTile16 = id;
                tile16Int.Value = id;
                tilesDown16 = true;
                UpdateCursor16();
                DrawTile16();
            }
        }
        private void tileImage16_MouseMove(object sender, MouseEventArgs e)
        {
            if (!tilesDown16)
                return;

            Point p = e.GetPosition(tileImage16);
            int x = (int)p.X;
            int y = (int)p.Y;
            int cX = SNES.GetSelectedTile(x, tileImage16.ActualWidth, 16);
            int cY = SNES.GetSelectedTile(y, tileImage16.ActualHeight, 16);


            int id = selectedTile16 & 0xFF;
            int id2 = cX + (cY * 16);
            if (id == id2)
                return;

            int tX = selectedTile16 & 0xF;
            int tY = (selectedTile16 >> 4) & 0xF;

            if (tX < cX) //Width Selection
                Grid.SetColumnSpan(cursor16, 1 + cX - tX);
            else
            {
                if (tX == cX)
                    Grid.SetColumnSpan(cursor16, 1);
                else
                {
                    Grid.SetColumnSpan(cursor16, tX - cX + 1);
                    Grid.SetColumn(cursor16, cX);
                }
            }
            if (tY < cY) //Height Selection
                Grid.SetRowSpan(cursor16, 1 + cY - tY);
            else
            {
                if (tY == cY)
                    Grid.SetRowSpan(cursor16, 1);
                else
                {
                    Grid.SetRowSpan(cursor16, tY - cY + 1);
                    Grid.SetRow(cursor16, cY);
                }
            }
        }
        private void tileImage16_MouseLeave(object sender, MouseEventArgs e)
        {
            tilesDown16 = false;
        }
        private void screenInt16_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue == null || !mode16)
                return;
            if (screenId == (int)e.NewValue || SNES.rom == null)
                return;
            screenId = (int)e.NewValue;
            UpdateScreenCursor();
            DrawScreen16();
        }
        private void ScreenGrid16Btn_Click(object sender, RoutedEventArgs e)
        {
            if (screenGrid16.ShowGridLines)
                screenGrid16.ShowGridLines = false;
            else
                screenGrid16.ShowGridLines = true;
        }
        private void TileGrid16Btn_Click(object sender, RoutedEventArgs e)
        {
            if (tileGrid16.ShowGridLines)
                tileGrid16.ShowGridLines = false;
            else
                tileGrid16.ShowGridLines = true;
        }
        private void Confirm16Button_Click(object sender, RoutedEventArgs e)
        {
            if (tiles32.Count > Const.Tile32Count[Level.Id, Level.BG])
            {
                MessageBox.Show($"Max amount of 32x32 is: {Const.Tile32Count[Level.Id, Level.BG]:X}");
                return;
            }

            //Save Level as 32x32 Tiles
            Dictionary<ulong, ushort> tileDictionary = new Dictionary<ulong, ushort>();

            ushort index = 0;

            foreach (ulong key in tiles32)
            {
                tileDictionary[key] = index;
                index++;
            }

            byte[] data = screenData16;

            int Id;
            if (Const.Id == Const.GameId.MegaManX3 && Level.Id == 0xE) Id = 0x10; //special case for MMX3 rekt version of dophler 2
            else if (Const.Id == Const.GameId.MegaManX3 && Level.Id > 0xE) Id = (Level.Id - 0xF) + 0xE; //Buffalo or Beetle
            else Id = Level.Id;

            int screenDestBase = SNES.CpuToOffset(BitConverter.ToInt32(SNES.rom, Const.ScreenDataPointersOffset[Level.BG] + Id * 3));

            //Create the Screen Data
            for (int screen = 0; screen < Const.ScreenCount[Level.Id, Level.BG]; screen++)
            {
                int screenBase = screen * 0x200;

                for (int y = 0; y < 8; y++)
                {
                    int rowBase = screenBase + y * 64;

                    for (int x = 0; x < 8; x++)
                    {
                        int baseOffset = rowBase + x * 4;

                        ushort TL = (ushort)(data[baseOffset + 0] | (data[baseOffset + 1] << 8));
                        ushort TR = (ushort)(data[baseOffset + 2] | (data[baseOffset + 3] << 8));
                        ushort BL = (ushort)(data[baseOffset + 32] | (data[baseOffset + 33] << 8));
                        ushort BR = (ushort)(data[baseOffset + 34] | (data[baseOffset + 35] << 8));

                        ulong key = ((ulong)TL)
                                  | ((ulong)TR << 16)
                                  | ((ulong)BL << 32)
                                  | ((ulong)BR << 48);

                        ushort value = tileDictionary[key];
                        BinaryPrimitives.WriteUInt16LittleEndian(SNES.rom.AsSpan(screenDestBase + screen * 0x80 + x * 2 + y * 16), value);
                    }
                }
            }

            int tile32DestBase = SNES.CpuToOffset(BitConverter.ToInt32(SNES.rom, Const.Tile32DataPointersOffset[Level.BG] + Id * 3));
            foreach (var tile in tileDictionary)
            {
                int offset = tile.Value * 8 + tile32DestBase;
                BinaryPrimitives.WriteUInt64LittleEndian(SNES.rom.AsSpan(offset), tile.Key);
            }

            //Clear Screen Undos of 16x16 Mode & Undos of 32x32 Tile Edits
            if (MainWindow.undos.Count != 0)
            {
                for (int i = (MainWindow.undos.Count - 1); i != -1; i--)
                {
                    if (MainWindow.undos[i].type == TeheManX_Editor.Undo.UndoType.Screen || MainWindow.undos[i].type == TeheManX_Editor.Undo.UndoType.X32)
                        MainWindow.undos.RemoveAt(i);
                }
            }

            //Done
            screenInt.Value = screenId;
            MainWindow.window.layoutE.DrawLayout();
            MainWindow.window.layoutE.DrawScreen();
            MainWindow.window.enemyE.DrawLayout();

            mode16 = false;
            DrawTiles();
            DrawTile();

            tile16ModeGrid.Visibility = Visibility.Collapsed;
            tile32ModeGrid.Visibility = Visibility.Visible;
        }
        private void tile16Int_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue == null)
                return;
            if (selectedTile16 == (int)e.NewValue || SNES.rom == null)
                return;
            selectedTile16 = (int)e.NewValue;
            DrawTile16();
        }
        #endregion Events
    }
}
