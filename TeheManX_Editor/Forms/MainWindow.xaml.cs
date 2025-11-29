using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace TeheManX_Editor.Forms
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region Fields
        internal static MainWindow window;
        internal static Settings settings = Settings.SetDefaultSettings();
        internal static Process emu;
        #endregion Fields

        #region Properties
        private bool max = false;
        #endregion Properties

        #region Constructors
        public MainWindow()
        {
            InitializeComponent();
            if (window == null)
            {
                window = this;
                //Open Settings
                if (File.Exists("Settings.json"))
                {
                    try
                    {
                        settings = JsonSerializer.Deserialize<Settings>(File.ReadAllText("Settings.json"));
                    }
                    catch (Exception e)
                    {
                        MessageBox.Show(e.Message, "ERROR");
                        Application.Current.Shutdown();
                    }
                }
                string[] args = Environment.GetCommandLineArgs();

                if (args.Length == 2) //Open Game Files using args
                {
                    if (!File.Exists(args[1]))
                    {
                        MessageBox.Show("The file: " + args[1] + " does not exist.");
                        return;
                    }
                    //Validate SFC File
                    byte[] rom = File.ReadAllBytes(args[1]);

                    if (!SNES.IsValidRom(rom))
                    {
                        MessageBox.Show("Your ROM File is corrupted or you didn't  select the correct version of the game.", "ERROR");
                        return;
                    }

                    SNES.rom = rom;

                    //File Validated
                    SNES.date = File.GetLastWriteTime(args[1]);
                    SNES.savePath = args[1];
                    Level.LoadLevelData();
                    //Setup Editor
                    Level.AssignPallete();
                    Level.LoadLevelTiles();
                    Update();
                    hub.Visibility = Visibility.Visible;
                }
            }
            else
            {
                dockBar.Visibility = Visibility.Collapsed;
                hub.Visibility = Visibility.Visible;
                this.Title = "Tehe SubWindow";
            }
        }
        #endregion Constructors

        #region Methods
        public void Update()
        {
            window.layoutE.AssignLimits();
            window.screenE.AssignLimits();
            window.tile32E.AssignLimits();
            window.tile16E.AssignLimits();
            window.paletteE.AssignLimits();
            window.enemyE.DrawLayout();
            window.enemyE.DrawEnemies();
            UpdateViewrCam();
            UpdateEnemyViewerCam();
            spawnE.SetSpawnSettings();
            tileE.AssignLimits();
            UpdateWindowTitle();
        }
        public void UpdateWindowTitle()
        {
            if (Const.Id != Const.GameId.MegaManX)
                window.Title = $"TeheMan X{(int)Const.Id + 1} Editor - STAGE {Level.Id:X2}";
            else
                window.Title = $"TeheMan X Editor - STAGE {Level.Id:X2}";
        }
        public void UpdateViewrCam()
        {
            window.layoutE.camLbl.Text = "X:" + Convert.ToString(window.layoutE.viewerX >> 8, 16).PadLeft(2, '0').ToUpper() + " Y:" + Convert.ToString(window.layoutE.viewerY >> 8, 16).PadLeft(2, '0').ToUpper();
        }
        public void UpdateEnemyViewerCam()
        {
            window.enemyE.camLbl.Text = "X:" + Convert.ToString(window.enemyE.viewerX >> 8, 16).PadLeft(4, '0').ToUpper() + " Y:" + Convert.ToString(window.enemyE.viewerY, 16).PadLeft(4, '0').ToUpper();
        }
        private void MainKeyCheck(string key)
        {
            if (key == "F1")
            {
                if (window.screenE.mode16)
                {
                    MessageBox.Show("You must exit 16x16 Mode before you can switch stages!");
                    return;
                }
                if (Level.Id != 0)
                    Level.Id--;
                else
                    Level.Id = Const.LevelsCount - 1;
                //Re-Update
                Level.TileSet = 0;
                Level.AssignPallete();
                Level.LoadLevelTiles();
                Update();
            }
            else if (key == "F2")
            {
                if (window.screenE.mode16)
                {
                    MessageBox.Show("You must exit 16x16 Mode before you can switch stages!");
                    return;
                }
                if (Level.Id != (Const.LevelsCount - 1))
                    Level.Id++;
                else
                    Level.Id = 0;
                //Re-Update
                Level.TileSet = 0;
                Level.AssignPallete();
                Level.LoadLevelTiles();
                Update();
            }
        }
        private void LayoutKeyCheck(string key, bool notFocus)
        {
            if (key == "Delete")
            {
                return;
            }
            if (!notFocus)  //check if NumInt is focused
                return;
            if (key == "W")
            {
                if (window.layoutE.viewerY != 0)
                {
                    window.layoutE.viewerY -= 0x100;
                    window.layoutE.DrawLayout();
                    UpdateViewrCam();
                }
            }
            else if (key == "S")
            {
                if ((window.layoutE.viewerY >> 8) < (32 - 3))
                {
                    window.layoutE.viewerY += 0x100;
                    window.layoutE.DrawLayout();
                    UpdateViewrCam();
                }
            }
            else if (key == "D")
            {
                if ((window.layoutE.viewerX >> 8) < (32 - 3))
                {
                    window.layoutE.viewerX += 0x100;
                    window.layoutE.DrawLayout();
                    UpdateViewrCam();
                }
            }
            else if (key == "A")
            {
                if (window.layoutE.viewerX != 0)
                {
                    window.layoutE.viewerX -= 0x100;
                    window.layoutE.DrawLayout();
                    UpdateViewrCam();
                }
            }
            else if (key == "D1")
            {
                if (window.screenE.mode16)
                {
                    MessageBox.Show("You must exit 16x16 Mode before you can switch layers!");
                    return;
                }
                if (Level.BG != 0)
                {
                    Level.BG = 0;
                    //window.enemyE.Draw();
                    /*if (ListWindow.screenViewOpen)
                    {
                        layoutWindow.DrawScreens();
                        layoutWindow.Title = "All Screens in Layer " + (Level.BG + 1);
                    }*/
                    window.layoutE.UpdateBtn();
                    window.layoutE.AssignLimits();
                    window.screenE.AssignLimits();
                    window.tile32E.AssignLimits();
                    window.tile16E.AssignLimits();
                    window.enemyE.DrawLayout();
                }
            }
            else if (key == "D2")
            {
                if (window.screenE.mode16)
                {
                    MessageBox.Show("You must exit 16x16 Mode before you can switch layers!");
                    return;
                }
                if (Level.BG != 1)
                {
                    Level.BG = 1;
                    //window.enemyE.Draw();
                    /*if (ListWindow.screenViewOpen)
                    {
                        layoutWindow.DrawScreens();
                        layoutWindow.Title = "All Screens in Layer " + (Level.BG + 1);
                    }*/
                    window.layoutE.UpdateBtn();
                    window.layoutE.AssignLimits();
                    window.screenE.AssignLimits();
                    window.tile32E.AssignLimits();
                    window.tile16E.AssignLimits();
                    window.enemyE.DrawLayout();
                }
            }
        }
        private void ScreenKeyCheck(string key)
        {
            //Clear Screen
            if (key == "Delete")
                window.screenE.DeleteScreen();
        }
        private void PaletteKeyCheck(string key, bool notFocus)
        {

        }
        private void EnemyKeyCheck(string key, bool notFocus)
        {
            if (!notFocus)  //check if NumInt is focused
                return;
            if (key == "W")
            {
                if (window.enemyE.viewerY != 0)
                {
                    window.enemyE.viewerY -= 0x100;
                    window.enemyE.DrawLayout();
                    window.enemyE.DrawEnemies();
                    UpdateEnemyViewerCam();
                }
            }
            else if (key == "S")
            {
                if ((window.enemyE.viewerY >> 8) < (32 - 2))
                {
                    window.enemyE.viewerY += 0x100;
                    window.enemyE.DrawLayout();
                    window.enemyE.DrawEnemies();
                    UpdateEnemyViewerCam();
                }
            }
            else if (key == "D")
            {
                if ((window.enemyE.viewerX >> 8) < (32 - 3))
                {
                    window.enemyE.viewerX += 0x100;
                    window.enemyE.DrawLayout();
                    window.enemyE.DrawEnemies();
                    UpdateEnemyViewerCam();
                }
            }
            else if (key == "A")
            {
                if (window.enemyE.viewerX != 0)
                {
                    window.enemyE.viewerX -= 0x100;
                    window.enemyE.DrawLayout();
                    window.enemyE.DrawEnemies();
                    UpdateEnemyViewerCam();
                }
            }
        }
        private int GetHubIndex()
        {
            IEnumerable<Dragablz.DragablzItem> tabs = this.hub.GetOrderedHeaders();

            int index = 0;
            foreach (var t in tabs)
            {
                if (((TabItem)t.Content).Name == ((TabItem)this.hub.SelectedItem).Name)
                    return index;
                index++;
            }

            return index;
        }
        private int GetActualIndex(int i /*Visual Index*/)
        {
            var tabs = this.hub.GetOrderedHeaders().ToList();

            int index = 0;
            foreach (var item in this.hub.Items)
            {
                if (((TabItem)item).Name == ((TabItem)tabs[i].Content).Name)
                    return index;
                index++;
            }
            return -1;
        }
        private void OpenGame()
        {
            if (window.screenE.mode16)
            {
                MessageBox.Show("You must exit 16x16 Mode before you can open another MegaMan X game!");
                return;
            }
            using (var fd = new System.Windows.Forms.OpenFileDialog())
            {
                fd.Filter = "SFC |*.sfc";
                fd.Title = "Open an MegaMan X1-3 SFC File";
                if (fd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    //Validate SFC File
                    byte[] rom = File.ReadAllBytes(fd.FileName);

                    if (!SNES.IsValidRom(rom))
                    {
                        MessageBox.Show("Your ROM File is corrupted or you didn't  select the correct version of the game.", "ERROR");
                        return;
                    }

                    SNES.rom = rom;

                    //File Validated
                    SNES.date = File.GetLastWriteTime(fd.FileName);
                    SNES.savePath = fd.FileName;
                    Level.LoadLevelData();
                    //Setup Editor
                    Level.Id = 0;
                    Level.AssignPallete();
                    Level.LoadLevelTiles();
                    Update();
                    hub.Visibility = Visibility.Visible;
                }
            }
        }
        private bool SaveGame()
        {
            if (window.screenE.mode16)
                return false;
            try
            {
                if (SNES.edit || !File.Exists(SNES.savePath))
                {
                    if (!Level.SaveLayouts() || !Level.SaveEnemyData())
                        return false;
                    File.WriteAllBytes(SNES.savePath, SNES.rom);
                    SNES.date = File.GetLastWriteTime(SNES.savePath);
                    SNES.edit = false;
                }
                else if (SNES.date != File.GetLastWriteTime(SNES.savePath))
                {
                    File.WriteAllBytes(SNES.savePath, SNES.rom);
                    SNES.date = File.GetLastWriteTime(SNES.savePath);
                    SNES.edit = false;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "ERROR");
                return false;
            }
            return true;
        }
        #endregion Methods

        #region Events
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {

        }
        private void Window_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            Xceed.Wpf.Toolkit.WatermarkTextBox num = Keyboard.FocusedElement as Xceed.Wpf.Toolkit.WatermarkTextBox;
            if (num != null)
            {
                TraversalRequest tRequest = new TraversalRequest(FocusNavigationDirection.Next);
                num.MoveFocus(tRequest);

                while (true)
                {
                    if (Keyboard.FocusedElement.GetType() != typeof(Xceed.Wpf.Toolkit.WatermarkTextBox))
                        break;
                    ((Xceed.Wpf.Toolkit.WatermarkTextBox)Keyboard.FocusedElement).MoveFocus(tRequest);
                }
            }
        }
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (Dragablz.TabablzControl.GetIsClosingAsPartOfDragOperation(this) && this == window)
                e.Cancel = true;
            else if (this == window)
            {
                if (SNES.edit)
                {
                    var result = MessageBox.Show("You have edited the game without saving.\nAre you sure you want to exit the editor?", "WARNING", MessageBoxButton.YesNo);
                    if (result != MessageBoxResult.Yes)
                    {
                        e.Cancel = true;
                        return;
                    }
                }
                Application.Current.Shutdown();
            }
        }
        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            string key = e.Key.ToString();
            if (key == "F11")
            {
                if (this.max)
                {
                    this.WindowStyle = WindowStyle.SingleBorderWindow;
                    this.WindowState = WindowState.Normal;
                    this.max = false;
                }
                else
                {
                    this.WindowStyle = WindowStyle.None;
                    this.WindowState = WindowState.Maximized;
                    this.max = true;
                }
                return;
            }
            if (e.KeyboardDevice.Modifiers == ModifierKeys.Control)
            {
                if (key == "O")
                {
                    OpenGame();
                }
                else if (key == "S" && SNES.rom != null)
                {
                    if (window.screenE.mode16)
                    {
                        MessageBox.Show("You must exit 16x16 Mode before you can save the game!");
                        return;
                    }
                    SaveGame();
                    return;
                }
                else if (key == "E" && SNES.rom != null) //For Launching Emu for Testing
                {
                    if (settings.EmuPath == "")
                    {
                        MessageBox.Show("You must set the emulator path in the settings before using the test button");
                        return;
                    }
                    if (settings.SaveOnTest)
                    {
                        if (window.screenE.mode16)
                        {
                            MessageBox.Show("You must exit 16x16 Mode before you can save the game!");
                            return;
                        }
                        if (!SaveGame())
                        {
                            return; //TODO: maybe add in a message or something...
                        }
                    }
                    try
                    {
                        if (emu == null)
                            emu = Process.Start(settings.EmuPath, "\"" + SNES.savePath + "\"");
                        else
                        {
                            if (!emu.HasExited)
                                emu.Kill();
                            emu.Start();
                        }
                    }
                    catch (System.ComponentModel.Win32Exception)
                    {
                        MessageBox.Show("Cant find the Emulator EXE", "ERROR");
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "ERROR while Launching Emu");
                    }
                    return;
                }
                else if (key == "Z" && SNES.rom != null)
                {

                }
                else if (key == "Left" && SNES.rom != null && this.hub.Items.Count > 1)
                {
                    int hubIndex = GetHubIndex();
                    if (hubIndex == 0)
                    {
                        this.hub.SelectedIndex = GetActualIndex(this.hub.Items.Count - 1);
                        window.Focus();
                        return;
                    }
                    this.hub.SelectedIndex = GetActualIndex(hubIndex - 1);
                    window.Focus();
                }
                else if (key == "Right" && SNES.rom != null && this.hub.Items.Count > 1)
                {
                    int hubIndex = GetHubIndex();
                    if (hubIndex == this.hub.Items.Count - 1)
                    {
                        this.hub.SelectedIndex = GetActualIndex(0);
                        window.Focus();
                        return;
                    }
                    this.hub.SelectedIndex = GetActualIndex(hubIndex + 1);
                    window.Focus();
                }
                return;
            }
            if (SNES.rom == null)
                return;
            MainKeyCheck(key);
            if (hub.SelectedItem == null)
                return;
            bool nonNumInt = false;
            if (Keyboard.FocusedElement.GetType() != typeof(Xceed.Wpf.Toolkit.WatermarkTextBox)) nonNumInt = true;
            TabItem tab = (TabItem)hub.SelectedItem;
            switch (tab.Name)
            {
                case "layoutTab":
                    {
                        LayoutKeyCheck(key, nonNumInt);
                        break;
                    }
                case "screenTab":
                    {
                        ScreenKeyCheck(key);
                        break;
                    }
                case "clutTab":
                    {
                        //ClutKeyCheck(key);
                        break;
                    }
                case "enemyTab":
                    {
                        EnemyKeyCheck(key, nonNumInt);
                        break;
                    }
                case "animeTab":
                    {
                        //AnimeKeyCheck(key, nonNumInt);
                        break;
                    }
            }
        }
        private void openBtn_Click(object sender, RoutedEventArgs e)
        {
            OpenGame();
        }
        private void saveBtn_Click(object sender, RoutedEventArgs e)
        {
            if (window.screenE.mode16)
            {
                MessageBox.Show("You must exit 16x16 Mode before you can save the game!");
                return;
            }
            if (!File.Exists(SNES.savePath) || File.GetLastWriteTime(SNES.savePath) != SNES.date || SNES.edit)
            {
                try
                {
                    if (!Level.SaveLayouts() || !Level.SaveEnemyData())
                        return;
                    File.WriteAllBytes(SNES.savePath, SNES.rom);
                    SNES.date = File.GetLastWriteTime(SNES.savePath);
                    SNES.edit = false;
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "ERROR");
                }
            }
        }
        private void saveAsBtn_Click(object sender, RoutedEventArgs e)
        {
            if (window.screenE.mode16)
            {
                MessageBox.Show("You must exit 16x16 Mode before you can save the game!");
                return;
            }
            if (!Level.SaveLayouts() || !Level.SaveEnemyData())
                return;
            using (var fd = new System.Windows.Forms.SaveFileDialog())
            {
                fd.Filter = "SFC |*.sfc";
                fd.Title = "Select Save Location";
                if (fd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    try
                    {
                        File.WriteAllBytes(fd.FileName, SNES.rom);
                        SNES.savePath = fd.FileName;
                        SNES.date = File.GetLastWriteTime(fd.FileName);
                        SNES.edit = false;
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "ERROR");
                    }
                }
            }
        }
        private void testBtn_Click(object sender, RoutedEventArgs e)
        {
            if (SNES.rom == null)
                return;
            if (settings.EmuPath == "")
            {
                MessageBox.Show("You must set the emulator path in the settings before using the test button");
                return;
            }
            if (settings.SaveOnTest)
            {
                if (window.screenE.mode16)
                {
                    MessageBox.Show("You must exit 16x16 Mode before you can save the game!");
                    return;
                }
                if (!SaveGame())
                {
                    return; //TODO: maybe add in a message or something...
                }
            }
            try
            {
                if (emu == null)
                    emu = Process.Start(settings.EmuPath, "\"" + SNES.savePath + "\"");
                else
                {
                    if (!emu.HasExited)
                        emu.Kill();
                    emu.Start();
                }
            }
            catch (System.ComponentModel.Win32Exception)
            {
                MessageBox.Show("Cant find the Emulator EXE", "ERROR");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "ERROR while Launching Emu");
            }
        }
        private void toolsBtn_Click(object sender, RoutedEventArgs e)
        {
            var t = new ToolsWindow();
            t.ShowDialog();
        }
        private void aboutBtn_Click(object sender, RoutedEventArgs e)
        {
            AboutWindow about = new AboutWindow();
            about.ShowDialog();
        }
        private void settingsBtn_Click(object sender, RoutedEventArgs e)
        {
            SettingsWindow settings = new SettingsWindow();
            settings.ShowDialog();
        }
        #endregion Events
    }
}
