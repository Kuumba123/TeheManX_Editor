using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace TeheManX_Editor.Forms;

public partial class EnemyEditor : UserControl
{
    #region Constants
    enum MouseState
    {
        None,
        Pan,            //Pan Camera
        Move,           //Enemy Move
        TriggerSelect   //Select Camera Trigger
    }
    public static readonly SKPaint labelBackPaint = new SKPaint() { Color = SKColors.Black, Style = SKPaintStyle.Fill, IsAntialias = false };
    public static readonly SKPaint labelFrontPaint = new SKPaint() { Color = SKColors.White, TextSize = 13, Typeface = SKTypeface.FromFamilyName("Consolas"), IsAntialias = true };
    public static readonly SKPaint labelBigFillPaint = new SKPaint() { Color = SKColors.White, TextSize = 22, Typeface = SKTypeface.FromFamilyName("Consolas"), IsAntialias = true };
    public static readonly SKPaint labelBigStrokePaint = new SKPaint { Typeface = SKTypeface.FromFamilyName("Consolas"), TextSize = 22, Style = SKPaintStyle.Stroke, StrokeWidth = 1.5f, Color = SKColors.Black, IsAntialias = true };
    public static readonly SKFontMetrics labelMetrics = labelFrontPaint.FontMetrics;
    public static readonly SKTypeface nameFontTypeface = SKTypeface.FromStream(AssetLoader.Open(new Uri("avares://TeheManX_Editor/Resources/unifont-15.0.01.ttf")));
    public static readonly SKPaint nameFillPaint = new SKPaint() { Color = SKColors.White, TextSize = 13, Typeface = nameFontTypeface, IsAntialias = true };
    public static readonly SKPaint nameStrokePaint = new SKPaint { Typeface = nameFontTypeface, TextSize = 13, Style = SKPaintStyle.Stroke, StrokeWidth = 1.5f, Color = SKColors.Black, IsAntialias = true };

    static readonly SKPaint effectLinePaint = new SKPaint { Color = SKColors.Purple, Style = SKPaintStyle.Stroke, StrokeWidth = 1, IsAntialias = false };
    static readonly SKPaint cameraTriggerFillPaint = new SKPaint { Style = SKPaintStyle.Fill, Color = new SKColor(0xAD, 0xD8, 0xE6, 96), IsAntialias = false };
    static readonly SKPaint cameraTriggerStrokePaint = new SKPaint { Style = SKPaintStyle.Stroke, Color = SKColors.Green, StrokeWidth = 1, IsAntialias = false };
    static readonly SKPaint selectFillPaint = new SKPaint { Style = SKPaintStyle.Fill, Color = new SKColor(0x00, 0x78, 0xFF, 64), IsAntialias = false };
    static readonly SKPaint selectStrokePaint = new SKPaint { Style = SKPaintStyle.Stroke, Color = new SKColor(0x00, 0x78, 0xD7), StrokeWidth = 1, IsAntialias = false };

    static readonly SKPaint cameraFillPaint = new SKPaint { Style = SKPaintStyle.Fill, Color = new SKColor(0xFF, 0xFF, 0xFF, 0x40), IsAntialias = false };
    static readonly SKPaint cameraStrokePaint = new SKPaint { Style = SKPaintStyle.Stroke, Color = new SKColor(0xFF, 0xFF, 0xFF), StrokeWidth = 1, IsAntialias = false };

    static readonly SKPaint playerFillPaint = new SKPaint { Style = SKPaintStyle.Fill, Color = new SKColor(0, 0, 0xFF, 0x40), IsAntialias = false };
    static readonly SKPaint playerStrokePaint = new SKPaint { Style = SKPaintStyle.Stroke, Color = new SKColor(0, 0, 0xFF), StrokeWidth = 1, IsAntialias = false };
    static readonly SKPaint playerLandFill = new SKPaint { Style = SKPaintStyle.Fill, Color = SKColors.CadetBlue, IsAntialias = false };

    static readonly SKPaint[] enemyBorderPaints =
    {
    new SKPaint { Color = SKColors.Blue,     Style = SKPaintStyle.Stroke, StrokeWidth = 1, IsAntialias = false },// 0
    new SKPaint { Color = SKColors.Purple,  Style = SKPaintStyle.Stroke, StrokeWidth = 1, IsAntialias = false }, // 1
    new SKPaint { Color = SKColors.HotPink, Style = SKPaintStyle.Stroke, StrokeWidth = 1, IsAntialias = false }, // 2
    new SKPaint { Color = SKColors.Red,     Style = SKPaintStyle.Stroke, StrokeWidth = 1, IsAntialias = false }, // 3
    new SKPaint { Color = SKColors.Green,   Style = SKPaintStyle.Stroke, StrokeWidth = 1, IsAntialias = false }, // 4
    new SKPaint { Color = SKColors.Orange,  Style = SKPaintStyle.Stroke, StrokeWidth = 1, IsAntialias = false }  // 5
    };

