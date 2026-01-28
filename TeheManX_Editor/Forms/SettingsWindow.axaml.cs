using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace TeheManX_Editor.Forms;

public partial class SettingsWindow : Window
{
    #region Properties
    private bool enable;
    private bool edited;
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

        enemyInvertSpeedCheck.IsChecked = MainWindow.settings.InvertSpeed;
        enable = true;
    }
    #endregion Constructors

    #region Events
    private async void emuBtn_Click(object sender, RoutedEventArgs e)
    {
        FilePickerFileType[] fileTypes = null;
        string title = null;

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            fileTypes = new FilePickerFileType[] { new FilePickerFileType("Executable or Batch File") { Patterns = ["*.exe;*.bat;*.cmd"] } };
            title = "Select Emulator EXE or Batch File";
        }
        else
        {
            fileTypes = new FilePickerFileType[] { new FilePickerFileType("All") { Patterns = ["*"] } };
            title = "Select a Executable or Script to launch";
        }

        IReadOnlyList<IStorageFile> result = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = title,
            FileTypeFilter = fileTypes,
            AllowMultiple = false
        });

        IStorageFile? file = result.FirstOrDefault();

        if (file != null)
        {
            MainWindow.settings.EmuPath = file.Path.LocalPath;
            MainWindow.emu = null;
            emuPathBox.Text = file.Path.LocalPath;
            edited = true;
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
    private void displayInt_ValueChanged(object sender, int e)
    {
        if (!enable) return;
        MainWindow.settings.ReferanceWidth = (int)e;
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
    private void layoutDouble_ValueChanged(object sender, double e)
    {
        if (!enable) return;
        MainWindow.settings.LayoutScale = (double)e;
        edited = true;
    }
    private void layoutScreenDouble_ValueChanged(object sender, double e)
    {
        if (!enable) return;
        MainWindow.settings.LayoutScreenScale = (double)e;
        edited = true;
    }
    private void screenDouble_ValueChanged(object sender, double e)
    {
        if (!enable) return;
        MainWindow.settings.ScreenScale = (double)e;
        edited = true;
    }
    private void screenTilesDouble_ValueChanged(object sender, double e)
    {
        if (!enable) return;
        MainWindow.settings.ScreenTilesScale = (double)e;
        edited = true;
    }
    private void tile32Double_ValueChanged(object sender, double e)
    {
        if (!enable) return;
        MainWindow.settings.Tile32Scale = (double)e;
        edited = true;
    }
    private void tile32Image16Double_ValueChanged(object sender, double e)
    {
        if (!enable) return;
        MainWindow.settings.Tile32Image16Scale = (double)e;
        edited = true;
    }
    private void tile16Double_ValueChanged(object sender, double e)
    {
        if (!enable) return;
        MainWindow.settings.Tile16Scale = (double)e;
        edited = true;
    }
    private void enemyInvertSpeedCheckChange(object sender, RoutedEventArgs e)
    {
        if (!enable) return;
        MainWindow.settings.InvertSpeed = (bool)enemyInvertSpeedCheck.IsChecked;
        edited = true;
    }
    private async void Window_Closing(object? sender, WindowClosingEventArgs e)
    {
        if (edited)
        {
            try
            {
                string json = JsonConvert.SerializeObject(MainWindow.settings);
                await File.WriteAllTextAsync("Settings.json", json);
            }
            catch (Exception ex)
            {
                await MessageBox.Show(MainWindow.window, ex.Message);
            }
            MainWindow.window.DefineSizing();
        }
    }
    #endregion Events
}