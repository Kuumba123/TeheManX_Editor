using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using SkiaSharp;
using System;
using System.Buffers.Binary;

namespace TeheManX_Editor.Forms;

public partial class Tile32Editor : UserControl
{
    #region Fields
    internal static bool ShowScreenGrid;
    internal static bool ShowTile32Grid;
    internal static bool ShowTile16Grid;
    static SKBitmap x16BMP = new SKBitmap(256, 256, SKColorType.Bgra8888, SKAlphaType.Premul);
    static SKBitmap tileBMP = new SKBitmap(256, 1024, SKColorType.Bgra8888, SKAlphaType.Premul);
    static SKBitmap tileBMP_S = new SKBitmap(32, 32, SKColorType.Bgra8888, SKAlphaType.Premul);
    #endregion Fields

    #region Properties
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
    }
    #endregion Constructors

    #region Methods
    public void AssignLimits()
    {
        int tile32Amount = Const.Tile32Count[Level.Id, Level.BG] - 1;
        tileInt.Maximum = tile32Amount;
        if (selectedTile > tile32Amount)
            tileInt.Value = tile32Amount;

        int offset = Level.Tile32DataOffset;

        int max16 = Const.Tile16Count[Level.Id, Level.BG] - 1;

        topLeftInt.Value = BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(offset + selectedTile * 8 + 0));
        topLeftInt.Maximum = max16;
        topRightInt.Value = BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(offset + selectedTile * 8 + 2));
        topRightInt.Maximum = max16;
        bottomLeftInt.Value = BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(offset + selectedTile * 8 + 4));
        bottomLeftInt.Maximum = max16;
        bottomRightInt.Value = BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(offset + selectedTile * 8 + 6));
        bottomRightInt.Maximum = max16;

        DrawTiles();
        Draw16xTiles();
        DrawTile();
    }
    public void DrawTiles()
    {
        tileImage.InvalidateMeasure();
    }
    public void Draw16xTiles()
    {
        x16Image.InvalidateVisual();
    }
    public void DrawTile()
    {
        tileImageS.InvalidateVisual();
    }
    private void ChangePageTxt()
    {
        //pageBtn.Content = Convert.ToString(page).PadRight(3, '0') + "-" + Convert.ToString(page).PadRight(3, 'F');
    }
    public void UpdateTile32Ints(int offset)
    {
        var rom = SNES.rom.AsSpan();

        int baseOffset = offset + selectedTile * 8;

        topLeftInt.Value = BinaryPrimitives.ReadUInt16LittleEndian(rom.Slice(baseOffset + 0));
        topRightInt.Value = BinaryPrimitives.ReadUInt16LittleEndian(rom.Slice(baseOffset + 2));
        bottomLeftInt.Value = BinaryPrimitives.ReadUInt16LittleEndian(rom.Slice(baseOffset + 4));
        bottomRightInt.Value = BinaryPrimitives.ReadUInt16LittleEndian(rom.Slice(baseOffset + 6));
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
            past.Background = Brushes.Transparent; //Old Color
        //New Color
        b.Background = Brushes.LightBlue;
        past = b;
        DrawTiles();
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
            past2.Background = Brushes.Transparent;
        b.Background = Brushes.LightBlue;
        past2 = b;
        Draw16xTiles();
    }
    private void tileImage_PointerPressed(object sender, PointerPressedEventArgs e)
    {
        var p = e.GetPosition(MainWindow.window.tile32E.tileImage);
        int x = (int)p.X;
        int y = (int)p.Y;
        int cX = SNES.GetSelectedTile(x, tileImage.Bounds.Width, 8);
        int cY = SNES.GetSelectedTile(y, tileImage.Bounds.Height, 32);
        int id = cX + (cY * 8);
        if ((uint)id > 0xFF)
            id = 0xFF;
        id += page * 0x100;

        if (id > (Const.Tile32Count[Level.Id, Level.BG]) - 1)
            return;
        //New Valid Tile
        selectedTile = id;

        int offset = Level.Tile32DataOffset;
        UpdateTile32Ints(offset);
        DrawTile();
        tileInt.Value = selectedTile;
        ChangePageTxt();
        DrawTiles();
        Draw16xTiles();
    }
    private void x16Image_PointerPressed(object sender, PointerPressedEventArgs e)
    {
        PointerPoint p = e.GetCurrentPoint(x16Image);
        PointerPointProperties properties = p.Properties;
        int x = (int)p.Position.X;
        int y = (int)p.Position.Y;
        int cX = SNES.GetSelectedTile(x, x16Image.Bounds.Width, 16);
        int cY = SNES.GetSelectedTile(y, x16Image.Bounds.Height, 16);
        int id = cX + (cY * 16);
        if ((uint)id > 0xFF)
            id = 0xFF;
        id += page2 * 0x100;

        if (id > (Const.Tile16Count[Level.Id, Level.BG]) - 1)
            return;
        //New Valid Inner Tile
        if (properties.IsRightButtonPressed)
        {
            //MainWindow.window.tile16E.tileInt.Value = id; //select Tile in 16x16 Tile Editor
            return;
        }
        int offset = Level.Tile32DataOffset;
        if (MainWindow.undos.Count == Const.MaxUndo)
            MainWindow.undos.RemoveAt(0);
        MainWindow.undos.Add(Undo.CreateTile32Undo((ushort)selectedTile, BinaryPrimitives.ReadUInt64LittleEndian(SNES.rom.AsSpan(offset + selectedTile * 8))));
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
        DrawTiles();
        Draw16xTiles();

        MainWindow.window.layoutE.DrawLayout();
        MainWindow.window.layoutE.DrawScreen();
        MainWindow.window.enemyE.DrawLayout();
        MainWindow.window.screenE.DrawScreen();
        MainWindow.window.screenE.DrawTiles();
        MainWindow.window.screenE.DrawTile();
    }
    private void TileGridBtn_Click(object sender, RoutedEventArgs e)
    {
        ShowTile32Grid = !ShowTile32Grid;
        DrawTiles();
    }
    private void x16GridBtn_Click(object sender, RoutedEventArgs e)
    {
        ShowTile16Grid = !ShowTile16Grid;
        Draw16xTiles();
    }
    private void tileInt_ValueChanged(object sender, int e)
    {
        if (SNES.rom == null)
            return;
        if (selectedTile == e)
            return;

        selectedTile = e;

        int offset = Level.Tile32DataOffset;

        MainWindow.window.tile32E.topLeftInt.Value = BitConverter.ToUInt16(SNES.rom, offset + selectedTile * 8 + 0);
        MainWindow.window.tile32E.topRightInt.Value = BitConverter.ToUInt16(SNES.rom, offset + selectedTile * 8 + 2);
        MainWindow.window.tile32E.bottomLeftInt.Value = BitConverter.ToUInt16(SNES.rom, offset + selectedTile * 8 + 4);
        MainWindow.window.tile32E.bottomRightInt.Value = BitConverter.ToUInt16(SNES.rom, offset + selectedTile * 8 + 6);
        DrawTile();
        DrawTiles();
        ChangePageTxt();
        DrawTiles();
        Draw16xTiles();
    }
    private void topLeftInt_ValueChanged(object sender, int e)
    {
        if (SNES.rom == null)
            return;

        int offset = Level.Tile32DataOffset;
        if ((ushort)e == BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(offset + selectedTile * 8 + 0)))
            return;

        BinaryPrimitives.WriteUInt16LittleEndian(SNES.rom.AsSpan(offset + selectedTile * 8 + 0), (ushort)e);
        SNES.edit = true;
        DrawTile();
        DrawTiles();
        Draw16xTiles();

        MainWindow.window.layoutE.DrawLayout();
        MainWindow.window.layoutE.DrawScreen();
        MainWindow.window.enemyE.DrawLayout();
        MainWindow.window.screenE.DrawScreen();
        MainWindow.window.screenE.DrawTiles();
        MainWindow.window.screenE.DrawTile();
    }
    private void topRightInt_ValueChanged(object sender, int e)
    {
        if (SNES.rom == null)
            return;

        int offset = Level.Tile32DataOffset;
        if ((ushort)e == BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(offset + selectedTile * 8 + 2)))
            return;

        if (MainWindow.undos.Count == Const.MaxUndo)
            MainWindow.undos.RemoveAt(0);
        MainWindow.undos.Add(Undo.CreateTile32Undo((ushort)selectedTile, BinaryPrimitives.ReadUInt64LittleEndian(SNES.rom.AsSpan(offset + selectedTile * 8))));

        BinaryPrimitives.WriteUInt16LittleEndian(SNES.rom.AsSpan(offset + selectedTile * 8 + 2), (ushort)e);
        SNES.edit = true;
        DrawTile();
        DrawTiles();
        Draw16xTiles();

        MainWindow.window.layoutE.DrawLayout();
        MainWindow.window.layoutE.DrawScreen();
        MainWindow.window.enemyE.DrawLayout();
        MainWindow.window.screenE.DrawScreen();
        MainWindow.window.screenE.DrawTiles();
        MainWindow.window.screenE.DrawTile();
    }
    private void bottomLeftInt_ValueChanged(object sender, int e)
    {
        if (SNES.rom == null)
            return;

        int offset = Level.Tile32DataOffset;
        if ((ushort)e == BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(offset + selectedTile * 8 + 4)))
            return;

        if (MainWindow.undos.Count == Const.MaxUndo)
            MainWindow.undos.RemoveAt(0);
        MainWindow.undos.Add(Undo.CreateTile32Undo((ushort)selectedTile, BinaryPrimitives.ReadUInt64LittleEndian(SNES.rom.AsSpan(offset + selectedTile * 8))));

        BinaryPrimitives.WriteUInt16LittleEndian(SNES.rom.AsSpan(offset + selectedTile * 8 + 4), (ushort)e);
        SNES.edit = true;
        DrawTile();
        DrawTiles();
        Draw16xTiles();

        MainWindow.window.layoutE.DrawLayout();
        MainWindow.window.layoutE.DrawScreen();
        MainWindow.window.enemyE.DrawLayout();
        MainWindow.window.screenE.DrawScreen();
        MainWindow.window.screenE.DrawTiles();
        MainWindow.window.screenE.DrawTile();
    }
    private void bottomRightInt_ValueChanged(object sender, int e)
    {
        if (SNES.rom == null)
            return;

        int offset = Level.Tile32DataOffset;
        if ((ushort)e == BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(offset + selectedTile * 8 + 6)))
            return;

        if (MainWindow.undos.Count == Const.MaxUndo)
            MainWindow.undos.RemoveAt(0);
        MainWindow.undos.Add(Undo.CreateTile32Undo((ushort)selectedTile, BinaryPrimitives.ReadUInt64LittleEndian(SNES.rom.AsSpan(offset + selectedTile * 8))));

        BinaryPrimitives.WriteUInt16LittleEndian(SNES.rom.AsSpan(offset + selectedTile * 8 + 6), (ushort)e);
        SNES.edit = true;
        DrawTile();
        DrawTiles();
        Draw16xTiles();

        MainWindow.window.layoutE.DrawLayout();
        MainWindow.window.layoutE.DrawScreen();
        MainWindow.window.enemyE.DrawLayout();
        MainWindow.window.screenE.DrawScreen();
        MainWindow.window.screenE.DrawTiles();
        MainWindow.window.screenE.DrawTile();
    }
    private void tileImageS_PointerPressed(object sender, PointerPressedEventArgs e) //select 16x16 tile within the selected 32x32 tile
    {
        PointerPoint p = e.GetCurrentPoint(tileImageS);
        PointerPointProperties properties = p.Properties;
        int x = (int)p.Position.X;
        int y = (int)p.Position.Y;
        int cX = SNES.GetSelectedTile(x,tileImageS.Width, 2);
        int cY = SNES.GetSelectedTile(y,tileImageS.Height, 2);

        if (properties.IsRightButtonPressed) //TODO: make it select the 16x16 tile into the 16x16 tile editor
        {

        }
        else
        {
            selectedInnerTile = cX + (cY * 2);
            DrawTile();
            Draw16xTiles();
        }
    }
    private void tileImage_MeasureEvent(object? sender, Size e)
    {
        const double srcW = 256;
        const double srcH = 1024;

        if (MainWindow.settings.UseFixedScale)
        {
            tileImage.MeasuredSize = new Size(srcW * MainWindow.settings.Tile32Scale, srcH * MainWindow.settings.Tile32Scale);
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

            for (int y = 0; y < 32; y++)
            {
                for (int x = 0; x < 8; x++)
                {
                    int tileId32 = x + (y * 8) + (page * 0x100);
                    if (tileId32 > (Const.Tile32Count[Level.Id, Level.BG] - 1))
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
        {
            rect = new Rect(0, 0, 256 * MainWindow.settings.Tile32Scale, 1024 * MainWindow.settings.Tile32Scale);
        }

        SKRect destRect = new SKRect(0, 0, (float)rect.Width, (float)rect.Height);
        canvas.DrawBitmap(tileBMP, destRect);

        if (ShowTile32Grid)
            MainWindow.DrawGrid(canvas, destRect.Width, destRect.Height, 8, 32);

        if (page == ((selectedTile >> 8) & 0xFF))
            MainWindow.DrawSelect(canvas, destRect.Width, destRect.Height, 8, 32, selectedTile & 0x7, (selectedTile >> 3) & 0x1F);
    }
    private void x16Image_MeasureEvent(object? sender, Size e)
    {
        if (MainWindow.settings.UseFixedScale)
        {
            double fixedDimension = MainWindow.settings.Tile32Image16Scale * 256;
            x16Image.MeasuredSize = new Size(fixedDimension, fixedDimension);
            return;
        }

        Size size = new Size(256, 256);
        if (e.Width < 256 || e.Height < 256)
        {
            x16Image.MeasuredSize = size;
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

            x16Image.MeasuredSize = newSize;
        }
    }
    private void x16Image_RenderEvent(object? sender, SkiaCanvasEventArgs e)
    {
        unsafe
        {
            int stride = x16BMP.RowBytes;
            nint ptr = x16BMP.GetPixels();

            for (int y = 0; y < 16; y++)
            {
                for (int x = 0; x < 16; x++)
                {
                    int id = x + (y * 16) + (page2 * 0x100);
                    if (id > (Const.Tile16Count[Level.Id, Level.BG] - 1))
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
            double fixedDimension = MainWindow.settings.Tile32Image16Scale * 256;
            rect = new Rect(0, 0, fixedDimension, fixedDimension);
        }

        SKRect destRect = new SKRect(0, 0, (float)rect.Width, (float)rect.Height);
        canvas.DrawBitmap(x16BMP, destRect);

        if (ShowTile16Grid)
            MainWindow.DrawGrid(canvas, destRect.Width, destRect.Height, 16, 16);

        int offset = Level.Tile32DataOffset;
        ushort tile16 = BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(offset + selectedTile * 8 + (selectedInnerTile * 2)));

        if (page2 == ((tile16 >> 8) & 0xFF))
            MainWindow.DrawSelect(canvas, destRect.Width, destRect.Height, 16, 16, tile16 & 0xF, (tile16 >> 4) & 0xF);
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

        MainWindow.DrawSelect(canvas, destRect.Width, destRect.Height, 2, 2, selectedInnerTile & 1, selectedInnerTile >> 1);
    }
    #endregion Events
}