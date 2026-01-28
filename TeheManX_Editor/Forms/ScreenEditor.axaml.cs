using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Platform.Storage;
using SkiaSharp;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace TeheManX_Editor.Forms;

public partial class ScreenEditor : UserControl
{
    #region Fields
    internal static bool ShowScreenGrid;
    internal static bool ShowTileGrid;
    #endregion Fields

    #region Properties
    SKBitmap screenBMP = new SKBitmap(256, 256, SKColorType.Bgra8888, SKAlphaType.Premul);
    SKBitmap tileBMP = new SKBitmap(256, 1024, SKColorType.Bgra8888, SKAlphaType.Premul);
    SKBitmap tileBMP_S = new SKBitmap(32, 32, SKColorType.Bgra8888, SKAlphaType.Premul);
    Button past;
    bool screenDown = false; //used in both modes
    public int page;
    public int selectedTile; //32x
    public int screenId = 1; //used in both modes

    /*16x16 Mode Properties*/
    public bool mode16 = false; //is 16x16 mode active
    public byte[] screenData16 = null; //holds the screen data for 16x16 mode
    HashSet<ulong> tiles32 = new HashSet<ulong>(); //the 32x32 tiles that are based off the data in screenData16
    int pastTiles32Count = -1;
    SKBitmap tileBMP16 = new SKBitmap(256, 256, SKColorType.Bgra8888, SKAlphaType.Premul);
    SKBitmap tileBMP_S16 = new SKBitmap(16, 16, SKColorType.Bgra8888, SKAlphaType.Premul);
    Button past16;
    public int page16;
    public int screenSelect16 = -1;
    public int selectColumn16 = -1;
    public int selectRow16 = -1;
    public int selectColumnSpan16;
    public int selectRowSpan16;
    public int startCol16;
    public int startRow16;
    public int selectedTile16; //16x
    public int tileColumn16;
    public int tileRow16;
    public int tileColumnSpan16 = 1;
    public int tileRowSpan16 = 1;
    bool tilesDown16 = false;
    #endregion Properties

    #region Constructors
    public ScreenEditor()
    {
        InitializeComponent();
    }
    #endregion Constructors