    static readonly string[] HexText = Enumerable.Range(0, 256).Select(i => i.ToString("X2")).ToArray();
    static readonly float CharWidth = labelFrontPaint.MeasureText("0");
    static readonly SKFontMetrics Metrics = labelFrontPaint.FontMetrics;
    static readonly float TextHeight = Metrics.Descent - Metrics.Ascent;
    #endregion Constants

    #region Fields
    public static double scale = 2;
    static SKRect selectRect;
    static SKBitmap layoutBMP = new SKBitmap(1920, 1080, SKColorType.Bgra8888, SKAlphaType.Premul);
    #endregion Fields

    #region Properties
    public int viewerX = 0x400;
    public int viewerY = 0;
    public Enemy selectedEnemy;
    MouseState mouseState;
    int mouseStartX;
    int mouseStartY;
    int referanceStartX;
    int referanceStartY;
    #endregion Properties

    #region Constructor
    public EnemyEditor()
    {
        InitializeComponent();
        DragDrop.SetAllowDrop(layoutCanvas, true);
        layoutCanvas.AddHandler(DragDrop.DragOverEvent, layoutCanvas_OnDragOver, RoutingStrategies.Bubble);
        layoutCanvas.AddHandler(DragDrop.DropEvent, layoutCanvas_OnDrop, RoutingStrategies.Bubble);
    }
    #endregion Constructor

    #region Methods
    public void DrawLayout()
    {
        layoutCanvas.InvalidateVisual();
    }
    private void SelectEnemy(Enemy en)
    {
        selectedEnemy = en;
        columnInt.Value = en.Column;
        idInt.Value = en.Id;
        subIdInt.Value = en.SubId;
        typeInt.Value = en.Type;
        xInt.Value = en.X;
        yInt.Value = en.Y;

        idInt.IsEnabled = true;
        subIdInt.IsEnabled = true;
        typeInt.IsEnabled = true;
        xInt.IsEnabled = true;
        yInt.IsEnabled = true;
        columnInt.IsEnabled = true;
    }
    public void DisableSelect() //Disable editing Enemy Properties
    {
        selectedEnemy = null;
        //Disable
        idInt.IsEnabled = false;
        subIdInt.IsEnabled = false;
        typeInt.IsEnabled = false;
        xInt.IsEnabled = false;
        yInt.IsEnabled = false;
        columnInt.IsEnabled = false;
    }
    private async Task<bool> ValidEnemyAdd()
    {
        if (Level.Id >= Const.PlayableLevelsCount || (Const.Id == Const.GameId.MegaManX3 && Level.Id > 0xE))
        {
            await MessageBox.Show(MainWindow.window, "Enemies cannot be added to this level.", "ERROR");
            return false;
        }
        if (Level.Enemies[Level.Id].Count == 0xCC)
        {
            await MessageBox.Show(MainWindow.window, "The max amount of enemies you can put in a level is 0xCC.", "ERROR");
            return false;
        }
        return true;
    }
    public void ZoomIn()
    {
        int oldScale = (int)scale;
        scale = Math.Clamp(scale + 1, 1, Const.MaxScaleUI);

        if (scale == oldScale)
            return;

        float W = (float)layoutCanvas.MeasuredSize.Width;
        float H = (float)layoutCanvas.MeasuredSize.Height;

        float dx = (float)((W / oldScale - W / scale) * 0.5f);
        float dy = (float)((H / oldScale - H / scale) * 0.5f);

        viewerX = (short)Math.Clamp(viewerX + dx, 0, 0x1FFF);
        viewerY = (short)Math.Clamp(viewerY + dy, 0, 0x1FFF);

        layoutCanvas.InvalidateVisual();
    }
    public void ZoomOut()
    {
        int oldScale = (int)scale;
        scale = Math.Clamp(scale - 1, 1, Const.MaxScaleUI);

        if (scale == oldScale)
            return;

        float W = (float)layoutCanvas.MeasuredSize.Width;
        float H = (float)layoutCanvas.MeasuredSize.Height;

        float dx = (float)((W / oldScale - W / scale) * 0.5f);
        float dy = (float)((H / oldScale - H / scale) * 0.5f);

        viewerX = (short)Math.Clamp(viewerX + dx, 0, 0x1FFF);

        viewerY = (short)Math.Clamp(viewerY + dy, 0, 0x1FFF);

        layoutCanvas.InvalidateVisual();
    }
    private async Task ObjectDescriptionMessage(ObjectIcon icon, int id, int type)
    {
        string description = null;
        string name = null;


        if (description == null)
        {
            if (icon != null)
            {
                if (name == null)
                    await MessageBox.Show(MainWindow.window, $"Tile Id - {icon.TileId:X} , Palette Id - {icon.PaletteId:X}");
                else
                    await MessageBox.Show(MainWindow.window, $"Tile Id - {icon.TileId:X} , Palette Id - {icon.PaletteId:X}", name);
            }
        }
    }
    #endregion Methods

