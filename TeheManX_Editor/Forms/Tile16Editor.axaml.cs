using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using SkiaSharp;
using System;
using System.Buffers.Binary;

namespace TeheManX_Editor.Forms;

public partial class Tile16Editor : UserControl
{
    #region Fields
    public static double scale = 2;
    internal static bool ShowTile16Grid;
    internal static bool ShowVramGrid;
    SKBitmap x16BMP = new SKBitmap(256, 256, SKColorType.Bgra8888, SKAlphaType.Premul);
    SKBitmap vramTiles = new SKBitmap(128, 512, SKColorType.Bgra8888, SKAlphaType.Premul);
    SKBitmap tileBMP_S = new SKBitmap(16, 16, SKColorType.Bgra8888, SKAlphaType.Premul);
    #endregion Fields

    #region Properties
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
    }
    #endregion Constructors

    #region Methods
    public void AssignLimits()
    {
        int max16 = Const.Tile16Count[Level.Id, Level.BG] - 1;
        tileInt.Maximum = max16;
        if (selectedTile > max16)
            tileInt.Value = max16;
        int offset = Level.TileCollisionDataOffset;
        collisionInt.Value = SNES.rom[offset + selectedTile];

        Draw16xTiles();
        DrawVramTiles();
        DrawTile();
        UpdateTileAttributeUI();
    }
    public unsafe void DrawVramTiles()
    {
        vramTileImage.InvalidateVisual();
    }
    public void Draw16xTiles()
    {
        x16Image.InvalidateVisual();
    }
    public void DrawTile()
    {
        tileImageS.InvalidateVisual();
    }
    public void UpdateTileAttributeUI()
    {
        int offset = Level.Tile16DataOffset;

        ushort val = BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(offset + selectedTile * 8 + selectedInnerTile * 2));

        vramInt.Value = (val & 0x3FF);
        palInt.Value = (val >> 10) & 7;
        priorityCheck.IsChecked = (val & 0x2000) != 0;
        flipHCheck.IsChecked = (val & 0x4000) != 0;
        flipVCheck.IsChecked = (val & 0x8000) != 0;
    }
    #endregion Methods

    #region Events
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

        id += page * 0x100;
        if (id > (Const.Tile16Count[Level.Id, Level.BG]) - 1)
            return;

        selectedTile = id;
        tileInt.Value = selectedTile;
        int offset = Level.TileCollisionDataOffset;
        collisionInt.Value = SNES.rom[offset + selectedTile];

        DrawTile();
        Draw16xTiles();
        DrawVramTiles();
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
            past2.Background = Brushes.Transparent;
        b.Background = Brushes.LightBlue;
        past2 = b;
        DrawVramTiles();
        DrawTile();
    }
    private void Page_Click(object sender, RoutedEventArgs e)
    {
        Button b = (Button)sender;
        int i = Convert.ToInt32(b.Content.ToString().Trim(), 16);
        if (page == i)
            return;
        page = i;
        if (past != null)
            past.Background = Brushes.Transparent;
        b.Background = Brushes.LightBlue;
        past = b;
        Draw16xTiles();
    }
    private void tileImageS_PointerPressed(object sender, PointerPressedEventArgs e)
    {
        PointerPoint p = e.GetCurrentPoint(tileImageS);
        PointerPointProperties properties = p.Properties;
        int x = (int)p.Position.X;
        int y = (int)p.Position.Y;
        int cX = SNES.GetSelectedTile(x, tileImageS.Width, 2);
        int cY = SNES.GetSelectedTile(y, tileImageS.Height, 2);

        selectedInnerTile = cX + (cY * 2);
        DrawVramTiles();
        DrawTile();
        UpdateTileAttributeUI();
    }
    private void vramTileImage_PointerPressed(object sender, PointerPressedEventArgs e)
    {
        PointerPoint p = e.GetCurrentPoint(vramTileImage);
        PointerPointProperties properties = p.Properties;
        int x = (int)p.Position.X;
        int y = (int)p.Position.Y;
        int cX = SNES.GetSelectedTile(x, 128 * scale, 16);
        int cY = SNES.GetSelectedTile(y, 512 * scale, 64);

        if (properties.IsLeftButtonPressed)
        {
            int offset = Level.Tile16DataOffset;
            if (MainWindow.undos.Count == Const.MaxUndo)
                MainWindow.undos.RemoveAt(0);
            MainWindow.undos.Add(Undo.CreateTile16Undo((ushort)selectedTile, BinaryPrimitives.ReadUInt64LittleEndian(SNES.rom.AsSpan(offset + selectedTile * 8))));
            offset += selectedTile * 8 + selectedInnerTile * 2;

            ushort val = BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(offset));
            ushort newVal = (ushort)((val & 0xE000) | (cX + (cY * 16)) | (palId << 10));
            BinaryPrimitives.WriteUInt16LittleEndian(SNES.rom.AsSpan(offset), newVal);
            SNES.edit = true;
            DrawTile();
            Draw16xTiles();
            DrawVramTiles();
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
        ShowTile16Grid = !ShowTile16Grid;
        Draw16xTiles();
    }
    private void x8GridBtn_Click(object sender, RoutedEventArgs e)
    {
        ShowVramGrid = !ShowVramGrid;
        DrawVramTiles();
    }
    private void tileInt_ValueChanged(object sender, int e)
    {
        if (SNES.rom == null)
            return;
        if (selectedTile == e)
            return;

        selectedTile = e;
        int offset = Level.TileCollisionDataOffset;
        collisionInt.Value = SNES.rom[offset + selectedTile];
        Draw16xTiles();
        DrawVramTiles();
        UpdateTileAttributeUI();
        DrawTile();
    }
    private void collisionInt_ValueChanged(object sender, int e)
    {
        if (SNES.rom == null)
            return;

        int offset = Level.TileCollisionDataOffset;
        offset += selectedTile;

        byte val = SNES.rom[offset];
        if (val == (byte)e)
            return;

        if (MainWindow.undos.Count == Const.MaxUndo)
            MainWindow.undos.RemoveAt(0);
        MainWindow.undos.Add(Undo.CreateCollisionUndo((ushort)selectedTile, SNES.rom[offset]));

        SNES.rom[offset] = (byte)e;
        SNES.edit = true;
    }
    private void vramInt_ValueChanged(object sender, int e)
    {
        if (SNES.rom == null)
            return;

        int offset = Level.Tile16DataOffset;
        int tileBase = offset + selectedTile * 8;
        offset += selectedTile * 8 + selectedInnerTile * 2;

        ushort val = BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(offset));
        if ((val & 0x3FF) == e)
            return;

        if (MainWindow.undos.Count == Const.MaxUndo)
            MainWindow.undos.RemoveAt(0);
        MainWindow.undos.Add(Undo.CreateTile16Undo((ushort)selectedTile, BinaryPrimitives.ReadUInt64LittleEndian(SNES.rom.AsSpan(tileBase))));

        BinaryPrimitives.WriteUInt16LittleEndian(SNES.rom.AsSpan(offset), (ushort)((val & 0xFC00) | (e)));
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
    private void palInt_ValueChanged(object sender, int e)
    {
        if (SNES.rom == null)
            return;

        int offset = Level.Tile16DataOffset;
        int tileBase = offset + selectedTile * 8;
        offset += selectedTile * 8 + selectedInnerTile * 2;

        ushort val = BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(offset));
        if (((val >> 10) & 7) == e)
            return;

        if (MainWindow.undos.Count == Const.MaxUndo)
            MainWindow.undos.RemoveAt(0);
        MainWindow.undos.Add(Undo.CreateTile16Undo((ushort)selectedTile, BinaryPrimitives.ReadUInt64LittleEndian(SNES.rom.AsSpan(tileBase))));

        BinaryPrimitives.WriteUInt16LittleEndian(SNES.rom.AsSpan(offset), (ushort)((val & 0xE3FF) | (e << 10)));
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

        int offset = Level.Tile16DataOffset;
        int tileBase = offset + selectedTile * 8;
        offset += selectedTile * 8 + selectedInnerTile * 2;

        ushort val = BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(offset));

        if ((((val & 0x2000) != 0) ? 1 : 0) == ((priorityCheck.IsChecked == true) ? 1 : 0))
            return;

        if (MainWindow.undos.Count == Const.MaxUndo)
            MainWindow.undos.RemoveAt(0);
        MainWindow.undos.Add(Undo.CreateTile16Undo((ushort)selectedTile, BinaryPrimitives.ReadUInt64LittleEndian(SNES.rom.AsSpan(tileBase))));

        BinaryPrimitives.WriteUInt16LittleEndian(SNES.rom.AsSpan(offset), (ushort)(val ^ 0x2000));
        SNES.edit = true;
        
        //The editor doesnt show anything when it comes to priority so we wont upate the UI
    }
    private void flipHCheck_CheckChange(object sender, RoutedEventArgs e)
    {
        if (SNES.rom == null)
            return;

        int offset = Level.Tile16DataOffset;
        int tileBase = offset + selectedTile * 8;
        offset += selectedTile * 8 + selectedInnerTile * 2;

        ushort val = BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(offset));

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

        int offset = Level.Tile16DataOffset;
        int tileBase = offset + selectedTile * 8;
        offset += selectedTile * 8 + selectedInnerTile * 2;

        ushort val = BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(offset));

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
    private void zoomInBtn_Click(object sender, RoutedEventArgs e)
    {
        scale = Math.Clamp(scale + 1, 1, Const.MaxScaleUI);
        vramTileImage.InvalidateMeasure();
    }
    private void zoomOutBtn_Click(object sender, RoutedEventArgs e)
    {
        scale = Math.Clamp(scale - 1, 1, Const.MaxScaleUI);
        vramTileImage.InvalidateMeasure();
    }
    private void x16Image_MeasureEvent(object? sender, Size e)
    {
        if (MainWindow.settings.UseFixedScale)
        {
            double fixedDimension = MainWindow.settings.Tile16Scale * 256;
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
                    int id = x + (y * 16) + (page * 0x100);
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
            double fixedDimension = MainWindow.settings.Tile16Scale * 256;
            rect = new Rect(0, 0, fixedDimension, fixedDimension);
        }

        SKRect destRect = new SKRect(0, 0, (float)rect.Width, (float)rect.Height);
        canvas.DrawBitmap(x16BMP, destRect);

        if (ShowTile16Grid)
            MainWindow.DrawGrid(canvas, destRect.Width, destRect.Height, 16, 16);

        if (page == ((selectedTile >> 8) & 0xFF))
            MainWindow.DrawSelect(canvas, destRect.Width, destRect.Height, 16, 16, selectedTile & 0xF, (selectedTile >> 4) & 0xF);
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

            int set = palId;

            // Pin source data to avoid bounds checks
            fixed (byte* tilesPtr = Level.DecodedTiles)
            fixed (uint* palettePtr = Level.Palette)
            {
                // Base pointer for the active palette (set * 16 colors)
                uint* palBase = palettePtr + (set << 4);

                /*
                 * Draw 0x200 (512) tiles from VRAM
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

                        // Pointer to the top left pixel of this tile in the destination
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
                                dst += 4; // advance one pixel
                            }
                        }
                    }
                }
            }
        }
        SKCanvas canvas = e.Canvas;
        SKRect destRect = new SKRect(0, 0, (float)(scale * 128), (float)(scale * 512));
        canvas.DrawBitmap(vramTiles, destRect);

        if (ShowVramGrid)
            MainWindow.DrawGrid(canvas, destRect.Width, destRect.Height, 16, 64);

        int offset = Level.Tile16DataOffset;

        int vram = BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(offset + selectedTile * 8 + selectedInnerTile * 2)) & 0x3FF;
        MainWindow.DrawSelect(canvas, destRect.Width, destRect.Height, 16, 64, vram & 0xF, (vram >> 4));
    }
    private void tileImageS_MeasureEvent(object? sender, Size e)
    {
        tileImageS.MeasuredSize = new Size(64, 64);
    }
    private void tileImageS_RenderEvent(object? sender, SkiaCanvasEventArgs e)
    {
        unsafe
        {
            Level.Draw16xTile(selectedTile, 0, 0, tileBMP_S.RowBytes, tileBMP_S.GetPixels());
        }
        
        SKCanvas canvas = e.Canvas;
        SKRect destRect = new SKRect(0, 0, 64, 64);
        canvas.DrawBitmap(tileBMP_S, new SKRect(0, 0, 64, 64));

        MainWindow.DrawSelect(canvas, destRect.Width, destRect.Height, 2, 2, selectedInnerTile & 1, selectedInnerTile >> 1);
    }
    #endregion Events
}