    #region Methods
    public void AssignLimits()
    {
        int screenAmount = Const.ScreenCount[Level.Id, Level.BG] - 1;
        screenInt.Maximum = screenAmount;
        if (screenInt.Value > screenAmount)
            screenInt.Value = screenAmount;

        int tile32Amount = Const.Tile32Count[Level.Id, Level.BG] - 1;
        tile32Int.Maximum = tile32Amount;
        if (selectedTile > tile32Amount)
            tile32Int.Value = tile32Amount;

        DrawScreen();
        DrawTiles();
        DrawTile();
    }
    public void DrawScreen()
    {
        if (mode16)
            screenImage16.InvalidateVisual();
        else
            screenImage.InvalidateVisual();
    }
    public void DrawTiles()
    {
        if (mode16)
            tileImage16.InvalidateVisual();
        else
            tileImage.InvalidateVisual();
    }
    public void DrawTile()
    {
        if (mode16)
            tileImageS16.InvalidateVisual();
        else 
            tileImageS.InvalidateVisual();
    }
    private void ChangePageTxt()
    {
        //pageBtn.Content = Convert.ToString(page).PadRight(3, '0') + "-" + Convert.ToString(page).PadRight(3, 'F');
    }
    public async Task DeleteScreen(bool shift)
    {
        if (shift)
        {
            bool result = await MessageBox.Show(MainWindow.window, "Are you sure you want to delete all of the Screens in Layer " + (Level.BG + 1) + "?\nThis cant be un-done", "WARNING", MessageBoxButton.YesNo);
            if (!result)
                return;

            if (mode16)
                Array.Clear(screenData16, 0, screenData16.Length);
            else
            {
                int offset = Level.ScreenDataOffset;
                Array.Clear(SNES.rom, offset, 0x80 * Const.ScreenCount[Level.Id, Level.BG]);
            }
            //Clear Screen Undos of 32x32
            if (MainWindow.undos.Count != 0)
            {
                for (int i = (MainWindow.undos.Count - 1); i != -1; i--)
                {
                    if (MainWindow.undos[i].type == Undo.UndoType.Screen)
                        MainWindow.undos.RemoveAt(i);
                }
            }
            MainWindow.window.layoutE.DrawLayout();
            MainWindow.window.layoutE.DrawScreen();
            MainWindow.window.enemyE.DrawLayout();
            DrawScreen();
            SNES.edit = true;
            return;
        }
        if (mode16)
        {
            if (MainWindow.undos.Count == Const.MaxUndo)
                MainWindow.undos.RemoveAt(0);
            MainWindow.undos.Add(Undo.CreateGroupScreenUndo16((byte)screenId, 0, 0, 16, 16));
            Array.Clear(screenData16, screenId * 0x200, 0x200);
            DrawScreen16();
        }
        else
        {
            if (MainWindow.undos.Count == Const.MaxUndo)
                MainWindow.undos.RemoveAt(0);
            MainWindow.undos.Add(Undo.CreateGroupScreenUndo((byte)screenId, 0, 0, 8, 8)); ;

            int offset = Level.ScreenDataOffset + screenId * 0x80;
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
        if (mode16)
            screenImage16.InvalidateVisual();
        else
            screenImage.InvalidateVisual();
    }
    private void DrawTiles16()
    {
        if (mode16)
            tileImage16.InvalidateVisual();
        else
            tileImage.InvalidateVisual();
    }
    private void DrawTile16()
    {
        if (mode16)
            tileImageS16.InvalidateVisual();
        else
            tileImageS.InvalidateVisual();
    }
    public void ResetScreenCursor16()
    {
        screenSelect16 = -1;
        selectColumn16 = -1;
        DrawScreen16();
    }
    public void Update32x32TileList() //Get the 16x16 Tile Screen Data and Create a list of 32x32 Tiles
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
    #endregion Methods

    #region Events
    private void screenInt_ValueChanged(object sender, int e)
    {
        if (screenId == e || SNES.rom == null)
            return;
        screenId = e;
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
            past.Background = Brushes.Transparent; //Old Color
        //New Color
        b.Background = Brushes.LightBlue;
        past = b;
        ChangePageTxt();
        DrawTiles();
    }
    private void screenImage_PointerPressed(object sender, PointerPressedEventArgs e)
    {
        PointerPoint p = e.GetCurrentPoint(screenImage);
        PointerPointProperties properties = p.Properties;
        int x = (int)p.Position.X;
        int y = (int)p.Position.Y;
        int cX = SNES.GetSelectedTile(x, screenImage.Bounds.Width, 8);
        int cY = SNES.GetSelectedTile(y, screenImage.Bounds.Height, 8);

        if (properties.IsRightButtonPressed)
        {
            int offset = Level.ScreenDataOffset;
            selectedTile = BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(offset + (screenId * 0x80) + (cX * 2) + (cY * 16)));
            tile32Int.Value = selectedTile;
            DrawTile();
            DrawTiles();
        }
        else
        {
            int offset = Level.ScreenDataOffset;
            screenDown = true;
            ushort tileId = BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(offset + screenId * 0x80 + cX * 2 + cY * 16));
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
    private void screenImage_PointerMoved(object? sender, PointerEventArgs e)
    {
        PointerPoint p = e.GetCurrentPoint(screenImage);
        PointerPointProperties properties = p.Properties;
        if (properties.IsLeftButtonPressed && screenDown)
        {
            //Get Cords
            int x = (int)p.Position.X;
            int y = (int)p.Position.Y;
            int cX = SNES.GetSelectedTile(x, screenImage.Bounds.Width, 8);
            int cY = SNES.GetSelectedTile(y, screenImage.Bounds.Width, 8);
            int cord = (cX * 2) + (cY * 16);

            int offset = Level.ScreenDataOffset + screenId * 0x80;

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
    private void screenImage_PointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        screenDown = false;
    }
    private void ScreenGridBtn_Click(object sender, RoutedEventArgs e)
    {
        ShowScreenGrid = !ShowScreenGrid;
        DrawScreen();
    }
    private void TileGridBtn_Click(object sender, RoutedEventArgs e)
    {
        ShowTileGrid = !ShowTileGrid;
        DrawTiles();
    }
    private void tile32Int_ValueChanged(object sender, int e)
    {
        if (SNES.rom == null)
            return;
        if (selectedTile == e)
            return;
        selectedTile = e;
        DrawTiles();
        DrawTile();
    }
    private void screenImage_MeasureEvent(object? sender, Size e)
    {
        if (MainWindow.settings.UseFixedScale)
        {
            double fixedDimension = MainWindow.settings.ScreenScale * 256;
            screenImage.MeasuredSize = new Size(fixedDimension, fixedDimension);
            return;
        }

        Size size = new Size(256, 256);

        if (e.Width < 256 || e.Height < 256)
        {
            screenImage.MeasuredSize = size;
        }
        else
        {
            double width = e.Width;
            double height = e.Height;

            // Handle Infinity cases
            if (double.IsInfinity(width) && double.IsInfinity(height))
                width = height = 256;
            else if (double.IsInfinity(width))
                width = height;
            else if (double.IsInfinity(height))
                height = width;

            double dimension = Math.Max(width, height);
            Size newSize = new Size(dimension, dimension);

            screenImage.MeasuredSize = newSize;
        }
    }
    private void screenImage_RenderEvent(object? sender, SkiaCanvasEventArgs e)
    {
        unsafe
        {
            Level.DrawScreen(screenId, screenBMP.RowBytes, screenBMP.GetPixels());
        }
        SKCanvas canvas = e.Canvas;
        Rect rect = e.Bounds;

        if (rect.Width < 256 || rect.Height < 256)
            rect = new Rect(0, 0, 256, 256);

        if (MainWindow.settings.UseFixedScale)
        {
            double fixedDimension = MainWindow.settings.ScreenScale * 256;
            rect = new Rect(0, 0, fixedDimension, fixedDimension);
        }

        SKRect destRect = new SKRect(0, 0, (float)rect.Width, (float)rect.Height);
        canvas.DrawBitmap(screenBMP, destRect);

        if (ShowScreenGrid)
            MainWindow.DrawGrid(canvas, destRect.Width, destRect.Height, 8, 8);
    }
    private void tileImage_MeasureEvent(object? sender, Size e)
    {
        const double srcW = 256;
        const double srcH = 1024;

        if (MainWindow.settings.UseFixedScale)
        {
            tileImage.MeasuredSize = new Size(srcW * MainWindow.settings.ScreenTilesScale, srcH * MainWindow.settings.ScreenTilesScale);
            return;
        }

        if (e.Width < srcW || e.Height < srcH)
            tileImage.MeasuredSize = new Size(srcW, srcH);
        else
        {
            double scale = e.Width / srcW;
            double height = srcH * scale;

            tileImage.MeasuredSize = new Size(e.Width, height);
        }
    }
    private void tileImage_RenderEvent(object? sender, SkiaCanvasEventArgs e)
    {
        unsafe
        {
            int stride = tileBMP.RowBytes;
            nint ptr = tileBMP.GetPixels();

            int tile32Offset = Level.Tile32DataOffset;
            int tile32MaxId = Const.Tile32Count[Level.Id, Level.BG] - 1;

            for (int y = 0; y < 32; y++)
            {
                for (int x = 0; x < 8; x++)
                {
                    int tileId32 = x + (y * 8) + (page * 0x100);
                    if (tileId32 > tile32MaxId)
                    {
                        unsafe
                        {
                            byte* buffer = (byte*)ptr;
                            for (int r = 0; r < 32; r++)
                            {
                                for (int c = 0; c < 32; c++)
                                {
                                    buffer[(x * 32 + c) * 4 + (y * 32 + r) * stride + 0] = 0;
                                    buffer[(x * 32 + c) * 4 + (y * 32 + r) * stride + 1] = 0;
                                    buffer[(x * 32 + c) * 4 + (y * 32 + r) * stride + 2] = 0;
                                    buffer[(x * 32 + c) * 4 + (y * 32 + r) * stride + 3] = 0xFF;
                                }
                            }
                        }
                        continue;
                    }
                    Level.Draw16xTile(BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(tile32Offset + (tileId32 * 8))), x * 32, y * 32, stride, ptr);
                    Level.Draw16xTile(BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(tile32Offset + (tileId32 * 8) + 2)), x * 32 + 16, y * 32, stride, ptr);
                    Level.Draw16xTile(BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(tile32Offset + (tileId32 * 8) + 4)), x * 32, y * 32 + 16, stride, ptr);
                    Level.Draw16xTile(BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(tile32Offset + (tileId32 * 8) + 6)), x * 32 + 16, y * 32 + 16, stride, ptr);
                }
            }
        }
        SKCanvas canvas = e.Canvas;

        Rect rect = e.Bounds;

        if (rect.Width < 256 || rect.Height < 1024)
            rect = new Rect(0, 0, 256, 1024);

        if (MainWindow.settings.UseFixedScale)
            rect = new Rect(0, 0, 256 * MainWindow.settings.ScreenTilesScale, 1024 * MainWindow.settings.ScreenTilesScale);

        SKRect destRect = new SKRect(0, 0, (float)rect.Width, (float)rect.Height);
        canvas.DrawBitmap(tileBMP, destRect);

        if (ShowTileGrid)
            MainWindow.DrawGrid(canvas, destRect.Width, destRect.Height, 8, 32);

        if (page == ((selectedTile >> 8) & 0xFF))
            MainWindow.DrawSelect(canvas, destRect.Width, destRect.Height, 8, 32, selectedTile & 0x7, (selectedTile >> 3) & 0x1F);
    }
    private void tileImage_PointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        PointerPoint p = e.GetCurrentPoint(tileImage);
        PointerPointProperties properties = p.Properties;
        int x = (int)p.Position.X;
        int y = (int)p.Position.Y;
        int cX = SNES.GetSelectedTile(x, tileImage.Bounds.Width, 8);
        int cY = SNES.GetSelectedTile(y, tileImage.Bounds.Height, 32);
        int id = cX + (cY * 8);
        if ((uint)id > 0xFF)
            id = 0xFF;
        id += page * 0x100;
        if (id > (Const.Tile32Count[Level.Id, Level.BG]) - 1)
            return;
        //New Valid Tile
        if (properties.IsRightButtonPressed)
        {
            MainWindow.window.tile32E.tileInt.Value = id; //select Tile in 32x32 Tile Editor
            return;
        }
        selectedTile = id;
        tile32Int.Value = selectedTile;
        DrawTiles();
        DrawTile();
    }
    private void tileImageS_RenderEvent(object? sender, SkiaCanvasEventArgs e)
    {
        unsafe
        {
            int stride = tileBMP_S.RowBytes;
            nint ptr = tileBMP_S.GetPixels();

            int tile32Offset = Level.Tile32DataOffset;
            Level.Draw16xTile(BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(tile32Offset + (selectedTile * 8))), 0, 0, stride, ptr);
            Level.Draw16xTile(BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(tile32Offset + (selectedTile * 8) + 2)), 16, 0, stride, ptr);
            Level.Draw16xTile(BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(tile32Offset + (selectedTile * 8) + 4)), 0, 16, stride, ptr);
            Level.Draw16xTile(BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(tile32Offset + (selectedTile * 8) + 6)), 16, 16, stride, ptr);
        }
        SKCanvas canvas = e.Canvas;
        SKRect destRect = new SKRect(0, 0, 64, 64);
        canvas.DrawBitmap(tileBMP_S, destRect);
    }
    private async void Help_Click(object sender, RoutedEventArgs e)
    {
        HelpWindow h = new HelpWindow(2);
        await h.ShowDialog(MainWindow.window);
    }
    private async void SnapButton_Click(object sender, RoutedEventArgs e)
    {
        IStorageProvider storageProvider = MainWindow.window.StorageProvider;

        IReadOnlyList<IStorageFolder> folders = await storageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Select Screens Save Location",
            AllowMultiple = false
        });
        IStorageFolder? folder = folders.FirstOrDefault();
        if (folder != null)
        {
            string folderPath = folder.Path.LocalPath;

            WriteableBitmap bitmap = new WriteableBitmap(new PixelSize(256, 256), new Vector(96, 96), PixelFormats.Bgr32, null);
            for (int i = 0; i < Const.ScreenCount[Level.Id, Level.BG]; i++)
            {
                using (var fb = bitmap.Lock())
                {
                    Level.DrawScreen(i, 0, 0, fb.RowBytes, fb.Address);
                }

                string filePath = Path.Combine(folderPath, $"SCREEN_{i:X2}.png");
                using (var fs = File.Create(filePath))
                {
                    bitmap.Save(fs);
                }
            }
            await MessageBox.Show(MainWindow.window, "All Screens have been exported !!!");
        }
    }
    private void Mode16x16Button_Click(object sender, RoutedEventArgs e)
    {
        int newSize = Const.ScreenCount[Level.Id, Level.BG] * 0x200;

        if (screenData16 == null || screenData16.Length != newSize)
            screenData16 = GC.AllocateUninitializedArray<byte>(newSize, pinned: false);

        int screenDataBaseOffset = Level.ScreenDataOffset;
        int tile32BaseOffset = Level.Tile32DataOffset;

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
                    BinaryPrimitives.WriteUInt16LittleEndian(screenData16.AsSpan(dstBaseOffset + ((x * 2) + 0 + y * 32 + 0) * 2, 2), BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(srcBaseOffset + 0)));
                    BinaryPrimitives.WriteUInt16LittleEndian(screenData16.AsSpan(dstBaseOffset + ((x * 2) + 1 + y * 32 + 0) * 2, 2), BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(srcBaseOffset + 2)));
                    BinaryPrimitives.WriteUInt16LittleEndian(screenData16.AsSpan(dstBaseOffset + ((x * 2) + 0 + y * 32 + 16) * 2, 2), BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(srcBaseOffset + 4)));
                    BinaryPrimitives.WriteUInt16LittleEndian(screenData16.AsSpan(dstBaseOffset + ((x * 2) + 1 + y * 32 + 16) * 2, 2), BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(srcBaseOffset + 6)));

                }
            }
        }

        //Clear Screen Undos of 32x32 Mode
        if (MainWindow.undos.Count != 0)
        {
            for (int i = (MainWindow.undos.Count - 1); i != -1; i--)
            {
                if (MainWindow.undos[i].type == Undo.UndoType.Screen)
                    MainWindow.undos.RemoveAt(i);
            }
        }

        tiles32.Clear();

        //Update 16x16 Mode UI before swapping Modes
        DrawTiles16();
        screenInt16.Value = screenId;
        screenInt16.Maximum = Const.ScreenCount[Level.Id, Level.BG] - 1;

        tile16Int.Maximum = Const.Tile16Count[Level.Id, Level.BG] - 1;
        DrawTile16();
        ResetScreenCursor16();

        tile32ModeGrid.IsVisible = false;
        tile16ModeGrid.IsVisible = true;
        mode16 = true;
    }
    /*
     *  Mode 16x16 GUI Events
     */
    private void screenImage16_PointerPressed(object sender, PointerPressedEventArgs e)
    {
        PointerPoint p = e.GetCurrentPoint(screenImage16);
        PointerPointProperties properties = p.Properties;
        int x = (int)p.Position.X;
        int y = (int)p.Position.Y;
        int cX = SNES.GetSelectedTile(x, screenImage16.Bounds.Width, 16);
        int cY = SNES.GetSelectedTile(y, screenImage16.Bounds.Height, 16);
        int cord = (cX * 2) + (cY * 16 * 2);
        if (properties.IsRightButtonPressed) // Copy
        {
            if (e.KeyModifiers == KeyModifiers.Shift) // Multi-Select
            {
                if (selectColumn16 == -1)
                {
                    selectColumnSpan16 = 1;
                    selectRowSpan16 = 1;
                    selectColumn16 = cX;
                    selectRow16 = cY;
                    startCol16 = cX;
                    startRow16 = cY;
                    screenSelect16 = screenId;
                }
                else
                {
                    if (cX > startCol16)
                    {
                        selectColumn16 = startCol16;
                        selectColumnSpan16 = cX - startCol16 + 1;
                    }
                    else if (cX < startCol16)
                    {
                        selectColumn16 = cX;
                        selectColumnSpan16 = startCol16 - cX + 1;
                    }
                    else
                    {
                        selectColumn16 = startCol16;
                        selectColumnSpan16 = 1;
                    }

                    if (cY > startRow16)
                    {
                        selectRow16 = startRow16;
                        selectRowSpan16 = cY - startRow16 + 1;
                    }
                    else if (cY < startRow16)
                    {
                        selectRow16 = cY;
                        selectRowSpan16 = startRow16 - cY + 1;
                    }
                    else
                    {
                        selectRow16 = startRow16;
                        selectRowSpan16 = 1;
                    }
                }
                DrawScreen16();
            }
            else
            {
                selectedTile16 = BinaryPrimitives.ReadUInt16LittleEndian(screenData16.AsSpan(cord + screenId * 0x200));
                tile16Int.Value = selectedTile16;
                tileColumnSpan16 = 1;
                tileRowSpan16 = 1;
                ResetScreenCursor16();
                DrawTiles16();
                DrawTile16();
            }
        }
        else // Paste
        {
            if (e.KeyModifiers == KeyModifiers.Shift)
            {
                if ((selectColumnSpan16 != 1 || selectRowSpan16 != 1) && selectColumn16 != -1) //Paste From other Screen
                {
                    if (MainWindow.undos.Count == Const.MaxUndo)
                        MainWindow.undos.RemoveAt(0);
                    MainWindow.undos.Add(Undo.CreateGroupScreenUndo16((byte)screenId, (byte)cX, (byte)cY, (byte)selectColumnSpan16, (byte)selectRowSpan16));
                    for (int r = 0; r < selectRowSpan16; r++)
                    {
                        for (int c = 0; c < selectColumnSpan16; c++)
                        {
                            if (cX + c > 15)
                                continue;
                            if (cY + r > 15)
                                continue;
                            int dest = cord + c * 2 + r * 32 + (screenId * 0x200);

                            int srcCol = selectColumn16 + c;
                            int srcRow = selectRow16 + r;
                            ushort val = BinaryPrimitives.ReadUInt16LittleEndian(screenData16.AsSpan(screenSelect16 * 0x200 + srcCol * 2 + srcRow * 32));
                            BinaryPrimitives.WriteUInt16LittleEndian(screenData16.AsSpan(dest), val);
                        }
                    }
                    //End of Loops
                    SNES.edit = true;
                    MainWindow.window.layoutE.DrawScreen();
                    MainWindow.window.layoutE.DrawLayout();
                    MainWindow.window.enemyE.DrawLayout();
                    DrawScreen16();
                }
                return;
            }

            //Tile Paste
            screenDown = true;

            if (tileColumnSpan16 != 1 || tileRowSpan16 != 1) //Multi-Select
            {
                int tileAmount = Const.Tile16Count[Level.Id, Level.BG] - 1;
                int rowSrc = tileRow16;
                int colSrc = tileColumn16;

                if (MainWindow.undos.Count == Const.MaxUndo)
                    MainWindow.undos.RemoveAt(0);
                MainWindow.undos.Add(Undo.CreateGroupScreenUndo16((byte)screenId, (byte)cX, (byte)cY, (byte)tileColumnSpan16, (byte)tileRowSpan16));

                for (int r = 0; r < tileRowSpan16; r++)
                {
                    for (int c = 0; c < tileColumnSpan16; c++)
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
            MainWindow.window.layoutE.DrawScreen();
            MainWindow.window.layoutE.DrawLayout();
            MainWindow.window.enemyE.DrawLayout();
            DrawScreen16();
        }
    }
    private void screenImage16_PointerMoved(object? sender, PointerEventArgs e)
    {
        PointerPoint p = e.GetCurrentPoint(screenImage16);
        PointerPointProperties properties = p.Properties;
        if (properties.IsLeftButtonPressed && screenDown)
        {
            //Get Cords
            int x = (int)p.Position.X;
            int y = (int)p.Position.Y;
            int cX = SNES.GetSelectedTile(x, screenImage16.Bounds.Width, 16);
            int cY = SNES.GetSelectedTile(y, screenImage16.Bounds.Height, 16);
            int cord = (cX * 2) + (cY * 32);

            if (BinaryPrimitives.ReadUInt16LittleEndian(screenData16.AsSpan(screenId * 0x200 + cord)) == (ushort)selectedTile16)
                return;

            if (MainWindow.undos.Count == Const.MaxUndo)
                MainWindow.undos.RemoveAt(0);
            MainWindow.undos.Add(Undo.CreateScreenUndo16((byte)screenId, (byte)cX, (byte)cY));
            BinaryPrimitives.WriteUInt16LittleEndian(screenData16.AsSpan(screenId * 0x200 + cord), (ushort)selectedTile16);
            SNES.edit = true;
            MainWindow.window.layoutE.DrawScreen();
            MainWindow.window.layoutE.DrawLayout();
            MainWindow.window.enemyE.DrawLayout();
            DrawScreen16();
        }
    }
    private void screenImage16_PointerReleased(object? sender, PointerReleasedEventArgs e)
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
            past16.Background = Brushes.Transparent;
        b.Background = Brushes.LightBlue;
        past16 = b;
        DrawTiles16();
    }
    private void tileImage16_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        int tileAmount = Const.Tile16Count[Level.Id, Level.BG] - 1;
        PointerPoint p = e.GetCurrentPoint(tileImage16);
        PointerPointProperties properties = p.Properties;
        int x = (int)p.Position.X;
        int y = (int)p.Position.Y;
        int cX = SNES.GetSelectedTile(x, tileImage16.Bounds.Width, 16);
        int cY = SNES.GetSelectedTile(y, tileImage16.Bounds.Height, 16);
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
            tileColumn16 = cX;
            tileRow16 = cY;
            tileColumnSpan16 = 1;
            tileRowSpan16 = 1;
            tile16Int.Value = id;
            tilesDown16 = true;
            DrawTiles16();
            DrawTile16();
        }
    }
    private void tileImage16_PointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        tilesDown16 = false;
        screenDown = false;
    }
    private void tileImage16_PointerMoved(object? sender, PointerEventArgs e)
    {
        if (!tilesDown16)
            return;

        PointerPoint p = e.GetCurrentPoint(tileImage16);
        PointerPointProperties properties = p.Properties;
        int x = (int)p.Position.X;
        int y = (int)p.Position.Y;
        int cX = SNES.GetSelectedTile(x, tileImage16.Bounds.Width, 16);
        int cY = SNES.GetSelectedTile(y, tileImage16.Bounds.Height, 16);


        int id = selectedTile16 & 0xFF;
        int id2 = cX + (cY * 16);
        if (id == id2)
            return;

        int tX = selectedTile16 & 0xF;
        int tY = (selectedTile16 >> 4) & 0xF;

        if (tX < cX) //Width Selection
            tileColumnSpan16 = 1 + cX - tX;
        else
        {
            if (tX == cX)
                tileColumnSpan16 = 1;
            else
            {
                tileColumnSpan16 = tX - cX + 1;
                tileColumn16 = cX;
            }
        }
        if (tY < cY) //Height Selection
            tileRowSpan16 = 1 + cY - tY;
        else
        {
            if (tY == cY)
                tileRowSpan16 = 1;
            else
            {
                tileRowSpan16 = tY - cY + 1;
                tileRow16 = cY;
            }
        }
        DrawTiles16();
    }
    private void tileImage16_PointerExited(object? sender, PointerEventArgs e)
    {
        tilesDown16 = false;
    }
    private void screenInt16_ValueChanged(object sender, int e)
    {
        if (!mode16)
            return;
        if (screenId == e || SNES.rom == null)
            return;
        screenId = e;
        DrawScreen16();
    }
    private void ScreenGrid16Btn_Click(object sender, RoutedEventArgs e)
    {
        ShowScreenGrid = !ShowScreenGrid;
        DrawScreen16();
    }
    private void TileGrid16Btn_Click(object sender, RoutedEventArgs e)
    {
        ShowTileGrid = !ShowTileGrid;
        DrawTiles16();
    }
    private void tile16Int_ValueChanged(object sender, int e)
    {
        if (selectedTile16 == e || SNES.rom == null)
            return;
        selectedTile16 = e;
        tileColumnSpan16 = 1;
        tileRowSpan16 = 1;
        DrawTile16();
        DrawTiles16();
    }
    private void screenImage16_MeasureEvent(object? sender, Size e)
    {
        if (MainWindow.settings.UseFixedScale)
        {
            double fixedDimension = MainWindow.settings.ScreenScale * 256;
            screenImage.MeasuredSize = new Size(fixedDimension, fixedDimension);
            return;
        }

        Size size = new Size(256, 256);
        if (e.Width < 256 || e.Height < 256)
        {
            screenImage16.MeasuredSize = size;
        }
        else
        {
            double width = e.Width;
            double height = e.Height;

            // Handle Infinity cases
            if (double.IsInfinity(width) && double.IsInfinity(height))
                width = height = 256;
            else if (double.IsInfinity(width))
                width = height;
            else if (double.IsInfinity(height))
                height = width;

            double dimension = Math.Max(width, height);
            Size newSize = new Size(dimension, dimension);

            screenImage16.MeasuredSize = newSize;
        }
    }
    private void screenImage16_RenderEvent(object? sender, SkiaCanvasEventArgs e)
    {
        int stride = screenBMP.RowBytes;
        unsafe
        {
            nint ptr = screenBMP.GetPixels();

            for (int y = 0; y < 16; y++)
            {
                for (int x = 0; x < 16; x++)
                {
                    ushort id = BinaryPrimitives.ReadUInt16LittleEndian(screenData16.AsSpan(x * 2 + y * 32 + screenId * 0x200));
                    Level.Draw16xTile(id, x * 16, y * 16, stride, ptr);
                }
            }
        }
        SKCanvas canvas = e.Canvas;
        Rect rect = e.Bounds;

        if (rect.Width < 256 || rect.Height < 256)
            rect = new Rect(0, 0, 256, 256);

        if (MainWindow.settings.UseFixedScale)
        {
            double fixedDimension = MainWindow.settings.ScreenScale * 256;
            rect = new Rect(0, 0, fixedDimension, fixedDimension);
        }

        SKRect destRect = new SKRect(0, 0, (float)rect.Width, (float)rect.Height);
        canvas.DrawBitmap(screenBMP, destRect);

        if (ShowScreenGrid)
            MainWindow.DrawGrid(canvas, destRect.Width, destRect.Height, 16, 16);

        if (screenId == screenSelect16 && selectColumn16 != -1)
            MainWindow.DrawSelect(canvas, destRect.Width, destRect.Height, 16, 16, selectColumn16, selectRow16, selectColumnSpan16, selectRowSpan16);
    }
    private void tileImage16_MeasureEvent(object? sender, Size e)
    {
        if (MainWindow.settings.UseFixedScale)
        {
            double fixedDimension = MainWindow.settings.ScreenTilesScale * 256;
            tileImage16.MeasuredSize = new Size(fixedDimension, fixedDimension);
            return;
        }

        Size size = new Size(256, 256);
        if (e.Width < 256 || e.Height < 256)
        {
            tileImage16.MeasuredSize = size;
        }
        else
        {
            double width = e.Width;
            double height = e.Height;

            // Handle Infinity cases
            if (double.IsInfinity(width) && double.IsInfinity(height))
                width = height = 256;
            else if (double.IsInfinity(width))
                width = height;
            else if (double.IsInfinity(height))
                height = width;

            double dimension = Math.Max(width, height);
            Size newSize = new Size(dimension, dimension);

            tileImage16.MeasuredSize = newSize;
        }
    }
    private void tileImage16_RenderEvent(object? sender, SkiaCanvasEventArgs e)
    {
        int stride = tileBMP16.RowBytes;
        int tile16MaxId = Const.Tile16Count[Level.Id, Level.BG] - 1;

        unsafe
        {
            nint ptr = tileBMP16.GetPixels();
            for (int y = 0; y < 16; y++)
            {
                for (int x = 0; x < 16; x++)
                {
                    int id = x + (y * 16) + (page16 * 0x100);
                    if (id > tile16MaxId)
                    {
                        unsafe
                        {
                            byte* buffer = (byte*)ptr;
                            for (int r = 0; r < 16; r++)
                            {
                                for (int c = 0; c < 16; c++)
                                {
                                    buffer[(x * 16 + c) * 4 + (y * 16 + r) * stride + 0] = 0;
                                    buffer[(x * 16 + c) * 4 + (y * 16 + r) * stride + 1] = 0;
                                    buffer[(x * 16 + c) * 4 + (y * 16 + r) * stride + 2] = 0;
                                    buffer[(x * 16 + c) * 4 + (y * 16 + r) * stride + 3] = 0xFF;
                                }
                            }
                        }
                        continue;
                    }
                    Level.Draw16xTile(id, x * 16, y * 16, stride, ptr);
                }
            }
        }
        SKCanvas canvas = e.Canvas;
        Rect rect = e.Bounds;

        if (rect.Width < 256 || rect.Height < 256)
            rect = new Rect(0, 0, 256, 256);

        if (MainWindow.settings.UseFixedScale)
        {
            double fixedDimension = MainWindow.settings.ScreenTilesScale * 256;
            rect = new Rect(0, 0, fixedDimension, fixedDimension);
        }

        SKRect destRect = new SKRect(0, 0, (float)rect.Width, (float)rect.Height);
        canvas.DrawBitmap(tileBMP16, destRect);

        if (ShowTileGrid)
            MainWindow.DrawGrid(canvas, destRect.Width, destRect.Height, 16, 16);

        if (page16 == ((selectedTile16 >> 8) & 0xFF))
        {
            if (tileColumnSpan16 == 1)
                MainWindow.DrawSelect(canvas, destRect.Width, destRect.Height, 16, 16, selectedTile16 & 0xF, (selectedTile16 >> 4) & 0xF);
            else
                MainWindow.DrawSelect(canvas, destRect.Width, destRect.Height, 16, 16, tileColumn16, tileRow16, tileColumnSpan16, tileRowSpan16);
        }
    }
    private void tileImageS16_RenderEvent(object? sender, SkiaCanvasEventArgs e)
    {
        unsafe
        {
            Level.Draw16xTile(selectedTile16, 0, 0, tileBMP_S16.RowBytes, tileBMP_S16.GetPixels());
        }
        SKCanvas canvas = e.Canvas;
        canvas.DrawBitmap(tileBMP_S16, new SKRect(0, 0, 64, 64));
    }
    private async void Confirm16Button_Click(object sender, RoutedEventArgs e)
    {
        Update32x32TileList();
        if (tiles32.Count > Const.Tile32Count[Level.Id, Level.BG])
        {
            await MessageBox.Show(MainWindow.window, $"Max amount of 32x32 is: 0x{Const.Tile32Count[Level.Id, Level.BG]:X} compared to your 0x{tiles32.Count:X}!", "ERROR");
            return;
        }

        int tile32DestBase = Level.Tile32DataOffset;

        if (tiles32.Count < Const.Tile32Count[Level.Id, Level.BG]) //Clearing Un-Needed 32x32 Tiles
        {
            int writeOffset = tile32DestBase + tiles32.Count * 8;
            int length = (Const.Tile32Count[Level.Id, Level.BG] - tiles32.Count) * 8;
            Array.Clear(SNES.rom, writeOffset, length);
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

        int screenDestBase = Level.ScreenDataOffset;

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
        selectedTile = 0;
        MainWindow.window.layoutE.DrawLayout();
        MainWindow.window.layoutE.DrawScreen();
        MainWindow.window.enemyE.DrawLayout();

        mode16 = false;
        DrawTiles();
        DrawTile();

        tile16ModeGrid.IsVisible = false;
        tile32ModeGrid.IsVisible = true;
    }
    #endregion Events
}