    #region Events
    private void layoutCanvas_MeasureEvent(object? sender, Size e)
    {
        layoutCanvas.MeasuredSize = e;
    }
    [SkipLocalsInit]
    private void layoutCanvas_RenderEvent(object? sender, SkiaCanvasEventArgs e)
    {
        if (layoutBMP.Width < e.Bounds.Width || layoutBMP.Height < e.Bounds.Height)
            layoutBMP.TryAllocPixels(new SKImageInfo((int)e.Bounds.Width, (int)e.Bounds.Height, SKColorType.Bgra8888, SKAlphaType.Premul));

        unsafe
        {
            int bmpWidth = layoutBMP.Width;
            int bmpHeight = layoutBMP.Height;
            nint bufferP = layoutBMP.GetPixels();
            int stride = layoutBMP.RowBytes;

            int tileX = viewerX >> 8;   // 256-pixel screen index
            int tileY = viewerY >> 8;
            int offX = viewerX & 0xFF;  // sub-screen offset (0–255)
            int offY = viewerY & 0xFF;

            const int ScreenSize = 256;

            int screenCountY = (int)(((e.Bounds.Height / scale) + ScreenSize - 1) / ScreenSize) + 1;
            int screenCountX = (int)(((e.Bounds.Width / scale) + ScreenSize - 1) / ScreenSize) + 1;

            for (int sy = 0; sy < screenCountY; sy++)
            {
                for (int sx = 0; sx < screenCountX; sx++)
                {
                    int screenIndexX = tileX + sx;
                    int screenIndexY = tileY + sy;

                    // bounds check so we never index outside Layout
                    if (screenIndexX < 0 || screenIndexX >= 32) continue;
                    if (screenIndexY < 0 || screenIndexY >= 32) continue;

                    int layoutIndex = screenIndexY * 32 + screenIndexX;

                    int drawX = sx * 256 - offX;
                    int drawY = sy * 256 - offY;

                    bool fullyInside = drawX >= 0 && drawY >= 0 && (drawX + 256) <= bmpWidth && (drawY + 256) <= bmpHeight;

                    if (fullyInside)
                    {
                        // non-clamped version (no bmpWidth/bmpHeight args)
                        Level.DrawScreen(Level.Layout[Level.Id, Level.BG, layoutIndex], drawX, drawY, stride, bufferP);
                    }
                    else
                    {
                        // partially outside - use clamped version
                        Level.DrawScreen_Clamped(Level.Layout[Level.Id, Level.BG, layoutIndex], drawX, drawY, stride, bufferP, bmpWidth, bmpHeight);
                    }
                }
            }
        }

        SKCanvas canvas = e.Canvas;

        canvas.Clear(SKColors.Black);
        canvas.Save();
        canvas.Scale((float)scale);

        SKRect clipRect = new SKRect(0, 0, 8192 - viewerX, 8192 - viewerY);

        if (!clipRect.IsEmpty)
        {
            canvas.Save();
            canvas.ClipRect(clipRect);
            SKRect destRect = new SKRect(0, 0, layoutBMP.Width, layoutBMP.Height);
            canvas.DrawBitmap(layoutBMP, destRect);
            canvas.Restore();
        }

        if (Level.Id >= Const.PlayableLevelsCount || (Const.Id == Const.GameId.MegaManX3 && Level.Id > 0xE))
        {

        }
        else //TODO: have most of this overlay stuff be configurable via the UI at the top right of the enemy tab
        {
            int id = Level.Id;
            int cameraX = viewerX;
            int cameraY = viewerY;

            for (int i = 0; i < SpawnEditor.Checkpoints[id].Count; i++) //Draw Checkpoints (Camera + MegaMan Location)
            {
                int drawX = SpawnEditor.Checkpoints[id][i].CameraX + -cameraX;
                int drawY = SpawnEditor.Checkpoints[id][i].CameraY + -cameraY;

                canvas.DrawRect(drawX, drawY, 256, 224, cameraFillPaint);
                canvas.DrawRect(drawX + 0.5f, drawY + 0.5f, 255, 223, cameraStrokePaint);

                drawX = SpawnEditor.Checkpoints[id][i].MegaX + -cameraX;
                drawY = SpawnEditor.Checkpoints[id][i].MegaY + -cameraY;

                const int MegaOffsetX = 0;
                const int MegaOffsetY = -1;
                const int MegaWidth = 6;
                const int MegaHeight = 14;

                canvas.DrawRect(drawX + MegaOffsetX, drawY + MegaOffsetY, MegaWidth, MegaHeight, playerFillPaint);
                canvas.DrawRect(drawX + MegaOffsetX + 0.5f, drawY + MegaOffsetY + 0.5f, MegaWidth, MegaHeight, playerStrokePaint);

                int collisionTimer = SpawnEditor.Checkpoints[id][i].CollisionTimer;

                if (collisionTimer != 0)
                    canvas.DrawRect(drawX + MegaWidth / 2, drawY, 1, 8 * collisionTimer, playerLandFill);
            }

            for (int i = 0; i < Level.Enemies[id].Count; i++) //Draw Enemies
            {
                Enemy en = Level.Enemies[id][i];
                int drawX = en.X + -cameraX;
                int drawY = en.Y + -cameraY;

                ObjectIcon icon = Level.GetObjectIcon(en.Id, en.Type);

                if (icon != null)
                {
                    icon.DrawCentre(canvas, drawX, drawY + 1); //Have to add 1 cause of weird OAM qwerk
                }
                else
                {
                    canvas.Save(); //Save Canvas State

                    canvas.ClipRect(new SKRect(drawX, drawY, drawX + 16, drawY + 16));
                    canvas.DrawRect(drawX, drawY, 16, 16, labelBackPaint);

                    string text = HexText[en.Id];

                    float textWidth = 2 * CharWidth; // always 2 chars
                    float x = drawX + (16 - textWidth) / 2f;
                    float y = drawY + (16 - TextHeight) / 2f - Metrics.Ascent;

                    canvas.DrawText(text, x, y, labelFrontPaint);

                    if (en.Type < 5)
                        canvas.DrawRect(drawX + 0.5f, drawY + 0.5f, 15, 15, enemyBorderPaints[en.Type]);

                    canvas.Restore(); //Restore Canvas State

                    string name = Level.GetObjectName(en.Id, en.Type);

                    if (name != null)
                    {
                        canvas.DrawText(name, drawX, (drawY - nameStrokePaint.FontMetrics.Ascent) + 16, nameStrokePaint);
                        canvas.DrawText(name, drawX, (drawY - nameFillPaint.FontMetrics.Ascent) + 16, nameFillPaint);
                    }
                }
            }

            if (MainWindow.window.camE.triggersEnabled) //Draw Camera Triggers
            {
                for (int i = 0; i < CameraEditor.CameraTriggers[id].Count; i++)
                {
                    int rightSide = CameraEditor.CameraTriggers[id][i].RightSide;
                    int leftSide = CameraEditor.CameraTriggers[id][i].LeftSide;
                    int bottomSide = CameraEditor.CameraTriggers[id][i].BottomSide;
                    int topSide = CameraEditor.CameraTriggers[id][i].TopSide;

                    int width = rightSide - leftSide;
                    int height = bottomSide - topSide;

                    if (width < 1) width = 1;
                    if (height < 1) height = 1;

                    int drawX = leftSide + -cameraX;
                    int drawY = topSide + -cameraY;
                    canvas.DrawRect(drawX, drawY, width, height, cameraTriggerFillPaint);
                    canvas.DrawRect(drawX + 0.5f, drawY + 0.5f, width - 1, height - 1, cameraTriggerStrokePaint);
                }
            }

            for (int i = 0; i < Level.Enemies[id].Count; i++) //Draw Object-Tile/BG-Tile/Palette Swap Trigger
            {
                Enemy en = Level.Enemies[id][i];
                if (en.Type != 2)
                    continue;

                if (en.Id >= 0x15 && en.Id <= 0x17) //Horizontal
                {
                    int drawX = en.X - cameraX - 1;
                    int drawY1 = en.Y - cameraY - 112;
                    int height = 224;
                    canvas.DrawRect(drawX, drawY1, 1, height, effectLinePaint);
                }
                else if (en.Id >= 0x18 && en.Id <= 0x1A) //Vertical
                {
                    int drawX1 = en.X - cameraX - 128;
                    int drawY = en.Y - cameraY - 1;
                    int width = 256;
                    canvas.DrawRect(drawX1, drawY, width, 1, effectLinePaint);
                }
            }

            if (mouseState == MouseState.TriggerSelect)
            {
                canvas.DrawRect(selectRect, selectFillPaint);
                canvas.DrawRect(selectRect.Left + 0.5f, selectRect.Top + 0.5f, selectRect.Width - 1, selectRect.Height - 1, selectStrokePaint);
            }
        }

        canvas.Restore();

        string cameraText = $"X:{viewerX:X4} Y:{viewerY:X4}";

        canvas.DrawText(cameraText, 5, 0 - labelBigStrokePaint.FontMetrics.Ascent, labelBigStrokePaint);
        canvas.DrawText(cameraText, 5, 0 - labelBigFillPaint.FontMetrics.Ascent, labelBigFillPaint);
    }
    private async void layoutCanvas_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        PointerPoint pos = e.GetCurrentPoint(layoutCanvas);
        PointerPointProperties properties = pos.Properties;

