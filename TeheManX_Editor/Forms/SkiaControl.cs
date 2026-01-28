using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using SkiaSharp;
using System;

namespace TeheManX_Editor.Forms
{
    /// <summary>
    /// Event args for the RenderEvent, passing SKCanvas and bounds.
    /// </summary>
    public class SkiaCanvasEventArgs : EventArgs
    {
        public SKCanvas Canvas { get; }
        public Rect Bounds { get; }

        public SkiaCanvasEventArgs(SKCanvas canvas, Rect bounds)
        {
            Canvas = canvas;
            Bounds = bounds;
        }
    }

    /// <summary>
    /// A custom Skia-backed control with pointer events and a render event for XAML.
    /// </summary>
    public class SkiaControl : Control
    {
        public SkiaControl()
        {
            // Make sure the control clips its drawing to its bounds
            ClipToBounds = true;
        }

        #region Properties and Pointer Events Exposed to XAML

        public event EventHandler<PointerPressedEventArgs>? PointerPressedEvent;
        public event EventHandler<PointerReleasedEventArgs>? PointerReleasedEvent;
        public event EventHandler<PointerEventArgs>? PointerMovedEvent;
        public event EventHandler<PointerEventArgs>? PointerEnteredEvent;
        public event EventHandler<PointerEventArgs>? PointerExitedEvent;
        public event EventHandler<Size>? MeasureEvent;
        public event EventHandler<Size>? ArrangeEvent;

        public static readonly StyledProperty<Size> MeasuredSizeProperty = AvaloniaProperty.Register<SkiaControl, Size>(nameof(MeasuredSize),Size.Infinity);

        public Size MeasuredSize
        {
            get => GetValue(MeasuredSizeProperty);
            set => SetValue(MeasuredSizeProperty, value);
        }

        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
            base.OnPointerPressed(e);
            PointerPressedEvent?.Invoke(this, e);
        }

        protected override void OnPointerReleased(PointerReleasedEventArgs e)
        {
            base.OnPointerReleased(e);
            PointerReleasedEvent?.Invoke(this, e);
        }

        protected override void OnPointerMoved(PointerEventArgs e)
        {
            base.OnPointerMoved(e);
            PointerMovedEvent?.Invoke(this, e);
        }

        protected override void OnPointerEntered(PointerEventArgs e)
        {
            base.OnPointerEntered(e);
            PointerEnteredEvent?.Invoke(this, e);
        }

        protected override void OnPointerExited(PointerEventArgs e)
        {
            base.OnPointerExited(e);
            PointerExitedEvent?.Invoke(this, e);
        }
        #endregion

        #region Render Event Exposed to XAML

        /// <summary>
        /// Exposes a Render event for XAML, passing a valid SKCanvas.
        /// </summary>
        public event EventHandler<SkiaCanvasEventArgs>? RenderEvent;

        public override void Render(DrawingContext context)
        {
            base.Render(context);

            // Add a custom draw operation for Skia rendering
            context.Custom(new SkiaDrawOp(Bounds, RenderEvent));
        }

        #endregion

        #region Layout Overrides
        protected override Size MeasureOverride(Size availableSize)
        {
            // Give subscribers a chance to update MeasuredSize
            MeasureEvent?.Invoke(this, availableSize);

            // Fallback if event handler didn't set it
            if (MeasuredSize == Size.Infinity)
                MeasuredSize = availableSize;

            return MeasuredSize;
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            return finalSize;
        }

        #endregion

        #region Internal Custom Draw Operation

        private class SkiaDrawOp : ICustomDrawOperation
        {
            private readonly Rect _bounds;
            private readonly EventHandler<SkiaCanvasEventArgs>? _renderEvent;

            public SkiaDrawOp(Rect bounds, EventHandler<SkiaCanvasEventArgs>? renderEvent)
            {
                _bounds = new Rect(
                    0,
                    0,
                    Math.Max(0, bounds.Width),
                    Math.Max(0, bounds.Height));

                _renderEvent = renderEvent;
            }

            public Rect Bounds => _bounds;

            public void Dispose() { }

            public bool Equals(ICustomDrawOperation? other) => false;

            public bool HitTest(Point p) => _bounds.Contains(p);

            public void Render(ImmediateDrawingContext context)
            {
                // Only proceed if Skia lease is available
                var leaseFeature = context.TryGetFeature<ISkiaSharpApiLeaseFeature>();
                if (leaseFeature == null)
                    return;

                using var lease = leaseFeature.Lease();
                var canvas = lease.SkCanvas;

                // Raise the RenderEvent for user drawing
                _renderEvent?.Invoke(null, new SkiaCanvasEventArgs(canvas, _bounds));
            }
        }

        #endregion
    }
}
