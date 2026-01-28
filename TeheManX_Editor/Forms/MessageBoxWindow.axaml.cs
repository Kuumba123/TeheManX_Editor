using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using ReactiveUI;
using System.ComponentModel;
using System.Reactive;

namespace TeheManX_Editor.Forms;

public partial class MessageBoxWindow : Window, INotifyPropertyChanged
{
    // INotifyPropertyChanged implementation
    public event PropertyChangedEventHandler? PropertyChanged;
    private void RaisePropertyChanged(string propertyName) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    private string _message = "";
    public string Message
    {
        get => _message;
        set
        {
            if (_message != value)
            {
                _message = value;
                RaisePropertyChanged(nameof(Message));
            }
        }
    }

    private string _titleText = "";
    public string TitleText
    {
        get => _titleText;
        set
        {
            if (_titleText != value)
            {
                _titleText = value;
                RaisePropertyChanged(nameof(TitleText));
            }
        }
    }

    private bool _showOk = true;
    public bool ShowOk
    {
        get => _showOk;
        set
        {
            if (_showOk != value)
            {
                _showOk = value;
                RaisePropertyChanged(nameof(ShowOk));
            }
        }
    }

    private bool _showCancel = false;
    public bool ShowCancel
    {
        get => _showCancel;
        set
        {
            if (_showCancel != value)
            {
                _showCancel = value;
                RaisePropertyChanged(nameof(ShowCancel));
            }
        }
    }

    private bool _showYes = false;
    public bool ShowYes
    {
        get => _showYes;
        set
        {
            if (_showYes != value)
            {
                _showYes = value;
                RaisePropertyChanged(nameof(ShowYes));
            }
        }
    }

    private bool _showNo = false;
    public bool ShowNo
    {
        get => _showNo;
        set
        {
            if (_showNo != value)
            {
                _showNo = value;
                RaisePropertyChanged(nameof(ShowNo));
            }
        }
    }

    public bool Result { get; private set; } = false;

    public MessageBoxWindow()
    {
        InitializeComponent();
        DataContext = this;
    }

    private void Ok_Click(object? sender, RoutedEventArgs e)
    {
        Result = true;
        Close();
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e)
    {
        Result = false;
        Close();
    }

    private void Yes_Click(object? sender, RoutedEventArgs e)
    {
        Result = true;
        Close();
    }

    private void No_Click(object? sender, RoutedEventArgs e)
    {
        Result = false;
        Close();
    }

    private void Window_Loaded(object? sender, RoutedEventArgs e)
    {
        if (_showOk)
            okButton.Focus();
        else if (_showYes)
            yesButton.Focus();
    }
}