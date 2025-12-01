using System;
using System.IO;
using System.Windows;

namespace TeheManX_Editor.Forms
{
    /// <summary>
    /// Interaction logic for SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : Window
    {
        #region Properties
        private bool enable = false;
        private bool edited = false;
        #endregion Properties

        #region Constructors
        public SettingsWindow()
        {
            InitializeComponent();
            emuPathBox.Text = MainWindow.settings.EmuPath;
            saveOnTestCheck.IsChecked = MainWindow.settings.SaveOnTest;
            displayInt.Value = MainWindow.settings.ReferanceWidth;
            dontUpdateCheck.IsChecked = MainWindow.settings.DontUpdate;
            enable = true;
        }
        #endregion Constructors

        #region Events
        private void emuBtn_Click(object sender, RoutedEventArgs e)
        {
            using(var fd = new System.Windows.Forms.OpenFileDialog())
            {
                fd.Filter = "EXE |*.exe";
                fd.Title = "Select Emulator EXE";
                if(fd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    MainWindow.settings.EmuPath = fd.FileName;
                    MainWindow.emu = null;
                    emuPathBox.Text = fd.FileName;
                    edited = true;
                }
            }
        }
        private void saveOnTestCheck_Checked(object sender, RoutedEventArgs e)
        {
            if (!enable)
                return;
            MainWindow.settings.SaveOnTest = true;
            edited = true;
        }
        private void saveOnTestCheck_Unchecked(object sender, RoutedEventArgs e)
        {
            if (!enable)
                return;
            MainWindow.settings.SaveOnTest = false;
            edited = true;
        }
        private void displayInt_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (!enable || e.NewValue == null || e.OldValue == null) return;
            MainWindow.settings.ReferanceWidth = (int)e.NewValue;
            edited = true;
        }
        private void dontUpdateCheck_Change(object sender, RoutedEventArgs e)
        {
            if (!enable) return;
            MainWindow.settings.DontUpdate = (bool)dontUpdateCheck.IsChecked;
            edited = true;
        }
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (edited)
            {
                try
                {
                    var json = System.Text.Json.JsonSerializer.Serialize<Settings>(MainWindow.settings);
                    File.WriteAllText("Settings.json", json);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
        }
        #endregion Events

    }
}
