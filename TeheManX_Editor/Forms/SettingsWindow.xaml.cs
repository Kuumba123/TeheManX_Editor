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
            fixedCheck.IsChecked = MainWindow.settings.UseFixedScale;

            layoutDouble.Value = MainWindow.settings.LayoutScale;
            layoutDouble.Maximum = Const.MaxScaleUI;
            layoutScreenDouble.Value = MainWindow.settings.LayoutScreenScale;
            layoutScreenDouble.Maximum = Const.MaxScaleUI;

            screenDouble.Value = MainWindow.settings.ScreenScale;
            screenDouble.Maximum = Const.MaxScaleUI;
            screenTilesDouble.Value = MainWindow.settings.ScreenTilesScale;
            screenTilesDouble.Maximum = Const.MaxScaleUI;

            tile32Double.Value = MainWindow.settings.Tile32Scale;
            tile32Double.Maximum = Const.MaxScaleUI;
            tile32Image16Double.Value = MainWindow.settings.Tile32Image16Scale;
            tile32Image16Double.Maximum = Const.MaxScaleUI;

            tile16Double.Value = MainWindow.settings.Tile16Scale;
            tile16Double.Maximum = Const.MaxScaleUI;

            enemyScaleCheck.IsChecked = MainWindow.settings.EnemyFixedScale;
            enemyInvertSpeedCheck.IsChecked = MainWindow.settings.InvertSpeed;
            enable = true;
        }
        #endregion Constructors

        #region Events
        private void emuBtn_Click(object sender, RoutedEventArgs e)
        {
            using(var fd = new System.Windows.Forms.OpenFileDialog())
            {
                fd.Filter = "Executable or Batch File|*.exe;*.bat;*.cmd";
                fd.Title = "Select Emulator EXE or Batch File";
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
        private void fixedCheck_CheckChange(object sender, RoutedEventArgs e)
        {
            if (!enable) return;
            MainWindow.settings.UseFixedScale = (bool)fixedCheck.IsChecked;
            edited = true;
        }
        private void layoutDouble_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (!enable || e.NewValue == null || e.OldValue == null) return;
            MainWindow.settings.LayoutScale = (double)e.NewValue;
            edited = true;
        }
        private void layoutScreenDouble_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (!enable || e.NewValue == null || e.OldValue == null) return;
            MainWindow.settings.LayoutScreenScale = (double)e.NewValue;
            edited = true;
        }
        private void screenDouble_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (!enable || e.NewValue == null || e.OldValue == null) return;
            MainWindow.settings.ScreenScale = (double)e.NewValue;
            edited = true;
        }
        private void screenTilesDouble_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (!enable || e.NewValue == null || e.OldValue == null) return;
            MainWindow.settings.ScreenTilesScale = (double)e.NewValue;
            edited = true;
        }
        private void tile32Double_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (!enable || e.NewValue == null || e.OldValue == null) return;
            MainWindow.settings.Tile32Scale = (double)e.NewValue;
            edited = true;
        }
        private void tile32Image16Double_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (!enable || e.NewValue == null || e.OldValue == null) return;
            MainWindow.settings.Tile32Image16Scale = (double)e.NewValue;
            edited = true;
        }
        private void tile16Double_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (!enable || e.NewValue == null || e.OldValue == null) return;
            MainWindow.settings.Tile16Scale = (double)e.NewValue;
            edited = true;
        }
        private void enemyScaleCheckChange(object sender, RoutedEventArgs e)
        {
            if (!enable) return;
            MainWindow.settings.EnemyFixedScale = (bool)enemyScaleCheck.IsChecked;
            edited = true;
        }
        private void enemyInvertSpeedCheckChange(object sender, RoutedEventArgs e)
        {
            if (!enable) return;
            MainWindow.settings.InvertSpeed = (bool)enemyInvertSpeedCheck.IsChecked;
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
                MainWindow.window.DefineSizing();
            }
        }
        #endregion Events
    }
}
