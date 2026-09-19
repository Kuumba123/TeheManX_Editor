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
        byte R = ColorTools.To24Bit(color % 32);
        byte G = ColorTools.To24Bit(color / 32 % 32);
        byte B = ColorTools.To24Bit(color / 1024 % 32);
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
        ushort newC = (ushort)(
			ColorTools.To15Bit(view.Color.B) * 1024 +
			ColorTools.To15Bit(view.Color.G) * 32 +
			ColorTools.To15Bit(view.Color.R)
		);
        Title = $"Set: {row:X}  Color: {col:X}    15BPP RGB #{newC:X4}";
    }
    private void Window_Closing(object? sender, WindowClosingEventArgs e)
    {
        pickerLeft = Position.X;
        pickerTop = Position.Y;
    }
    #endregion Events
}