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

namespace TeheManX_Editor.Forms;

public partial class LayoutEditor : UserControl
{
    #region Fields
    internal static bool ShowLayoutGrid;
    static readonly SKBitmap layoutBMP = new SKBitmap(768, 768, SKColorType.Bgra8888, SKAlphaType.Premul);
    static readonly SKBitmap selectBMP = new SKBitmap(256, 256, SKColorType.Bgra8888, SKAlphaType.Premul);
    #endregion Fields

    #region Properties
    public bool update;
    public int viewerX = 0;
    public int viewerY = 0;
    public int selectedScreen = 2;
    public Button pastLayer;
    #endregion Properties

    #region Constructors
    public LayoutEditor()
    {
        InitializeComponent();
    }
    #endregion Constructors

    #region Methods
    public void DrawLayout()
    {
        layoutImage.InvalidateVisual();
    }
    public void DrawScreen()
    {
        selectImage.InvalidateVisual();
    }
    public void AssignLimits()
    {
        int screenAmount = Const.ScreenCount[Level.Id, Level.BG] - 1;
        MainWindow.window.layoutE.screenInt.Maximum = screenAmount;
        if (MainWindow.window.layoutE.screenInt.Value > screenAmount)
            MainWindow.window.layoutE.screenInt.Value = screenAmount;

        DrawLayout();
        DrawScreen();
    }
    public void UpdateBtn()
    {
        if (pastLayer != null)
            pastLayer.Background = Brushes.Transparent;

        if (Level.BG == 0)
        {
            btn1.Background = Brushes.LightBlue;
            pastLayer = btn1;
        }
        else
        {
            btn2.Background = Brushes.LightBlue;
            pastLayer = btn2;
        }
    }
    #endregion Methods

