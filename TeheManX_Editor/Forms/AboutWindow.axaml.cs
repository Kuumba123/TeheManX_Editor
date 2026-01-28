using Avalonia.Controls;

namespace TeheManX_Editor.Forms;

public partial class AboutWindow : Window
{
    #region Constructors
    public AboutWindow()
    {
        InitializeComponent();
        Title += Const.EditorVersion;
    }
    #endregion Constructors
}