using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using SkiaSharp;
using System;

namespace TeheManX_Editor.Forms;

public partial class LayoutWindow : Window
{
    #region Constants
    const int CellWidth = 25;
    const int CellHeight = 22;
    const int GridLineWidth = 2;
    public static readonly SKPaint cellTextPaint = new SKPaint() { Color = SKColors.White, TextSize = 15, Typeface = SKTypeface.FromFamilyName("Consolas"), IsAntialias = true };
    public static readonly SKPaint cellBackPaint = new SKPaint() { Color = new SKColor(0x10, 0x10, 0x10), Style = SKPaintStyle.Fill, IsAntialias = false };
    public static readonly SKPaint cell2BackPaint = new SKPaint() { Color = new SKColor(0x2F, 0x4F, 0x4F), Style = SKPaintStyle.Fill, IsAntialias = false };
    #endregion Constants

    #region Fields
    public static bool isOpen;
    public static double layoutWidth = double.NaN;
    public static double layoutHeight;
    public static int layoutLeft;
    public static int layoutTop;
    public static int layoutState;
    #endregion Fields

    #region Constructor
    public LayoutWindow()
    {
        InitializeComponent();
        if (!double.IsNaN(layoutWidth))
        {
            Width = layoutWidth;
            Height = layoutHeight;
            Position = new PixelPoint(layoutLeft, layoutTop);
            WindowState = (WindowState)layoutState;
        }
    }
    #endregion Constructor

    #region Methods
    internal void UpdateLayoutGrid()
    {
        layoutCanvas.InvalidateVisual();
    }
    #endregion Methods

    #region Events
    private void layoutCanvas_MeasureEvent(object? sender, Size e)
    {
        layoutCanvas.MeasuredSize = new Size(893, 795);
    }
    private void layoutCanvas_RenderEvent(object? sender, SkiaCanvasEventArgs e)
    {
        SKCanvas canvas = e.Canvas;
        e.Canvas.Clear(SKColors.Black);

        int drawY = GridLineWidth;

        for (int y = 0; y < 33; y++)
        {

            int drawX = GridLineWidth;

            for (int x = 0; x < 33; x++)
            {
                canvas.Save();

                SKPaint cellPaint;
                string cellText;

                if (x == 0 || y == 0)
                {
                    cellPaint = cell2BackPaint;
                    cellText = (x == 0 && y == 0) ? "" : (x == 0) ? (y - 1).ToString("X2") : (x - 1).ToString("X2");
                }
                else
                {
                    cellPaint = cellBackPaint;
                    cellText = Level.Layout[Level.Id, Level.BG, x - 1 + (y - 1) * 32].ToString("X");
                }
                canvas.ClipRect(new SKRect(drawX, drawY, drawX + CellWidth, drawY + CellHeight));
                canvas.DrawRect(drawX, drawY, CellWidth, CellHeight, cellPaint);

                // Measure text
                SKRect textBounds = new SKRect();
                cellTextPaint.MeasureText(cellText, ref textBounds);

                // Compute centered position
                float textX = drawX + (CellWidth - textBounds.Width) / 2f - textBounds.Left;
                float textY = drawY + (CellHeight + textBounds.Height) / 2f - textBounds.Bottom;

                // Draw (already clipped to 16x16)
                canvas.DrawText(cellText, textX, textY, cellTextPaint);

                canvas.Restore();

                drawX += CellWidth + GridLineWidth;
            }
            drawY += CellHeight + GridLineWidth;
        }

    }
    private void layoutCanvas_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        PointerPoint p = e.GetCurrentPoint(layoutCanvas);
        PointerPointProperties properties = p.Properties;
        int clickX = (int)p.Position.X;
        int clickY = (int)p.Position.Y;

        int boundY = GridLineWidth;

        for (int y = 0; y < 33; y++)
        {

            int boundX = GridLineWidth;

            for (int x = 0; x < 33; x++)
            {
                if (clickX >= boundX && clickX < (boundX + CellWidth) && clickY >= boundY && clickY < (boundY + CellHeight))
                {
                    int camX;
                    int camY;
                    if (!properties.IsRightButtonPressed)
                    {
                        camX = MainWindow.window.layoutE.viewerX;
                        camY = MainWindow.window.layoutE.viewerY;
                    }
                    else
                    {
                        camX = MainWindow.window.enemyE.viewerX;
                        camY = MainWindow.window.enemyE.viewerY;
                    }

                    if (x == 0 || y == 0) //Only Set X or Y
                    {
                        if (x == 0) //Only Set Y
                            camY = (y - 1) * 0x100;
                        else
                            camX = (x - 1) * 0x100;
                    }
                    else
                    {
                        camX = (x - 1) * 0x100;
                        camY = (y - 1) * 0x100;
                    }

                    if (!properties.IsRightButtonPressed)
                    {
                        MainWindow.window.layoutE.viewerX = Math.Clamp(camX, 0, 0x1D00);
                        MainWindow.window.layoutE.viewerY = Math.Clamp(camY, 0, 0x1D00);
                        MainWindow.window.layoutE.DrawLayout();
                    }
                    else
                    {
                        MainWindow.window.enemyE.viewerX = camX;
                        MainWindow.window.enemyE.viewerY = camY;
                        MainWindow.window.enemyE.DrawLayout();
                    }
                    return;
                }

                boundX += CellWidth + GridLineWidth;
            }
            boundY += CellHeight + GridLineWidth;
        }
    }
    private void Window_Opened(object? sender, EventArgs e)
    {
        isOpen = true;
    }
    private void Window_Closing(object? sender, WindowClosingEventArgs e)
    {
        isOpen = false;
        layoutLeft = Position.X;
        layoutTop = Position.Y;
        layoutWidth = Width;
        layoutHeight = Height;
        layoutState = (int)WindowState;
    }
    #endregion Events
}