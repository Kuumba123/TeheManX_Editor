using Avalonia.Controls;
using System.Threading.Tasks;

namespace TeheManX_Editor.Forms
{
    public static class MessageBox
    {
        public static async Task<bool> Show(Window? owner, string message, string title = "Message",
            MessageBoxButton buttons = MessageBoxButton.Ok)
        {
            var window = new MessageBoxWindow
            {
                Message = message,
                Title = title,
                ShowOk = buttons == MessageBoxButton.Ok || buttons == MessageBoxButton.OkCancel,
                ShowCancel = buttons == MessageBoxButton.OkCancel,
                ShowYes = buttons == MessageBoxButton.YesNo,
                ShowNo = buttons == MessageBoxButton.YesNo
            };
            window.Message = message;

            await window.ShowDialog(owner);

            return window.Result;
        }
    }

    public enum MessageBoxButton
    {
        Ok,
        OkCancel,
        YesNo
    }
}
