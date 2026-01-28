using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using System;

namespace TeheManX_Editor.Forms
{
    public partial class NumDouble : UserControl
    {
        private TextBox? _inputBox;
        private bool supressTextChange;

        /* ========================
         * Events
         * ======================== */

        public event EventHandler<double>? ValueChanged;
        public event EventHandler<string>? InvalidValueEntered;

        /* ========================
         * Styled Properties
         * ======================== */

        public static readonly StyledProperty<double> ValueProperty =
            AvaloniaProperty.Register<NumDouble, double>(nameof(Value));

        public double Value
        {
            get => GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }

        public static readonly StyledProperty<double> MinimumProperty =
            AvaloniaProperty.Register<NumDouble, double>(nameof(Minimum), double.MinValue);

        public double Minimum
        {
            get => GetValue(MinimumProperty);
            set => SetValue(MinimumProperty, value);
        }

        public static readonly StyledProperty<double> MaximumProperty =
            AvaloniaProperty.Register<NumDouble, double>(nameof(Maximum), double.MaxValue);

        public double Maximum
        {
            get => GetValue(MaximumProperty);
            set => SetValue(MaximumProperty, value);
        }

        public static readonly StyledProperty<double> IncrementProperty =
            AvaloniaProperty.Register<NumDouble, double>(nameof(Increment), 1);

        public double Increment
        {
            get => GetValue(IncrementProperty);
            set => SetValue(IncrementProperty, value);
        }

        public static readonly StyledProperty<string> RawTextProperty =
            AvaloniaProperty.Register<NumDouble, string>(nameof(RawText), string.Empty);

        public string RawText
        {
            get => GetValue(RawTextProperty);
            set => SetValue(RawTextProperty, value);
        }
        public static readonly StyledProperty<int> MaxLengthProperty =
            AvaloniaProperty.Register<NumDouble, int>(nameof(MaxLength), 0);

        public int MaxLength
        {
            get => GetValue(MaxLengthProperty);
            set => SetValue(MaxLengthProperty, value);
        }
        public static readonly StyledProperty<double> SpinnerButtonWidthProperty =
            AvaloniaProperty.Register<NumDouble, double>(nameof(SpinnerButtonWidth), 17);

        public double SpinnerButtonWidth
        {
            get => GetValue(SpinnerButtonWidthProperty);
            set => SetValue(SpinnerButtonWidthProperty, value);
        }

        /* ========================
         * Object Lifecycle
         * ======================== */

        public NumDouble()
        {
            InitializeComponent();
            AttachedToVisualTree += OnAttached;

            // Value changes
            this.GetObservable(ValueProperty)
                .Subscribe(v =>
                {
                    OnValueChanged(v);
                    UpdateSpinnerButtonsState(v);
                });

            // Minimum changes
            this.GetObservable(MinimumProperty)
                .Subscribe(_ =>
                {
                    UpdateSpinnerButtonsState(Value);
                });

            // Maximum changes
            this.GetObservable(MaximumProperty)
                .Subscribe(_ =>
                {
                    UpdateSpinnerButtonsState(Value);
                });
        }

        private void OnAttached(object? sender, VisualTreeAttachmentEventArgs e)
        {
            _inputBox = this.FindControl<TextBox>("InputBox");

            if (_inputBox != null)
            {
                _inputBox.KeyDown += InputBox_KeyDown;
                _inputBox.TextChanged += InputBox_TextChanged;
                _inputBox.MaxLength = MaxLength;
            }
            // Apply spinner button width initially
            ApplySpinnerButtonWidth(SpinnerButtonWidth);

            // Keep TextBox.MaxLength in sync
            this.GetObservable(MaxLengthProperty).Subscribe(len =>
            {
                if (_inputBox != null)
                    _inputBox.MaxLength = len;
            });

            RestoreText();
            UpdateSpinnerButtonsState(Value);
        }

        /* ========================
         * Property Helpers
         * ======================== */
        private void OnValueChanged(double newValue)
        {
            RestoreText();
            ValueChanged?.Invoke(this, newValue);
        }

        /* ========================
         * Parsing / Validation
         * ======================== */

        public bool TryCommitText()
        {
            double parsed;

            if (RawText == null)
                return false;

            if (!double.TryParse(RawText, out parsed))
            {
                InvalidValueEntered?.Invoke(this, RawText);
                return false;
            }

            if (parsed > Maximum || parsed < Minimum)
                return false;

            Value = parsed;
            RestoreText();
            UpdateSpinnerButtonsState(Value);
            return true;
        }

        private void RestoreText()
        {
            supressTextChange = true;
            RawText = Value.ToString();
            supressTextChange = false;
        }

        /* ========================
         * Input Handling
         * ======================== */

        private void InputBox_TextChanged(object? sender, TextChangedEventArgs e)
        {
            if (supressTextChange)
                return;

            TryCommitText();
        }

        private void InputBox_KeyDown(object? sender, Avalonia.Input.KeyEventArgs e)
        {
            if (e.Key == Avalonia.Input.Key.Up)
            {
                e.Handled = true;
                Value = Math.Min(Value + Increment, Maximum);
                RestoreText();
                UpdateSpinnerButtonsState(Value);
            }
            else if (e.Key == Avalonia.Input.Key.Down)
            {
                e.Handled = true;
                Value = Math.Max(Value - Increment, Minimum);
                RestoreText();
                UpdateSpinnerButtonsState(Value);
            }
        }

        /* ========================
         * Spinner Buttons
         * ======================== */
        private void ApplySpinnerButtonWidth(double width)
        {
            if (increaseBtn != null)
                increaseBtn.Width = width;

            if (decreaseBtn != null)
                decreaseBtn.Width = width;
        }
        private void IncreaseButton_Click(object? sender, RoutedEventArgs e)
        {
            Value = Math.Min(Value + Increment, Maximum);
            RestoreText();
            UpdateSpinnerButtonsState(Value);
        }

        private void DecreaseButton_Click(object? sender, RoutedEventArgs e)
        {
            Value = Math.Max(Value - Increment, Minimum);
            RestoreText();
            UpdateSpinnerButtonsState(Value);
        }

        private void UpdateSpinnerButtonsState(double val)
        {
            increaseBtn.IsEnabled = val < Maximum;
            decreaseBtn.IsEnabled = val > Minimum;
        }
    }
}
