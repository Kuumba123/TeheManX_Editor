using Avalonia;
using Avalonia.Controls;
using SkiaSharp;
using System;
using System.Buffers.Binary;

namespace TeheManX_Editor.Forms
{
    public partial class BossGateWindow : Window
    {
        #region Fields
        public static bool isOpen;
        public static int? layoutLeft;
        public static int? layoutTop;
        public static int? layoutState;
        #endregion Fields

        #region Properties
        bool supressInts;
        int removedBossGateId;
        SKBitmap bossGateBMP = new SKBitmap(16, 48, SKColorType.Bgra8888, SKAlphaType.Premul);
        SKBitmap removedBossGateBMP = new SKBitmap(32, 48, SKColorType.Bgra8888, SKAlphaType.Premul);
        #endregion Properties

        #region Constructor
        public BossGateWindow()
        {
            InitializeComponent();

            removedBossGateInt.Maximum = Const.RemovedBossDoorCount - 1;

            if (layoutLeft.HasValue && layoutTop.HasValue && layoutState.HasValue)
            {
                Position = new PixelPoint(layoutLeft.Value, layoutTop.Value);
                WindowState = (WindowState)layoutState.Value;
            }
            UpdateRemovedBossGateInts();
        }
        #endregion Constructor

        #region Methods
        internal void UpdateBossGateUI()
        {
            DrawBossGate();
            UpdateBossGateInts();
        }
        internal void DrawBossGate()
        {
            bossGateImage.InvalidateVisual();
            removedBossGateImage.InvalidateVisual();
        }
        internal void UpdateBossGateInts()
        {
            supressInts = true;
            for (int i = 0; i < 3; i++)
            {
                int offset = Const.BossDoorTilesOffset + Level.Id * 6 + i * 2;
                if (offset + 1 >= SNES.rom.Length)
                    continue;
                short value = BinaryPrimitives.ReadInt16LittleEndian(SNES.rom.AsSpan(offset));
                (bossGateIntsPannel.Children[i] as NumInt).Value = value;
            }
            supressInts = false;
        }
        internal void UpdateRemovedBossGateInts()
        {
            supressInts = true;
            for (int i = 0; i < 6; i++)
            {
                int offset = Const.RemovedBossDoorTilesOffset + removedBossGateId * 0xC + i * 2;
                if (offset + 1 >= SNES.rom.Length)
                    continue;
                short value = BinaryPrimitives.ReadInt16LittleEndian(SNES.rom.AsSpan(offset));

                if (i < 3)
                    (removedBossGateLeftColumn.Children[i] as NumInt).Value = value;
                else
                    (removedBossGateRightColumn.Children[i - 3] as NumInt).Value = value;
            }
            supressInts = false;
        }
        #endregion Methods

        #region Events
        private void Window_Opened(object? sender, EventArgs e)
        {
            isOpen = true;
        }
        private void Window_Closing(object? sender, WindowClosingEventArgs e)
        {
            isOpen = false;
            layoutLeft = Position.X;
            layoutTop = Position.Y;
            layoutState = (int)WindowState;
        }
        private void bossGateTileInt_ValueChanged(object sender, int e)
        {
            if (SNES.rom == null || Level.Id >= Const.PlayableLevelsCount || supressInts)
                return;

            NumInt numInt = (NumInt)sender;

            int offset = Const.BossDoorTilesOffset + Level.Id * 2 + (int)numInt.Tag * 2;

            if (offset + 1 >= SNES.rom.Length)
                return;
            if (numInt.Value < 0 || numInt.Value > 0xFFFF)
                return;
            if (BinaryPrimitives.ReadInt16LittleEndian(SNES.rom.AsSpan(offset)) == (short)numInt.Value)
                return;
            BinaryPrimitives.WriteInt16LittleEndian(SNES.rom.AsSpan(offset), (short)numInt.Value);
            SNES.edit = true;
            DrawBossGate();
        }
        private void bossGateImage_MeasureEvent(object? sender, Size e)
        {
            Size size = new Size(32, 96);
            bossGateImage.MeasuredSize = size;
        }
        private void bossGateImage_RenderEvent(object? sender, SkiaCanvasEventArgs e)
        {
            if (Level.Id < Const.PlayableLevelsCount)
            {
                unsafe
                {
                    nint ptr = bossGateBMP.GetPixels();
                    int stride = bossGateBMP.RowBytes;

                    int offset = Const.BossDoorTilesOffset + Level.Id * 6;

                    for (int i = 0; i < 3; i++)
                    {
                        Level.Draw16xTile(BinaryPrimitives.ReadInt16LittleEndian(SNES.rom.AsSpan(offset)), 0, i * 16, stride, ptr);
                        offset += 2;
                    }
                }

                SKCanvas canvas = e.Canvas;
                SKRect destRect = new SKRect(0, 0, 32, 96);
                canvas.DrawBitmap(bossGateBMP, destRect);
            }
            else
            {
                SKCanvas canvas = e.Canvas;
                SKRect destRect = new SKRect(0, 0, 32, 96);
                canvas.Clear(SKColors.Black);
            }
        }
        private void removedBossGateImage_MeasureEvent(object? sender, Size e)
        {
            Size size = new Size(64, 96);
            removedBossGateImage.MeasuredSize = size;
        }
        private void removedBossGateImage_RenderEvent(object? sender, SkiaCanvasEventArgs e)
        {
            if (Level.Id < Const.PlayableLevelsCount)
            {
                unsafe
                {
                    nint ptr = removedBossGateBMP.GetPixels();
                    int stride = removedBossGateBMP.RowBytes;

                    int offset = Const.RemovedBossDoorTilesOffset + removedBossGateId * 0xC;

                    for (int x = 0; x < 2; x++)
                    {
                        for (int i = 0; i < 3; i++)
                        {
                            Level.Draw16xTile(BinaryPrimitives.ReadInt16LittleEndian(SNES.rom.AsSpan(offset)), x * 16, i * 16, stride, ptr);
                            offset += 2;
                        }
                    }
                }

                SKCanvas canvas = e.Canvas;
                SKRect destRect = new SKRect(0, 0, 64, 96);
                canvas.DrawBitmap(removedBossGateBMP, destRect);
            }
            else
            {
                SKCanvas canvas = e.Canvas;
                SKRect destRect = new SKRect(0, 0, 64, 96);
                canvas.Clear(SKColors.Black);
            }
        }
        private void removedBossGateInt_ValueChanged(object? sender, int e)
        {
            removedBossGateId = removedBossGateInt.Value;
            removedBossGateImage.InvalidateVisual();
            UpdateRemovedBossGateInts();
        }
        private void removedBossGateTileInt_ValueChanged(object sender, int e)
        {
            if (SNES.rom == null || Level.Id >= Const.PlayableLevelsCount || supressInts)
                return;

            NumInt numInt = (NumInt)sender;

            int offset = Const.RemovedBossDoorTilesOffset + removedBossGateId * 0xC + (int)numInt.Tag * 2;

            if (offset + 1 >= SNES.rom.Length)
                return;
            if (numInt.Value < 0 || numInt.Value > 0xFFFF)
                return;
            if (BinaryPrimitives.ReadInt16LittleEndian(SNES.rom.AsSpan(offset)) == (short)numInt.Value)
                return;
            BinaryPrimitives.WriteInt16LittleEndian(SNES.rom.AsSpan(offset), (short)numInt.Value);
            SNES.edit = true;
            DrawBossGate();
        }
        private void HelpButton_Click(object sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            HelpWindow helpWindow = new HelpWindow(8);
            helpWindow.ShowDialog(this);
        }
        #endregion Events
    }
}