        if (properties.IsMiddleButtonPressed)
        {
            mouseStartX = (int)(pos.Position.X / scale);
            mouseStartY = (int)(pos.Position.Y / scale);
            referanceStartX = viewerX;
            referanceStartY = viewerY;
            mouseState = MouseState.Pan;

            int leftSide = Math.Clamp(Math.Min(mouseStartX + -viewerX, mouseStartX + -viewerX), 0, 0x1FFF);
            int rightSide = Math.Clamp(Math.Max(mouseStartX + -viewerX, mouseStartX + -viewerX), 0, 0x1FFF);
            int topSide = Math.Clamp(Math.Min(mouseStartY + -viewerY, mouseStartY + -viewerY), 0, 0x1FFF);
            int bottomSide = Math.Clamp(Math.Max(mouseStartY + -viewerY, mouseStartY + -viewerY), 0, 0x1FFF);

            selectRect = new SKRect(leftSide, topSide, rightSide, bottomSide);

            Cursor = new Cursor(StandardCursorType.SizeAll);
            return;
        }

        if (Level.Id >= Const.PlayableLevelsCount || (Const.Id == Const.GameId.MegaManX3 && Level.Id > 0xE))
            return;

        if (properties.IsLeftButtonPressed && (bool)MainWindow.window.camE.triggerCheck.IsChecked && MainWindow.window.camE.triggersEnabled)
        {
            mouseStartX = ((int)(pos.Position.X / scale) + viewerX);
            mouseStartY = ((int)(pos.Position.Y / scale) + viewerY);
            mouseState = MouseState.TriggerSelect;

            if ((e.KeyModifiers & KeyModifiers.Shift) != 0)
            {
                mouseStartX &= 0xFFF8;
                mouseStartY &= 0xFFF8;
            }
        }
        else if (properties.IsLeftButtonPressed || properties.IsRightButtonPressed)
        {
            bool leftPressed = properties.IsLeftButtonPressed;

            const int DefaultWidth = 16;
            const int DefaultHeight = 16;
            const int DefaultOffsetX = 0;
            const int DefaultOffsetY = 0;

            int id = Level.Id;

            int clickX = (int)(pos.Position.X / scale) + viewerX;
            int clickY = (int)(pos.Position.Y / scale) + viewerY;

            Enemy en;

            for (int i = 0; i < Level.Enemies[id].Count; i++)
            {
                en = Level.Enemies[id][i];

                int width;
                int height;
                int offsetX;
                int offsetY;

                ObjectIcon icon = Level.GetObjectIcon(en.Id, en.Type);

                if (icon != null)
                {
                    width = icon.Width;
                    height = icon.Height;
                    offsetX = -width / 2;
                    offsetY = (-height / 2) + 1;
                }
                else
                {
                    width = DefaultWidth;
                    height = DefaultHeight;
                    offsetX = DefaultOffsetX;
                    offsetY = DefaultOffsetY;
                }

                //Bounding Box Check
                if (clickX >= (en.X + offsetX) && clickX < (en.X + width + offsetX) && clickY >= (en.Y + offsetY) && clickY < (en.Y + height + offsetY))
                {
                    if (leftPressed) // Move Enemy
                    {
                        mouseStartX = clickX;
                        mouseStartY = clickY;
                        mouseState = MouseState.Move;
                        referanceStartX = en.X;
                        referanceStartY = en.Y;
                        SelectEnemy(en);
                    }
                    else //...
                    {
                        await ObjectDescriptionMessage(icon, en.Id, en.Type);
                        return;
                    }
                }
            }
        }
    }
    private void layoutCanvas_PointerMoved(object? sender, PointerEventArgs e)
    {
        PointerPoint pos = e.GetCurrentPoint(layoutCanvas);
        PointerPointProperties properties = pos.Properties;
        int mouseX = (int)(Math.Round(pos.Position.X) / scale) + viewerX;
        int mouseY = (int)(Math.Round(pos.Position.Y) / scale) + viewerY;

        if (mouseState == MouseState.TriggerSelect)
        {
            int curX = mouseX;
            int curY = mouseY;

            if ((e.KeyModifiers & KeyModifiers.Shift) != 0)
            {
                curX &= 0xFFF8;
                curY &= 0xFFF8;
            }

            int left = Math.Min(curX, mouseStartX);
            int right = Math.Max(curX, mouseStartX);
            int top = Math.Min(curY, mouseStartY);
            int bottom = Math.Max(curY, mouseStartY);

            selectRect = new SKRect(left - viewerX, top - viewerY, right - viewerX, bottom - viewerY);
            DrawLayout();
        }
        else if (mouseState == MouseState.Move)
        {
            short locationX = (short)Math.Clamp((mouseX + -mouseStartX) + referanceStartX, 0, 0x1FFF);
            short locationY = (short)Math.Clamp((mouseY + -mouseStartY) + referanceStartY, 0, 0x1FFF);

            // Snap to 16-pixel grid when holding SHIFT
            if (e.KeyModifiers == KeyModifiers.Shift)
            {
                locationX = (short)(locationX & 0xFFF8); // snap X to multiple of 8
                locationY = (short)(locationY & 0xFFF8); // snap Y to multiple of 8
            }

            selectedEnemy.X = locationX;
            selectedEnemy.Y = locationY;

            byte column = (byte)(locationX / 32);
            selectedEnemy.Column = column;

            MainWindow.window.enemyE.xInt.Value = locationX;
            MainWindow.window.enemyE.yInt.Value = locationY;
            MainWindow.window.enemyE.columnInt.Value = column;

            SNES.edit = true;
            DrawLayout();
        }
        else if (mouseState == MouseState.Pan)
        {
            int screenX = (int)Math.Round(pos.Position.X / scale);
            int screenY = (int)Math.Round(pos.Position.Y / scale);

            int worldX = screenX + viewerX;
            int worldY = screenY + viewerY;

            viewerX = (short)Math.Clamp(referanceStartX - (screenX - mouseStartX), 0, 0x1FFF);

            viewerY = (short)Math.Clamp(referanceStartY - (screenY - mouseStartY), 0, 0x1FFF);

            DrawLayout();
        }
    }
    private void layoutCanvas_PointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (mouseState == MouseState.TriggerSelect)
        {
            DrawLayout();
            int id = Level.Id;

            if (CameraEditor.CameraTriggers[id].Count == 0)
                return;

            PointerPoint pos = e.GetCurrentPoint(layoutCanvas);

            int endX = (int)(Math.Round(pos.Position.X) / scale) + viewerX;
            int endY = (int)(Math.Round(pos.Position.Y) / scale) + viewerY;

            if ((e.KeyModifiers & KeyModifiers.Shift) != 0)
            {
                endX &= 0xFFF8;
                endY &= 0xFFF8;
            }

            int leftSide = Math.Clamp(Math.Min(endX, mouseStartX), 0, 0x1FFF);
            int rightSide = Math.Clamp(Math.Max(endX, mouseStartX), 0, 0x1FFF);
            int topSide = Math.Clamp(Math.Min(endY, mouseStartY), 0, 0x1FFF);
            int bottomSide = Math.Clamp(Math.Max(endY, mouseStartY), 0, 0x1FFF);

            var trigger = CameraEditor.CameraTriggers[id][CameraEditor.cameraTriggerId];
            trigger.LeftSide = (ushort)leftSide;
            trigger.RightSide = (ushort)rightSide;
            trigger.TopSide = (ushort)topSide;
            trigger.BottomSide = (ushort)bottomSide;

            SNES.edit = true;
            MainWindow.window.camE.suppressInts = true;
            MainWindow.window.camE.SetTriggerIntValues();
            MainWindow.window.camE.suppressInts = false;
        }
        else if (mouseState == MouseState.Pan)
            Cursor = new Cursor(StandardCursorType.Arrow);
        mouseState = MouseState.None;
    }
    private void layoutCanvas_PointerExited(object? sender, PointerEventArgs e)
    {
        if (mouseState == MouseState.Pan)
            Cursor = new Cursor(StandardCursorType.Arrow);
        mouseState = MouseState.None;
    }
    private void layoutCanvas_OnDragOver(object sender, DragEventArgs e)
    {
        e.DragEffects = DragDropEffects.Copy;
    }
    private async void layoutCanvas_OnDrop(object sender, DragEventArgs e)
    {
        if (!e.Data.Contains("OBJ") || !await ValidEnemyAdd())
            return;

        int value = (int)e.Data.Get("OBJ");

        byte id = (byte)(value & 0xFF);
        byte type = (byte)(value >> 8);

        Point point = e.GetPosition(layoutCanvas);

        short x = (short)(Math.Clamp((point.X / scale) + viewerX, 0, 0x1FFF));
        short y = (short)(Math.Clamp((point.Y / scale) + viewerY, 0, 0x1FFF));
        byte column = (byte)(x / 32);

        Enemy en = new Enemy();
        en.Id = id;
        en.Type = type;
        en.X = (x);
        en.Y = y;
        en.Column = column;
        Level.Enemies[Level.Id].Add(en);
        SNES.edit = true;
        SelectEnemy(en);
        DrawLayout();
    }
    private void layoutCanvas_PointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        if (e.KeyModifiers != KeyModifiers.Shift)
            return;

        Point pos = e.GetPosition(layoutCanvas); // Mouse position relative to canvas
        float mouseX = (float)pos.X;
        float mouseY = (float)pos.Y;

        if (e.Delta.Y < 0)
        {
            // Zoom Out
            int oldScale = (int)scale;
            scale = Math.Clamp(scale - 1, 1, Const.MaxScaleUI);
            if (scale == oldScale) return;

            // Calculate mouse centered zoom
            float contentX = viewerX + mouseX / oldScale;
            float contentY = viewerY + mouseY / oldScale;
            viewerX = (short)Math.Clamp(contentX - mouseX / scale, 0, 0x1FFF);
            viewerY = (short)Math.Clamp(contentY - mouseY / scale, 0, 0x1FFF);

            layoutCanvas.InvalidateVisual();
        }
        else
        {
            // Zoom In
            int oldScale = (int)scale;
            scale = Math.Clamp(scale + 1, 1, Const.MaxScaleUI);
            if (scale == oldScale) return;

            // Calculate mouse centered zoom
            float contentX = viewerX + mouseX / oldScale;
            float contentY = viewerY + mouseY / oldScale;
            viewerX = (short)Math.Clamp(contentX - mouseX / scale, 0, 0x1FFF);
            viewerY = (short)Math.Clamp(contentY - mouseY / scale, 0, 0x1FFF);

            layoutCanvas.InvalidateVisual();
        }

        e.Handled = true;
    }
    private void enemyListCanvas_MeasureEvent(object? sender, Size e)
    {
        int totalHeight = 0;

        foreach (var icon in Const.ItemIcons.Values)
            totalHeight += icon.Height * 2;

        foreach (var icon in Const.EnemyIcons.Values)
            totalHeight += icon.Height * 2;

        enemyListCanvas.MeasuredSize = new Size(e.Width, totalHeight);
    }
    private void enemyListCanvas_RenderEvent(object? sender, SkiaCanvasEventArgs e)
    {
        SKCanvas canvas = e.Canvas;

        float drawBaseY = 0;
        float drawX = (float)(e.Bounds.Width / 4f);

        canvas.Save();
        canvas.Scale(2, 2);

        foreach (var icon in Const.ItemIcons.Values)
        {
            float centerY = drawBaseY + icon.Height * 0.5f;
            icon.DrawCentre(canvas, drawX, centerY);
            drawBaseY += icon.Height;
        }

        foreach (var icon in Const.EnemyIcons.Values)
        {
            float centerY = drawBaseY + icon.Height * 0.5f;
            icon.DrawCentre(canvas, drawX, centerY);
            drawBaseY += icon.Height;
        }

        canvas.Restore();
    }
    private async void enemyListCanvas_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        int type = -1;
        int id = 0;

        int boundY = 0;

        double pointY = e.GetPosition(layoutCanvas).Y + enemyScroll.Offset.Y;

        ObjectIcon icon = null;

        foreach (KeyValuePair<int, ObjectIcon> entry in Const.ItemIcons) //Check if Item Objects we clicked
        {
            icon = entry.Value;
            if (pointY >= boundY && pointY < (boundY + icon.Height * 2))
            {
                type = 0;
                id = entry.Key;
                break;
            }
            else
                boundY += icon.Height * 2;
        }

        if (type == -1)
        {
            foreach (KeyValuePair<int, ObjectIcon> entry in Const.EnemyIcons) //Check if Enemy Objects we clicked
            {
                icon = entry.Value;
                if (pointY >= boundY && pointY < (boundY + icon.Height * 2))
                {
                    type = 3;
                    id = entry.Key;
                    break;
                }
                else
                    boundY += icon.Height * 2;
            }
        }


        if (e.Properties.IsLeftButtonPressed && type != -1)
        {
            DataObject dataObject = new DataObject();
            dataObject.Set("OBJ", id | (type << 8));
            await DragDrop.DoDragDrop(e, dataObject, DragDropEffects.Copy);
        }
        else if (e.Properties.IsRightButtonPressed && type != -1)
        {
            await ObjectDescriptionMessage(icon, id, type);
        }
    }
    private void idInt_ValueChanged(object sender, int e)
    {
        if (selectedEnemy == null)
            return;
        SNES.edit = true;
        selectedEnemy.Id = (byte)e;
        DrawLayout();
    }
    private void subIdInt_ValueChanged(object sender, int e)
    {
        if (selectedEnemy == null)
            return;
        SNES.edit = true;
        selectedEnemy.SubId = (byte)e;
    }
    private void typeInt_ValueChanged(object sender, int e)
    {
        if (selectedEnemy == null)
            return;
        SNES.edit = true;
        selectedEnemy.Type = (byte)e;
        DrawLayout();
    }
    private void colInt_ValueChanged(object sender, int e)
    {
        if (selectedEnemy == null)
            return;
        SNES.edit = true;
        selectedEnemy.Column = (byte)e;
    }
    private void xInt_ValueChanged(object sender, int e)
    {
        if (selectedEnemy == null)
            return;
        SNES.edit = true;
        short posX = (short)e;
        Enemy en = selectedEnemy;
        en.X = posX;
        byte column = (byte)((posX / 32));
        en.Column = column;
        columnInt.Value = column;
        DrawLayout();
    }
    private void yInt_ValueChanged(object sender, int e)
    {
        if (selectedEnemy == null)
            return;
        SNES.edit = true;
        selectedEnemy.Y = (short)e;
        DrawLayout();
    }
    private async void AddEnemy_Click(object sender, RoutedEventArgs e)
    {
        if (!await ValidEnemyAdd()) return;

        Enemy en = new Enemy();
        en.X = (short)Math.Clamp(viewerX + layoutCanvas.Bounds.Width / 2 / scale, 0, 0x1FFF);
        en.Y = (short)Math.Clamp(viewerY + layoutCanvas.Bounds.Height / 2 / scale, 0, 0x1FFF);
        en.Id = 0xB; //Default is Heart Tank since it is the same Id across all games
        en.Type = 0;
        en.Column = (byte)(en.X / 32);
        Level.Enemies[Level.Id].Add(en);
        SNES.edit = true;
        SelectEnemy(en);
        DrawLayout();
    }
    private void RemoveEnemy_Click(object sender, RoutedEventArgs e)
    {
        if (selectedEnemy == null)
            return;
        SNES.edit = true;
        Level.Enemies[Level.Id].Remove(selectedEnemy);
        DisableSelect();
        DrawLayout();
    }
    private async void ToolsBtn_Click(object sender, RoutedEventArgs e)
    {
        Window window = new Window() { Title = "Tools", SizeToContent = SizeToContent.WidthAndHeight, WindowStartupLocation = WindowStartupLocation.CenterScreen };

        Button deleteAllBtn = new Button() { Content = "Delete All" };
        deleteAllBtn.Click += async (s, e) =>
        {
            bool results = await MessageBox.Show(MainWindow.window, "Are you sure you want to delete all enemies?\nThis cant be un-done", "WARNING", MessageBoxButton.YesNo);

            if (!results)
                return;

            if (Level.Id >= Const.PlayableLevelsCount || (Const.Id == Const.GameId.MegaManX3 && Level.Id > 0xE))
            {
                await MessageBox.Show(MainWindow.window, "Enemies cannot be added to this level.", "Error");
                return;
            }
            Level.Enemies[Level.Id].Clear();
            SNES.edit = true;
            DisableSelect();
            DrawLayout();
            await MessageBox.Show(MainWindow.window, "All enemies have been deleted!");
            return;
        };

        StackPanel stackPanel = new StackPanel();
        stackPanel.Children.Add(deleteAllBtn);
        window.Content = stackPanel;
        await window.ShowDialog(MainWindow.window);
    }
    private void zoomInBtn_Click(object sender, RoutedEventArgs e)
    {
        ZoomIn();
    }
    private void zoomOutBtn_Click(object sender, RoutedEventArgs e)
    {
        ZoomOut();
    }
    private async void Help_Click(object sender, RoutedEventArgs e)
    {
        HelpWindow h = new HelpWindow(3);
        await h.ShowDialog(MainWindow.window);
    }
    #endregion Events
}