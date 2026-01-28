using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace TeheManX_Editor.Forms;

public partial class ColorDialog : Window
{
    #region Fields
    public static double pickerLeft = double.NaN;
    public static double pickerTop = double.NaN;
    #endregion Fields

    #region Properties
    public bool confirm = false;
    private int col;
    private int row;
    #endregion Properties

    #region Constructors
    public ColorDialog(ushort color, int col, int row)
    {
        this.col = col;
        this.row = row;
        InitializeComponent();
        if (!double.IsNaN(pickerLeft))
            Position = new PixelPoint((int)pickerLeft, (int)pickerTop);
        byte R = (byte)(color % 32 * 8);
        byte G = (byte)(color / 32 % 32 * 8);
        byte B = (byte)(color / 1024 % 32 * 8);
        view.Color = Color.FromRgb(R, G, B);
    }
    #endregion Constructors

    #region Events
    private void Confirm_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        confirm = true;
        Close();
    }
    private void ColorView_ColorChanged(object? sender, ColorChangedEventArgs e)
    {
        ushort newC = (ushort)(view.Color.B / 8 * 1024 + view.Color.G / 8 * 32 + view.Color.R / 8);
        Title = $"Set: {row:X}  Color: {col:X}    15BPP RGB #{newC:X4}";
    }
    private void Window_Closing(object? sender, WindowClosingEventArgs e)
    {
        pickerLeft = Position.X;
        pickerTop = Position.Y;
    }
    #endregion Events
}