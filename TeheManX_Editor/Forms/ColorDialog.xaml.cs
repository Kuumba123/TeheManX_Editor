using System;
using System.Windows;
using System.Windows.Media;

namespace TeheManX_Editor.Forms
{
    /// <summary>
    /// Interaction logic for ColorDialog.xaml
    /// </summary>
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
            {
                this.Left = pickerLeft;
                this.Top = pickerTop;
            }
            byte R = (byte)(color % 32 * 8);
            byte G = (byte)(color / 32 % 32 * 8);
            byte B = (byte)(color / 1024 % 32 * 8);
            this.canvas.SelectedColor = Color.FromRgb(R, G, B);
        }
        #endregion Constructors

        #region Events
        private void Confirm_Click(object sender, RoutedEventArgs e)
        {
            confirm = true;
            this.Close();
        }

        private void canvas_SelectedColorChanged(object sender, RoutedPropertyChangedEventArgs<Color?> e)
        {
            ushort newC = (ushort)(canvas.SelectedColor.Value.B / 8 * 1024 + canvas.SelectedColor.Value.G / 8 * 32 + canvas.SelectedColor.Value.R / 8);
            this.Title = "Set: " + Convert.ToString(row, 16).ToUpper() + "  Color: " + Convert.ToString(col, 16).ToUpper() + "    15BPP RGB #" + Convert.ToString(newC, 16).ToUpper().PadLeft(4, '0');
        }
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            pickerLeft = Left;
            pickerTop = Top;
        }
        #endregion Events
    }
}
