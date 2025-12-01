using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
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
        internal static LayoutWindow layoutWindow = new LayoutWindow();
        internal static Settings settings = Settings.SetDefaultSettings();
        internal static Process emu;
        public static List<Undo> undos = new List<Undo>();
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
                DefineSizing();

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

            if (LayoutWindow.isOpen)
                layoutWindow.UpdateLayoutGrid();
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
                undos.Clear();
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
                undos.Clear();
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
                var result = MessageBox.Show("Are you sure you want to delete all of Layer " + (Level.BG + 1) + "?\nThis cant be un-done", "", MessageBoxButton.YesNo);

                if (result == MessageBoxResult.Yes)
                {
                    if (undos.Count != 0)
                    {
                        for (int i = (undos.Count - 1); i == -1; i++)
                        {
                            if (undos[i].type == Undo.UndoType.Layout)
                                undos.RemoveAt(i);
                        }
                    }
                    for (int i = 0; i < 0x400; i++)
                    {
                        Level.Layout[Level.Id, Level.BG, i] = 0;
                    }
                    SNES.edit = true;
                    window.layoutE.DrawLayout();
                    window.enemyE.DrawLayout();
                    if (LayoutWindow.isOpen)
                        layoutWindow.UpdateLayoutGrid();
                }
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
                    window.layoutE.UpdateBtn();
                    window.layoutE.AssignLimits();
                    window.screenE.AssignLimits();
                    window.tile32E.AssignLimits();
                    window.tile16E.AssignLimits();
                    window.enemyE.DrawLayout();
                    if (LayoutWindow.isOpen)
                        layoutWindow.UpdateLayoutGrid();
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
                    window.layoutE.UpdateBtn();
                    window.layoutE.AssignLimits();
                    window.screenE.AssignLimits();
                    window.tile32E.AssignLimits();
                    window.tile16E.AssignLimits();
                    window.enemyE.DrawLayout();
                    if (LayoutWindow.isOpen)
                        layoutWindow.UpdateLayoutGrid();
                }
            }
        }
        private void ScreenKeyCheck(string key)
        {
            //Clear Screen
            if (key == "Delete")
                window.screenE.DeleteScreen();
        }
        private void PaletteKeyCheck(string key)
        {
            bool update = false;
            if (key == "Up")
            {
                window.paletteE.palId--;
                update = true;
            }
            else if(key == "Down")
            {
                window.paletteE.palId++;
                update = true;
            }

            if (update)
            {
                window.paletteE.palId &= 7;
                window.paletteE.UpdatePaletteText();
                window.paletteE.DrawVramTiles();
                window.paletteE.UpdateCursor();
            }
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
                    undos.Clear();
                    Level.Id = 0;
                    Level.AssignPallete();
                    Level.LoadLevelTiles();
                    Update();
                    hub.Visibility = Visibility.Visible;
                }
            }
        }
        private void ProcessUndo()
        {
            if (undos.Count != 0)
            {
                Undo undo = undos[undos.Count - 1];
                Undo.ApplyUndo(undo);
                undos.RemoveAt(undos.Count - 1);
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
        private void SaveLayout()
        {

        }
        private void LoadLayout()
        {

        }
        private void LockWindows()
        {
            foreach (var childWind in Application.Current.Windows)
            {
                if (childWind.GetType() != typeof(MainWindow)) continue;
                MainWindow window = childWind as MainWindow;
                if (window.Width < 1) continue;
                window.hub.Visibility = Visibility.Hidden;
            }
        }
        private void UnlockWindows()
        {
            foreach (var childWind in Application.Current.Windows)
            {
                if (childWind.GetType() != typeof(MainWindow)) continue;
                MainWindow window = childWind as MainWindow;
                if (window.Width < 1) continue;
                window.hub.Visibility = Visibility.Visible;
            }
        }
        private void CloseChildWindows()
        {
            var childWindows = Application.Current.Windows.Cast<Window>().Where(w => w != Application.Current.MainWindow).ToList();
            window.hub.ConsolidateOrphanedItems = false;
            foreach (var window in childWindows)
                window.Close();
        }
        public void DefineSizing()
        {
            int W;
            if (settings.ReferanceWidth < 200)
                W = (int)(40 * SystemParameters.PrimaryScreenWidth / 100);
            else
                W = 40 * settings.ReferanceWidth / 100;
            window.layoutE.selectImage.MaxWidth = W;
            window.screenE.tileImage.MaxWidth = W;
            window.screenE.tileImage16.MaxWidth = W;
            window.tile32E.x16Image.MaxWidth = W;
            window.enemyE.canvas.Width = window.enemyE.layoutBMP.PixelWidth;
        }
        #endregion Methods

        #region Events
        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //Check for Update
            if (settings.DontUpdate) return;
            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (compatible; Grand/3.0)");
                try
                {
                    HttpResponseMessage response = await client.GetAsync(Const.ReproURL);
                    response.EnsureSuccessStatusCode();
                    string json = await response.Content.ReadAsStringAsync();
                    dynamic release = JsonSerializer.Deserialize<dynamic>(json);
                    string tag = release.tag_name;
                    if (tag != Const.EditorVersion && !Settings.IsPastVersion(tag))
                    {
                        var result = MessageBox.Show($"There is a new version of this editor ({tag}) do you want to download the update?", "New Version", MessageBoxButton.YesNo);
                        if (result == MessageBoxResult.Yes)
                        {
                            //Start Downloading
                            string url = release.assets[0].browser_download_url;
                            response = await client.GetAsync(url);
                            response.EnsureSuccessStatusCode();
                            using (Stream contentStream = await response.Content.ReadAsStreamAsync())
                            {
                                using (FileStream fileStream = new FileStream("TeheManX4 Editor " + tag + ".exe", FileMode.Create, FileAccess.Write, FileShare.None))
                                {
                                    await contentStream.CopyToAsync(fileStream);
                                }
                            }
                            Process.Start(Directory.GetCurrentDirectory() + "/" + "TeheManX Editor " + tag + ".exe");
                            Application.Current.Shutdown();
                        }
                    }
                }
                catch (HttpRequestException)
                {
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
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
                CloseChildWindows();
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
                    ProcessUndo();
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
                case "paletteTab":
                    {
                        PaletteKeyCheck(key);
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
                MessageBox.Show("You must set the emulator/script path in the settings before using the test button");
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
        private void stagesBtn_Click(object sender, RoutedEventArgs e)
        {
            if (SNES.rom == null)
                return;

            // Create a new context menu
            ContextMenu menu = new ContextMenu();

            // Create Menu Items
            for (int i = 0; i < Const.LevelsCount; i++)
            {
                MenuItem item = new MenuItem() { Header = $"STAGE {i:X2}" };
                item.Uid = i.ToString();
                item.Click += (s, args) =>
                {
                    if (window.screenE.mode16)
                    {
                        MessageBox.Show("You must exit 16x16 Mode before you can switch stages!");
                        return;
                    }
                    Level.Id = int.Parse(((MenuItem)s).Uid);
                    //Re-Update
                    undos.Clear();
                    Level.TileSet = 0;
                    Level.AssignPallete();
                    Level.LoadLevelTiles();
                    Update();

                    if (LayoutWindow.isOpen)
                        layoutWindow.UpdateLayoutGrid();
                };
                menu.Items.Add(item);
            }

            // Attach the menu to the button
            stagesBtn.ContextMenu = menu;

            // Show the menu immediately
            menu.PlacementTarget = stagesBtn;
            menu.IsOpen = true;
        }
        private void helpBtn_Click(object sender, RoutedEventArgs e)
        {

        }
        private void undoBtn_Click(object sender, RoutedEventArgs e)
        {
            ProcessUndo();
        }
        #endregion Events
    }
}