    #region Events
    private void layoutImage_MeasureEvent(object? sender, Size e)
    {
        double width = e.Width;
        double height = e.Height;

        if (MainWindow.settings.UseFixedScale)
        {
            double fixedDimension = MainWindow.settings.LayoutScale * 256 * 3;
            layoutImage.MeasuredSize = new Size(fixedDimension, fixedDimension);
            return;
        }

        // Handle Infinity cases
        if (double.IsInfinity(width) && double.IsInfinity(height))
            width = height = 768;
        else if (double.IsInfinity(width))
            width = height;
        else if (double.IsInfinity(height))
            height = width;

        double dimension = Math.Max(width, height);
        Size newSize = new Size(dimension, dimension);

        layoutImage.MeasuredSize = newSize;
    }
    private void layoutImage_RenderEvent(object? sender, SkiaCanvasEventArgs e)
    {
        unsafe
        {
            int stride = layoutBMP.RowBytes;
            nint ptr = layoutBMP.GetPixels();
            for (int y = 0; y < 3; y++)
            {
                for (int x = 0; x < 3; x++)
                {
                    Level.DrawScreen(Level.Layout[Level.Id, Level.BG, ((viewerY >> 8) + y) * 32 + ((viewerX >> 8) + x)], x * 256, y * 256, stride, ptr);
                }
            }
        }

        SKCanvas canvas = e.Canvas;
        Rect rect = e.Bounds;

        if (rect.Width < 768 || rect.Height < 768)
            rect = new Rect(0, 0, 768, 768);

        if (MainWindow.settings.UseFixedScale)
        {
            double fixedDimension = MainWindow.settings.LayoutScale * 256 * 3;
            rect = new Rect(0, 0, fixedDimension, fixedDimension);
        }

        SKRect destRect = new SKRect(0, 0, (float)rect.Width, (float)rect.Height);
        canvas.DrawBitmap(layoutBMP, destRect);

        if (ShowLayoutGrid)
            MainWindow.DrawGrid(canvas, destRect.Width, destRect.Height, 3, 3);

        string cameraText = $"X:{viewerX >> 8:X2} Y:{viewerY >> 8:X2}";

        canvas.DrawText(cameraText, 5, 0 - EnemyEditor.labelBigStrokePaint.FontMetrics.Ascent, EnemyEditor.labelBigStrokePaint);
        canvas.DrawText(cameraText, 5, 0 - EnemyEditor.labelBigFillPaint.FontMetrics.Ascent, EnemyEditor.labelBigFillPaint);
    }
    private void layoutImage_PointerPressedEvent(object? sender, PointerPressedEventArgs e)
    {
        if (SNES.rom == null) return;

        PointerPoint p = e.GetCurrentPoint(layoutImage);
        PointerPointProperties properties = p.Properties;
        int x = (int)p.Position.X;
        int y = (int)p.Position.Y;
        int cX = SNES.GetSelectedTile(x, layoutImage.Bounds.Width, 3);
        int cY = SNES.GetSelectedTile(y, layoutImage.Bounds.Height, 3);
        int offsetX = (MainWindow.window.layoutE.viewerX >> 8) + cX;
        int offsetY = (MainWindow.window.layoutE.viewerY >> 8) + cY;
        int i = (offsetY * 32) + offsetX;
        if (properties.IsRightButtonPressed)
        {
            if (e.KeyModifiers == KeyModifiers.Shift)
            {
                int screen = Level.Layout[Level.Id, Level.BG, i];
                if (!MainWindow.window.screenE.mode16)
                    MainWindow.window.screenE.screenInt.Value = screen;
                else
                    MainWindow.window.screenE.screenInt16.Value = screen;
                return;
            }
            screenInt.Value = Level.Layout[Level.Id, Level.BG, i];
            DrawScreen();
        }
        else
        {
            if (Level.Layout[Level.Id, Level.BG, i] == screenInt.Value)
                return;
            
            //Save Undo & Edit
            if (MainWindow.undos.Count == Const.MaxUndo)
                MainWindow.undos.RemoveAt(0);
            MainWindow.undos.Add(Undo.CreateLayoutUndo(i));

            Level.Layout[Level.Id, Level.BG, i] = (byte)(selectedScreen & 0xFF);
            SNES.edit = true;
            DrawLayout();
            MainWindow.window.enemyE.DrawLayout();
            if (LayoutWindow.isOpen)
                MainWindow.layoutWindow.UpdateLayoutGrid();
        }
    }
    private void selectImage_MeasureEvent(object? sender, Size e)
    {
        double width = e.Width;
        double height = e.Height;

        if (MainWindow.settings.UseFixedScale)
        {
            double fixedDimension = MainWindow.settings.LayoutScreenScale * 256;
            selectImage.MeasuredSize = new Size(fixedDimension, fixedDimension);
            return;
        }

        // Handle Infinity cases
        if (double.IsInfinity(width) && double.IsInfinity(height))
            width = height = 256;
        else if (double.IsInfinity(width))
            width = height;
        else if (double.IsInfinity(height))
            height = width;

        double dimension = Math.Max(width, height);
        Size newSize = new Size(dimension, dimension);

        selectImage.MeasuredSize = newSize;
    }
    private void selectImage_RenderEvent(object? sender, SkiaCanvasEventArgs e)
    {
        unsafe
        {
            int stride = selectBMP.RowBytes;
            nint ptr = selectBMP.GetPixels();
            Level.DrawScreen(selectedScreen, stride, ptr);
        }
        SKCanvas canvas = e.Canvas;

        Rect rect = e.Bounds;

        if (rect.Width < 256 || rect.Height < 256)
            rect = new Rect(0, 0, 256, 256);

        if (MainWindow.settings.UseFixedScale)
        {
            double fixedDimension = MainWindow.settings.LayoutScreenScale * 256;
            rect = new Rect(0, 0, fixedDimension, fixedDimension);
        }

        SKRect destRect = new SKRect(0, 0, (float)rect.Width, (float)rect.Height);
        canvas.DrawBitmap(selectBMP, destRect);
    }
    private void screenInt_ValueChanged(object? sender, int newValue)
    {
        if (SNES.rom == null || selectedScreen == newValue)
            return;
        selectedScreen = newValue;
        DrawScreen();
    }
    private void gridBtn_Click(object? sender, RoutedEventArgs e)
    {
        ShowLayoutGrid = !ShowLayoutGrid;
        DrawLayout();
    }
    private void LayerButton_Click(object? sender, RoutedEventArgs e)
    {
        var b = (Button)sender;
        int i = Convert.ToInt32(b.Content.ToString(), 16) - 1;
        if (Level.BG == i)
            return;
        Level.BG = i;
        if (pastLayer != null)
            pastLayer.Background = Brushes.Transparent;
        b.Background = Brushes.LightBlue;
        pastLayer = b;
        Level.AssignOffsets();
        MainWindow.window.layoutE.AssignLimits();
        MainWindow.window.screenE.AssignLimits();
        MainWindow.window.tile32E.AssignLimits();
        MainWindow.window.tile16E.AssignLimits();
        MainWindow.window.enemyE.DrawLayout();
        if (LayoutWindow.isOpen)
            MainWindow.layoutWindow.UpdateLayoutGrid();
    }
    private void ViewScreens_Click(object? sender, RoutedEventArgs e)
    {
        if (LayoutWindow.isOpen)
            return;
        MainWindow.layoutWindow = new LayoutWindow();
        MainWindow.layoutWindow.Show();
    }
    private async void Help_Click(object sender, RoutedEventArgs e)
    {
        HelpWindow h = new HelpWindow(1);
        await h.ShowDialog(MainWindow.window);
    }
    private async void SnapButton_Click(object sender, RoutedEventArgs e)
    {
        IStorageProvider storageProvider = MainWindow.window.StorageProvider;
        IStorageFile file = await storageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Select Level Layout Save Location",
            FileTypeChoices = [new FilePickerFileType("PNG") { Patterns = ["*.png"] }],
            ShowOverwritePrompt = true
        });

        if (file != null)
        {
            WriteableBitmap fileBmp = new WriteableBitmap(new PixelSize(256 * 32, 256 * 32), new Vector(96, 96), PixelFormats.Bgr32, null);

            using(var fb = fileBmp.Lock())
            {
                IntPtr ptr = fb.Address;
                int stride = fb.RowBytes;
                for (int y = 0; y < 32; y++) //32 Screens  Tall
                {
                    for (int x = 0; x < 32; x++) //32 Screens Wide
                    {
                        byte screen = Level.Layout[Level.Id, Level.BG, (y * 32) + x];
                        Level.DrawScreen(screen, x * 256, y * 256, stride, ptr);
                    }
                }
            }
            using (var fs = System.IO.File.Create(file.Path.LocalPath))
            {
                fileBmp.Save(fs);
            }
            await MessageBox.Show(MainWindow.window, "Layout Exported");
        }
    }
    private void UpButton_Click(object sender, RoutedEventArgs e)
    {
        if (MainWindow.window.layoutE.viewerY != 0)
        {
            MainWindow.window.layoutE.viewerY -= 0x100;
            MainWindow.window.layoutE.DrawLayout();
        }
    }
    private void DownButton_Click(object sender, RoutedEventArgs e)
    {
        if ((MainWindow.window.layoutE.viewerY >> 8) < (32 - 3))
        {
            MainWindow.window.layoutE.viewerY += 0x100;
            MainWindow.window.layoutE.DrawLayout();
        }
    }
    private void LeftButton_Click(object sender, RoutedEventArgs e)
    {
        if (MainWindow.window.layoutE.viewerX != 0)
        {
            MainWindow.window.layoutE.viewerX -= 0x100;
            MainWindow.window.layoutE.DrawLayout();
        }
    }
    private void RightButton_Click(object sender, RoutedEventArgs e)
    {
        if ((MainWindow.window.layoutE.viewerX >> 8) < (32 - 3))
        {
            MainWindow.window.layoutE.viewerX += 0x100;
            MainWindow.window.layoutE.DrawLayout();
        }
    }
    #endregion Events
}