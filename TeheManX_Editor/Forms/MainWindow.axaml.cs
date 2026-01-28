using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Dock.Avalonia.Controls;
using Dock.Model;
using Dock.Model.Avalonia.Controls;
using Dock.Model.Controls;
using Dock.Model.Core;
using Dock.Serializer;
using Newtonsoft.Json;
using SkiaSharp;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace TeheManX_Editor.Forms
{
    public partial class MainWindow : Window
    {
        #region Constants
        internal static readonly SKPaint MajorPaint = new SKPaint
        {
            Color = SKColors.Blue,
            StrokeWidth = 2,
            IsAntialias = false,
            Style = SKPaintStyle.Stroke,
            PathEffect = SKPathEffect.CreateDash([4, 4], 0)
        };
        internal static readonly SKPaint MinorPaint = new SKPaint
        {
            Color = SKColors.Yellow,
            StrokeWidth = 2,
            IsAntialias = false,
            Style = SKPaintStyle.Stroke,
            PathEffect = SKPathEffect.CreateDash([4, 4], 4)
        };
        internal static readonly SKPaint SelectPaint = new SKPaint
        {
            Color = new SKColor(255, 0, 0, 0xAF),
            StrokeWidth = 2,
            IsAntialias = false,
            Style = SKPaintStyle.Stroke,
            PathEffect = SKPathEffect.CreateDash([4, 4], 0)
        };

        #endregion Constants

        #region Fields
        public static MainWindow window;
        internal static LayoutWindow layoutWindow;
        internal static Settings settings = Settings.SetDefaultSettings();
        internal static Process emu;
        private static IRootDock loadedIdock;
        private static IDockSerializer serializer = new DockSerializer();
        private static IDockState dockState = new DockState();
        public static List<Undo> undos = new List<Undo>();
        internal static bool close;
        #endregion Fields

        #region Properties
        public LayoutEditor layoutE;
        public ScreenEditor screenE;
        public Tile32Editor tile32E;
        public Tile16Editor tile16E;
        public PaletteEditor paletteE;
        public EnemyEditor enemyE;
        public SpawnEditor spawnE;
        public CameraEditor camE;
        public TileEditor tileE;
        #endregion Properties

        #region Constructors
        public MainWindow()
        {
            InitializeComponent();
            window = this;

            layoutE = new LayoutEditor();
            layoutE.IsVisible = false;
            layoutDoc.Content = layoutE;

            screenE = new ScreenEditor();
            screenE.IsVisible = false;
            screenDoc.Content = screenE;

            tile32E = new Tile32Editor();
            tile32E.IsVisible = false;
            tile32Doc.Content = tile32E;

            tile16E = new Tile16Editor();
            tile16E.IsVisible = false;
            tile16Doc.Content = tile16E;

            paletteE = new PaletteEditor();
            paletteE.IsVisible = false;
            paletteDoc.Content = paletteE;

            enemyE = new EnemyEditor();
            enemyE.IsVisible = false;
            enemyDoc.Content = enemyE;

            spawnE = new SpawnEditor();
            spawnE.IsVisible = false;
            spawnDoc.Content = spawnE;

            camE = new CameraEditor();
            camE.IsVisible = false;
            cameraDoc.Content = camE;

            tileE = new TileEditor();
            tileE.IsVisible = false;
            vramDoc.Content = tileE;
            dockState.Save(mainDockControl.Layout);
            ////////

            //Open Settings
            if (File.Exists("Settings.json"))
            {
                try
                {
                    settings = JsonConvert.DeserializeObject<Settings>(File.ReadAllText("Settings.json"));
                }
                catch
                {
                }
            }

            if (File.Exists("Layout.json"))
            {
                try
                {
                    Layout layout = JsonConvert.DeserializeObject<Layout>(File.ReadAllText("Layout.json"));

                    if (layout != null && layout.Version == Layout.CurrentVersion)
                    {
                        mainDockControl.InitializeLayout = false;
                        loadedIdock = serializer.Deserialize<IRootDock?>(layout.DockJson);
                        dockState.Restore(loadedIdock);
                        mainDockControl.Layout = loadedIdock;

                        Position = new PixelPoint(layout.MainWindowX, layout.MainWindowY);
                        Width = layout.MainWindowWidth;
                        Height = layout.MainWindowHeight;
                        WindowState = (WindowState)layout.MainWindowState;

                        LayoutWindow.layoutLeft = layout.LayoutLeft;
                        LayoutWindow.layoutTop = layout.LayoutTop;
                        LayoutWindow.layoutWidth = layout.LayoutWidth;
                        LayoutWindow.layoutHeight = layout.LayoutHeight;
                        LayoutWindow.layoutState = layout.LayoutState;

                        ColorDialog.pickerLeft = layout.PickerLeft;
                        ColorDialog.pickerTop = layout.PickerTop;

                        ToolsWindow.mmxOpen = layout.MegaManXOpen;
                        ToolsWindow.mmx2Open = layout.MegaManX2Open;
                        ToolsWindow.mmx3Open = layout.MegaManX3Open;

                        Tile16Editor.scale = Math.Clamp(layout.ScaleVram, 1, Const.MaxScaleUI);

                        PaletteEditor.scale = Math.Clamp(layout.ScaleVram2, 1, Const.MaxScaleUI);

                        TileEditor.scale = Math.Clamp(layout.ScaleObjectVram, 1, Const.MaxScaleUI);

                        window.tileE.objectTilesImage.Width = TileEditor.scale * 128;
                        window.tileE.objectTilesImage.Height = TileEditor.scale * 128;

                        EnemyEditor.scale = Math.Clamp(layout.ScaleEnemy, 1, Const.MaxScaleUI);

                        tileE.romOffsetCheck.IsChecked = layout.UseRomOffset;
                        tileE.freshCheck.IsChecked = layout.RefreshBackground;
                        tileE.objectFreshCheck.IsChecked = layout.RefreshObject;
                    }
                }
                catch
                {
                }
            }

            AddHandler(InputElement.KeyDownEvent, Window_KeyDown, RoutingStrategies.Tunnel);

            DefineSizing();

            string[] args = Environment.GetCommandLineArgs();

            if (args.Length == 2) //Open Game Files using args
            {
                if (File.Exists(args[1]))
                {
                    //Validate SFC File
                    byte[] rom = null;

                    try
                    {
                        rom = File.ReadAllBytes(args[1]);
                    }
                    catch (Exception)
                    {
                        return;
                    }

                    if (!SNES.IsValidRom(rom))
                        return;

                    SNES.rom = rom;

                    //File Validated
                    SNES.date = File.GetLastWriteTime(args[1]);
                    SNES.savePath = args[1];
                }
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
            window.camE.AssignTriggerLimits();
            window.enemyE.DisableSelect();
            window.enemyE.DrawLayout();
            window.spawnE.SetSpawnSettings();
            window.tileE.AssignLimits();
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
        private async void MainKeyCheck(DockControl dock, KeyEventArgs e, Window win)
        {
            if (e.Key == Key.F11)
            {
                if (win.WindowState != WindowState.FullScreen)
                    win.WindowState = WindowState.FullScreen;
                else
                    win.WindowState = WindowState.Normal;
                return;
            }
            else if (e.Key == Key.O && e.KeyModifiers == KeyModifiers.Control)
            {
                OpenGame();
                return;
            }
            else if (e.Key == Key.S && SNES.rom != null && e.KeyModifiers == KeyModifiers.Control)
            {
                if (window.screenE.mode16)
                {
                    await MessageBox.Show(window, "You must exit 16x16 Mode before you can open another MegaMan X game!");
                    return;
                }
                await SaveGame();
                return;
            }
            else if (e.Key == Key.E && SNES.rom != null  && e.KeyModifiers == KeyModifiers.Control)
            {
                if (settings.EmuPath == "")
                {
                    await MessageBox.Show(window, "You must set the emulator path in the settings before using the test button");
                    return;
                }
                if (settings.SaveOnTest)
                {
                    if (window.screenE.mode16)
                    {
                        await MessageBox.Show(window, "You must exit 16x16 Mode before you can save the game!");
                        return;
                    }
                    if (!await SaveGame())
                    {
                        return;
                    }
                }
                try
                {
                    TestGame();
                }
                catch (System.ComponentModel.Win32Exception)
                {
                    await MessageBox.Show(window, "Cant find the Emulator EXE", "ERROR");
                }
                catch (Exception ex)
                {
                    await MessageBox.Show(window, ex.Message, "ERROR while Launching Emu");
                }
                return;
            }
            else if (e.Key == Key.Z && SNES.rom != null && e.KeyModifiers == KeyModifiers.Control)
            {
                ProcessUndo();
                return;
            }

            IDock layout = dock.Layout;
            IDockable focusedDoc = layout.FocusedDockable; //the current selected doc
            DocumentDock owner = focusedDoc.Owner as DocumentDock;

            /*Logic For Handling Switching Between Tabs*/
            bool docSwitch = false;
            int direction = 0;

            if (e.Key == Key.PageUp || (e.Key == Key.Left && e.KeyModifiers == KeyModifiers.Control))
            {
                docSwitch = true;
                direction = -1;
            }
            else if (e.Key == Key.PageDown || (e.Key == Key.Right && e.KeyModifiers == KeyModifiers.Control))
            {
                docSwitch = true;
                direction = 1;
            }

            if (docSwitch && owner != null && owner.VisibleDockables.Count > 1)
            {
                int index = 0;

                for (int i = 0; i < owner.VisibleDockables.Count; i++)
                {
                    if (owner.VisibleDockables[i] == owner.ActiveDockable)
                    {
                        index = ((i + direction) % owner.VisibleDockables.Count + owner.VisibleDockables.Count) % owner.VisibleDockables.Count;
                        break;
                    }
                }
                owner.ActiveDockable = owner.VisibleDockables[index];
                e.Handled = true;
                return;
            }

            if (SNES.rom == null)
                return;

            //Switch Between Stages
            if (e.Key == Key.F1)
            {
                e.Handled = true;
                if (window.screenE.mode16)
                {
                    await MessageBox.Show(window, "You must exit 16x16 Mode before you can switch stages!");
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
                await Level.LoadLevelTiles();
                Level.DecodeAllTiles();
                Level.AssignOffsets();
                Update();
                return;
            }
            else if (e.Key == Key.F2)
            {
                e.Handled = true;
                if (window.screenE.mode16)
                {
                    await MessageBox.Show(window, "You must exit 16x16 Mode before you can switch stages!");
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
                await Level.LoadLevelTiles();
                Level.DecodeAllTiles();
                Level.AssignOffsets();
                Update();
                return;
            }
            //Tab Specfic Hot Keys
            bool nonNumInt = false;
            IInputElement? inputElement = FocusManager.GetFocusedElement();

            if (inputElement != null)
            {
                Type type = FocusManager.GetFocusedElement().GetType();
                if (type != typeof(NumInt) && type != typeof(TextBox))
                    nonNumInt = true;
            }
            else
                nonNumInt = true;

            switch (focusedDoc.Id)
            {
                case "layoutTabId":
                    LayoutKeyCheck(e, nonNumInt);
                    break;
                case "screenTabId":
                    ScreenKeyCheck(e);
                    break;
                case "tile16TabId":
                    Tile16KeyCheck(e);
                    break;
                case "paletteTabId":
                    PaletteKeyCheck(e, nonNumInt);
                    break;
                case "enemyTabId":
                    EnemyKeyCheck(e, nonNumInt);
                    break;
                case "vramTabId":
                    TileKeyCheck(e, nonNumInt);
                    break;
                default:
                    break;
            }
        }
        private async void LayoutKeyCheck(KeyEventArgs e, bool notFocus)
        {
            if (e.Key == Key.Delete)
            {
                bool result = await MessageBox.Show(window, "Are you sure you want to delete all of Layer " + (Level.BG + 1) + "?\nThis cant be un-done", "WARNING", MessageBoxButton.YesNo);

                if (result)
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

            if (e.Key == Key.W)
            {
                if (window.layoutE.viewerY != 0)
                {
                    window.layoutE.viewerY -= 0x100;
                    window.layoutE.DrawLayout();
                }
            }
            else if (e.Key == Key.S)
            {
                if ((window.layoutE.viewerY >> 8) < (32 - 3))
                {
                    window.layoutE.viewerY += 0x100;
                    window.layoutE.DrawLayout();
                }
            }
            else if (e.Key == Key.D)
            {
                if ((window.layoutE.viewerX >> 8) < (32 - 3))
                {
                    window.layoutE.viewerX += 0x100;
                    window.layoutE.DrawLayout();
                }
            }
            else if (e.Key == Key.A)
            {
                if (window.layoutE.viewerX != 0)
                {
                    window.layoutE.viewerX -= 0x100;
                    window.layoutE.DrawLayout();
                }
            }
            else if (e.Key == Key.D1)
            {
                if (window.screenE.mode16)
                {
                    await MessageBox.Show(window, "You must exit 16x16 Mode before you can switch layers!");
                    return;
                }
                if (Level.BG != 0)
                {
                    Level.BG = 0;
                    Level.AssignOffsets();
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
            else if (e.Key == Key.D2)
            {
                if (window.screenE.mode16)
                {
                    await MessageBox.Show(window,"You must exit 16x16 Mode before you can switch layers!");
                    return;
                }
                if (Level.BG != 1)
                {
                    Level.BG = 1;
                    Level.AssignOffsets();
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
        private async void ScreenKeyCheck(KeyEventArgs e)
        {
            //Clear Screen
            if (e.Key == Key.Delete)
                await window.screenE.DeleteScreen(e.KeyModifiers == KeyModifiers.Shift);
        }
        private async void Tile16KeyCheck(KeyEventArgs e)
        {
            if (e.KeyModifiers == KeyModifiers.Shift)
            {
                bool update = false;
                if (e.Key == Key.OemPlus)
                {
                    Tile16Editor.scale = Math.Clamp(Tile16Editor.scale + 1, 1, Const.MaxScaleUI);
                    update = true;
                }
                else if (e.Key == Key.OemMinus)
                {
                    Tile16Editor.scale = Math.Clamp(Tile16Editor.scale - 1, 1, Const.MaxScaleUI);
                    update = true;
                }
                if (update)
                    window.tile16E.vramTileImage.InvalidateMeasure();
            }
            if (e.Key == Key.Delete)
            {
                bool result = await MessageBox.Show(window, "Are you sure you want to delete all of the 16x16 Tiles?\nThis cant be un-done", "WARNING", MessageBoxButton.YesNo);
                if (result)
                {
                    int Id = Const.Id == Const.GameId.MegaManX3 && Level.Id == 0xE ? 0x10 : Const.Id == Const.GameId.MegaManX3 && Level.Id > 0xE ? (Level.Id - 0xF) + 0xE : Level.Id;

                    int collisionOffset = SNES.CpuToOffset(BinaryPrimitives.ReadInt32LittleEndian(SNES.rom.AsSpan(Const.TileCollisionDataPointersOffset + Id * 3)));
                    Array.Clear(SNES.rom, collisionOffset, Const.Tile16Count[Level.Id, Level.BG]);

                    int tileDat16Offset = SNES.CpuToOffset(BinaryPrimitives.ReadInt32LittleEndian(SNES.rom.AsSpan(Const.Tile16DataPointersOffset[Level.BG] + Id * 3)));
                    Array.Clear(SNES.rom, tileDat16Offset, Const.Tile16Count[Level.Id, Level.BG] * 8);

                    SNES.edit = true;

                    //Clear Screen Undos of 16x16 Tile Tab
                    if (undos.Count != 0)
                    {
                        for (int i = (undos.Count - 1); i != -1; i--)
                        {
                            if (undos[i].type == Undo.UndoType.X16)
                                undos.RemoveAt(i);
                        }
                    }

                    window.tile16E.DrawTile();
                    window.tile16E.Draw16xTiles();
                    window.tile16E.UpdateTileAttributeUI();
                    window.tile16E.collisionInt.Value = 0;
                    window.layoutE.DrawLayout();
                    window.layoutE.DrawScreen();
                    window.enemyE.DrawLayout();
                    window.screenE.DrawScreen();
                    window.screenE.DrawTiles();
                    window.screenE.DrawTile();
                    window.tile32E.DrawTiles();
                    window.tile32E.Draw16xTiles();
                    window.tile32E.DrawTile();
                }
            }
        }
        private void PaletteKeyCheck(KeyEventArgs e, bool notFocus)
        {
            if (!notFocus)  //check if NumInt is focused
                return;

            bool update = false;
            if (e.Key == Key.W || e.Key == Key.Up)
            {
                window.paletteE.selectedSet--;
                update = true;
            }
            else if (e.Key == Key.S || e.Key == Key.Down)
            {
                window.paletteE.selectedSet++;
                update = true;
            }
            else if (e.KeyModifiers == KeyModifiers.Shift)
            {
                bool updateS = false;
                if (e.Key == Key.OemPlus)
                {
                    updateS = true;
                    PaletteEditor.scale = Math.Clamp(PaletteEditor.scale + 1, 1, Const.MaxScaleUI);
                }
                else if (e.Key == Key.OemMinus)
                {
                    updateS = true;
                    PaletteEditor.scale = Math.Clamp(PaletteEditor.scale - 1, 1, Const.MaxScaleUI);
                }
                if (updateS)
                    window.paletteE.vramTileImage.InvalidateMeasure();
            }

            if (update)
            {
                window.paletteE.selectedSet &= 7;
                window.paletteE.UpdatePaletteText();
                window.paletteE.DrawVramTiles();
                window.paletteE.UpdateCursor();
            }
        }
        private void EnemyKeyCheck(KeyEventArgs e, bool notFocus)
        {
            if (e.Key == Key.Delete && window.enemyE.selectedEnemy != null)
            {
                Level.Enemies[Level.Id].Remove(window.enemyE.selectedEnemy);
                window.enemyE.DisableSelect();
                window.enemyE.DrawLayout();
                SNES.edit = true;
                return;
            }
            if (!notFocus)  //check if NumInt is focused
                return;
            int speed;
            if (settings.InvertSpeed)
                speed = 1;
            else
                speed = 0x100;

            if (e.KeyModifiers == KeyModifiers.Shift)
            {
                if (settings.InvertSpeed)
                    speed = 0x100;
                else
                    speed = 1;
            }

            if (e.Key == Key.W)
            {
                window.enemyE.viewerY -= speed;
                if (window.enemyE.viewerY < 0)
                    window.enemyE.viewerY = 0;
                window.enemyE.DrawLayout();
            }
            else if (e.Key == Key.S)
            {
                window.enemyE.viewerY += speed;
                if (window.enemyE.viewerY > 0x1FFF)
                    window.enemyE.viewerY = 0x1FFF;
                window.enemyE.DrawLayout();
            }
            else if (e.Key == Key.D)
            {
                window.enemyE.viewerX += speed;
                if (window.enemyE.viewerX > 0x1FFF)
                    window.enemyE.viewerX = 0x1FFF;
                window.enemyE.DrawLayout();
            }
            else if (e.Key == Key.A)
            {
                window.enemyE.viewerX -= speed;
                if (window.enemyE.viewerX < 0)
                    window.enemyE.viewerX = 0;
                window.enemyE.DrawLayout();
            }
            else if (e.KeyModifiers == KeyModifiers.Shift)
            {
                if (e.Key == Key.OemPlus)
                {
                    window.enemyE.ZoomIn();
                }
                else if (e.Key == Key.OemMinus)
                {
                    window.enemyE.ZoomOut();
                }
            }
        }
        private void TileKeyCheck(KeyEventArgs e, bool notFocus)
        {
            if (!notFocus) //Check if num int is focused
                return;

            bool update = false;
            if (e.Key == Key.W || e.Key == Key.Up)
            {
                TileEditor.palId--;
                update = true;
            }
            else if (e.Key == Key.S || e.Key == Key.Down)
            {
                TileEditor.palId++;
                update = true;
            }
            else if (e.KeyModifiers == KeyModifiers.Shift)
            {
                bool updateS = false;
                if (e.Key == Key.OemPlus)
                {
                    TileEditor.scale = Math.Clamp(TileEditor.scale + 1, 1, Const.MaxScaleUI);
                    updateS = true;
                }
                else if (e.Key == Key.OemMinus)
                {
                    TileEditor.scale = Math.Clamp(TileEditor.scale - 1, 1, Const.MaxScaleUI);
                    updateS = true;
                }
                if (updateS)
                {
                    window.tileE.objectTilesImage.Width = TileEditor.scale * 128;
                    window.tileE.objectTilesImage.Height = TileEditor.scale * 128;
                }
            }

            if (update)
            {
                TileEditor.palId &= 7;
                window.tileE.DrawPalette();
                window.tileE.UpdateCursor();
                window.tileE.DrawObjectTiles();
            }
        }
        private async void OpenGame()
        {
            if (window.screenE.mode16)
            {
                await MessageBox.Show(window, "You must exit 16x16 Mode before you can open another MegaMan X game!");
                return;
            }

            IStorageProvider storageProvider = window.StorageProvider;

            IReadOnlyList<IStorageFile> result = await storageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Open an MegaMan X1-3 SFC File",
                FileTypeFilter = [new FilePickerFileType("SFC") { Patterns = ["*.sfc"] }],
                AllowMultiple = false
            });

            IStorageFile? file = result.FirstOrDefault();
            if (file != null)
            {
                //Validate SFC File
                byte[] rom = null;

                try
                {
                    FileInfo fileInfo = new FileInfo(file.Path.LocalPath);

                    if (fileInfo.Length > 0x800000)
                    {
                        await MessageBox.Show(window,"Invalid Rom Size", "ERROR");
                        return;
                    }
                    rom = await File.ReadAllBytesAsync(file.Path.LocalPath);
                }
                catch (Exception ex)
                {
                    await MessageBox.Show(window, ex.Message, "ERROR");
                    return;
                }

                if (!SNES.IsValidRom(rom))
                {
                    await MessageBox.Show(window,"Your ROM File is corrupted or you didn't  select the correct game.", "ERROR");
                    return;
                }

                SNES.rom = rom;
                SNES.edit = false;
                //File Validated
                SNES.date = File.GetLastWriteTime(file.Path.LocalPath);
                SNES.savePath = file.Path.LocalPath;
                await Level.LoadProject(Path.GetDirectoryName(SNES.savePath));
                await Level.LoadLevelData();
                Level.AssignOffsets();
                //Setup Editor
                undos.Clear();
                Level.Id = 0;
                Level.AssignPallete();
                window.tileE.CollectData();
                await Level.LoadLevelTiles();
                Level.DecodeAllTiles();
                window.paletteE.CollectData();
                window.spawnE.CollectData();
                window.camE.CollectData();
                window.enemyE.enemyListCanvas.InvalidateMeasure();
                window.enemyE.enemyScroll.Offset = new Vector(0, 0);
                Update();
                UnlockWindows();
            }
        }
        private void TestGame()
        {
            if (emu != null && !emu.HasExited)
            {
                emu.Kill();
                emu.Dispose();
            }

            ProcessStartInfo psi;

            if (settings.EmuPath.EndsWith(".bat", StringComparison.OrdinalIgnoreCase) ||
                settings.EmuPath.EndsWith(".cmd", StringComparison.OrdinalIgnoreCase))
            {
                psi = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c \"\"{settings.EmuPath}\" \"{SNES.savePath}\"\"",
                    UseShellExecute = false,
                    WorkingDirectory = Path.GetDirectoryName(settings.EmuPath)
                };
            }
            else
            {
                // EXE
                psi = new ProcessStartInfo
                {
                    FileName = settings.EmuPath,
                    Arguments = $"\"{SNES.savePath}\"",
                    UseShellExecute = false
                };
            }

            emu = Process.Start(psi);
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
        private async Task<bool> SaveGame()
        {
            if (window.screenE.mode16)
                return false;
            try
            {
                if (SNES.edit || !File.Exists(SNES.savePath))
                {
                    if (!await Level.SaveLayouts() || !await Level.SaveEnemyData() || !await Level.SaveProject())
                        return false;
                    await File.WriteAllBytesAsync(SNES.savePath, SNES.rom);
                    SNES.date = File.GetLastWriteTime(SNES.savePath);
                    SNES.edit = false;
                }
                else if (SNES.date != File.GetLastWriteTime(SNES.savePath))
                {
                    await File.WriteAllBytesAsync(SNES.savePath, SNES.rom);
                    SNES.date = File.GetLastWriteTime(SNES.savePath);
                    SNES.edit = false;
                }
            }
            catch (Exception ex)
            {
                await MessageBox.Show(window, ex.Message, "ERROR");
                return false;
            }
            return true;
        }
        public async Task SaveLayout()
        {
            string dockJson = serializer.Serialize(mainDockControl.Layout);

            Layout layout = new Layout
            {
                Version = Layout.CurrentVersion,

                MainWindowX = window.Position.X,
                MainWindowY = window.Position.Y,
                MainWindowWidth = (int)window.Width,
                MainWindowHeight = (int)window.Height,
                MainWindowState = (int)window.WindowState,

                DockJson = dockJson,

                LayoutLeft = LayoutWindow.layoutLeft,
                LayoutTop = LayoutWindow.layoutTop,
                LayoutWidth = LayoutWindow.layoutWidth,
                LayoutHeight = LayoutWindow.layoutHeight,
                LayoutState = LayoutWindow.layoutState,

                PickerLeft = ColorDialog.pickerLeft,
                PickerTop = ColorDialog.pickerTop,

                MegaManXOpen = ToolsWindow.mmxOpen,
                MegaManX2Open = ToolsWindow.mmx2Open,
                MegaManX3Open = ToolsWindow.mmx3Open,

                ScaleEnemy = EnemyEditor.scale,
                ScaleVram = Tile16Editor.scale,
                ScaleVram2 = PaletteEditor.scale,
                ScaleObjectVram = TileEditor.scale,

                UseRomOffset = tileE.romOffsetCheck.IsChecked == true,
                RefreshBackground = tileE.freshCheck.IsChecked == true,
                RefreshObject = tileE.objectFreshCheck.IsChecked == true
            };

            string json = JsonConvert.SerializeObject(layout);

            await File.WriteAllTextAsync("Layout.json", json);
        }
        private void LockWindow()
        {
            layoutE.IsVisible = false;
            screenE.IsVisible = false;
            tile32E.IsVisible = false;
            tile16E.IsVisible = false;
            paletteE.IsVisible = false;
            enemyE.IsVisible = false;
            spawnE.IsVisible = false;
            camE.IsVisible = false;
            tileE.IsVisible = false;
        }
        private void UnlockWindows()
        {
            layoutE.IsVisible = true;
            screenE.IsVisible = true;
            tile32E.IsVisible = true;
            tile16E.IsVisible = true;
            paletteE.IsVisible = true;
            enemyE.IsVisible = true;
            spawnE.IsVisible = true;
            camE.IsVisible = true;
            tileE.IsVisible = true;
        }
        internal void DefineSizing()
        {
            if (!settings.UseFixedScale)
            {
                int W;
                if (settings.ReferanceWidth < 200)
                    W = 40 * Screens.Primary.Bounds.Width / 100;
                else
                    W = 40 * settings.ReferanceWidth / 100;

                //Layout Tab
                window.layoutE.layoutImage.Width = double.NaN;
                window.layoutE.layoutImage.MaxWidth = double.PositiveInfinity;
                window.layoutE.layoutImage.Height = double.NaN;
                window.layoutE.layoutImage.MaxHeight = double.PositiveInfinity;
                window.layoutE.layoutImage.Width = double.NaN;
                window.layoutE.layoutImage.MaxWidth = double.PositiveInfinity;
                window.layoutE.layoutImage.Height = double.NaN;
                window.layoutE.layoutImage.MaxHeight = double.PositiveInfinity;
                window.layoutE.selectImage.Width = double.NaN;
                window.layoutE.selectImage.MaxWidth = W;
                window.layoutE.selectImage.Height = double.NaN;
                window.layoutE.selectImage.MaxHeight = double.PositiveInfinity;

                //Screen Tab
                window.screenE.screenImage.Width = double.NaN;
                window.screenE.screenImage.MaxWidth = double.PositiveInfinity;
                window.screenE.screenImage16.Width = double.NaN;
                window.screenE.screenImage16.MaxWidth = double.PositiveInfinity;

                window.screenE.screenImage.Height = double.NaN;
                window.screenE.screenImage.MaxHeight = double.PositiveInfinity;
                window.screenE.screenImage16.Height = double.NaN;
                window.screenE.screenImage16.MaxHeight = double.PositiveInfinity;

                window.screenE.tileImage.Width = double.NaN;
                window.screenE.tileImage.MaxWidth = W;
                window.screenE.tileImage16.Width = double.NaN;
                window.screenE.tileImage16.MaxWidth = W;

                window.screenE.tileImage.Height = double.NaN;
                window.screenE.tileImage.MaxHeight = double.PositiveInfinity;
                window.screenE.tileImage16.Height = double.NaN;
                window.screenE.tileImage16.MaxHeight = double.PositiveInfinity;

                //Tile 32x32 Tab
                window.tile32E.tileImage.Width = double.NaN;
                window.tile32E.tileImage.MaxWidth = double.PositiveInfinity;

                window.tile32E.tileImage.Height = double.NaN;
                window.tile32E.tileImage.MaxHeight = double.PositiveInfinity;

                window.tile32E.x16Image.Width = double.NaN;
                window.tile32E.x16Image.MaxWidth = W;

                window.tile32E.x16Image.Height = double.NaN;
                window.tile32E.x16Image.MaxHeight = double.PositiveInfinity;

                window.tile32E.mainGrid.ColumnDefinitions[0].Width = new GridLength(1, GridUnitType.Star);

                //Tile 16x16 Tab
                window.tile16E.x16Image.Width = double.NaN;
                window.tile16E.x16Image.MaxWidth = double.PositiveInfinity;
                window.tile16E.x16Image.Height = double.NaN;
                window.tile16E.x16Image.MaxHeight = double.PositiveInfinity;
            }
            else
            {
                double main = 768 * settings.LayoutScale;
                double sub = 256 * settings.LayoutScreenScale;

                //Layout Tab
                window.layoutE.layoutImage.Width = main;
                window.layoutE.layoutImage.MaxWidth = main;
                window.layoutE.layoutImage.Height = main;
                window.layoutE.layoutImage.MaxHeight = main;
                window.layoutE.layoutImage.Width = main;
                window.layoutE.layoutImage.Height = main;

                window.layoutE.selectImage.Width = sub;
                window.layoutE.selectImage.MaxWidth = sub;
                window.layoutE.selectImage.Height = sub;
                window.layoutE.selectImage.MaxHeight = sub;

                //Screen Tab
                main = 256 * settings.ScreenScale;
                window.screenE.screenImage.Width = main;
                window.screenE.screenImage.MaxWidth = main;
                window.screenE.screenImage.Height = main;
                window.screenE.screenImage.MaxHeight = main;
                window.screenE.screenImage16.Width = main;
                window.screenE.screenImage16.MaxWidth = main;
                window.screenE.screenImage16.Height = main;
                window.screenE.screenImage16.MaxHeight = main;

                main = 256 * settings.ScreenTilesScale;
                window.screenE.tileImage.Width = main;
                window.screenE.tileImage.MaxWidth = main;
                window.screenE.tileImage.Height = 1024 * settings.ScreenTilesScale;
                window.screenE.tileImage.MaxHeight = 1024 * settings.ScreenTilesScale;
                window.screenE.tileImage16.Width = main;
                window.screenE.tileImage16.MaxWidth = main;
                window.screenE.tileImage16.Height = main;
                window.screenE.tileImage16.MaxHeight = main;

                //Tile 32x32 Tab
                main = 256 * settings.Tile32Scale;
                sub = 256 * settings.Tile32Image16Scale;
                window.tile32E.tileImage.Width = main;
                window.tile32E.tileImage.MaxWidth = main;
                window.tile32E.tileImage.Height = 1024 * settings.Tile32Scale;
                window.tile32E.tileImage.MaxHeight = 1024 * settings.Tile32Scale; ;
                window.tile32E.x16Image.Width = sub;
                window.tile32E.x16Image.MaxWidth = sub;
                window.tile32E.x16Image.Height = sub;
                window.tile32E.x16Image.MaxHeight = sub;

                window.tile32E.mainGrid.ColumnDefinitions[0].Width = GridLength.Auto;

                main = 256 * settings.Tile16Scale;
                window.tile16E.x16Image.Width = main;
                window.tile16E.x16Image.MaxWidth = main;
                window.tile16E.x16Image.Height = main;
                window.tile16E.x16Image.MaxHeight = main;
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static float Snap(float v) => (float)Math.Floor(v) + 0.5f;
        internal static void DrawGrid(SKCanvas canvas, float width, float height, int columnCount, int rowCount)
        {
            float cellWidth = width / columnCount;
            float cellHeight = height / rowCount;

            // Draw all vertical lines
            for (int x = 0; x <= (columnCount - 1); x++)
            {
                if (x == 0) continue;
                float px = Snap(x * cellWidth);

                canvas.DrawLine(px, 0, px, height, MajorPaint);
                canvas.DrawLine(px, 0, px, height, MinorPaint);
            }

            // Draw all horizontal lines
            for (int y = 0; y <= (rowCount - 1); y++)
            {
                if (y == 0) continue;
                float py = Snap(y * cellHeight);

                canvas.DrawLine(0, py, width, py, MajorPaint);
                canvas.DrawLine(0, py, width, py, MinorPaint);
            }
        }
        internal static void DrawSelect(SKCanvas canvas, float width, float height, int columnCount, int rowCount, int x, int y)
        {
            float cellWidth = width / columnCount;
            float cellHeight = height / rowCount;
            float px = Snap(x * cellWidth);
            float py = Snap(y * cellHeight);

            canvas.DrawRect(px, py, cellWidth, cellHeight, SelectPaint);
        }
        internal static void DrawSelect(SKCanvas canvas, float width, float height, int columnCount, int rowCount, int x, int y, int columnAmount, int rowAmount)
        {
            float cellWidth = width / columnCount;
            float cellHeight = height / rowCount;
            float px = Snap(x * cellWidth);
            float py = Snap(y * cellHeight);

            canvas.DrawRect(px, py, cellWidth * columnAmount, cellHeight * rowAmount, SelectPaint);
        }
        #endregion Methods

        #region Events
        private void openBtn_Click(object sender, RoutedEventArgs e)
        {
            OpenGame();
        }
        private async void saveBtn_Click(object sender, RoutedEventArgs e)
        {
            if (SNES.rom == null)
                return;
            if (window.screenE.mode16)
            {
                await MessageBox.Show(window, "You must exit 16x16 Mode before you can save the game!");
                return;
            }
            await SaveGame();
        }
        private async void saveAsBtn_Click(object sender, RoutedEventArgs e)
        {
            if (window.screenE.mode16)
            {
                await MessageBox.Show(window, "You must exit 16x16 Mode before you can save the game!");
                return;
            }
            if (SNES.rom == null || !await Level.SaveLayouts() || !await Level.SaveEnemyData() || !await Level.SaveProject(false))
                return;
            IStorageProvider storageProvider = window.StorageProvider;
            IStorageFile file = await storageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Select Save Location",
                FileTypeChoices = [new FilePickerFileType("SFC") { Patterns = ["*.sfc"] }],
                ShowOverwritePrompt = true
            });

            if (file != null)
            {
                try
                {
                    await File.WriteAllBytesAsync(file.Path.LocalPath, SNES.rom);
                    SNES.savePath = file.Path.LocalPath;
                    await Level.SaveProject();
                    SNES.date = File.GetLastWriteTime(file.Path.LocalPath);
                    SNES.edit = false;
                }
                catch (Exception ex)
                {
                    await MessageBox.Show(window, ex.Message, "ERROR");
                }
            }
        }
        private async void testBtn_Click(object sender, RoutedEventArgs e)
        {
            if (SNES.rom == null)
                return;
            if (settings.EmuPath == "")
            {
                await MessageBox.Show(window, "You must set the emulator/script path in the settings before using the test button");
                return;
            }
            if (settings.SaveOnTest)
            {
                if (window.screenE.mode16)
                {
                    await MessageBox.Show(window, "You must exit 16x16 Mode before you can save the game!");
                    return;
                }
                if (!await SaveGame())
                {
                    return; //TODO: maybe add in a message or something...
                }
            }
            try
            {
                TestGame();
            }
            catch (System.ComponentModel.Win32Exception)
            {
                await MessageBox.Show(window ,"Cant find the Emulator EXE", "ERROR");
            }
            catch (Exception ex)
            {
                await MessageBox.Show(window, ex.Message, "ERROR while Launching Emu");
            }
        }
        private async void toolsBtn_Click(object sender, RoutedEventArgs e)
        {
            ToolsWindow t = new ToolsWindow();
            await t.ShowDialog(window);
        }
        private async void aboutBtn_Click(object sender, RoutedEventArgs e)
        {
            AboutWindow about = new AboutWindow();
            await about.ShowDialog(window);
        }
        private async void settingsBtn_Click(object sender, RoutedEventArgs e)
        {
            SettingsWindow settings = new SettingsWindow();
            await settings.ShowDialog(window);
        }
        private async void stagesBtn_Click(object sender, RoutedEventArgs e)
        {
            if (SNES.rom == null)
                return;

            if (window.screenE.mode16)
            {
                await MessageBox.Show(window, "You must exit 16x16 Mode before you can switch stages!");
                return;
            }

            // Create a new context menu
            ContextMenu menu = new ContextMenu();

            // Create Menu Items
            for (int i = 0; i < Const.LevelsCount; i++)
            {
                MenuItem item = new MenuItem() { Header = $"STAGE {i:X2}" };
                item.Tag = i;
                item.Click += async (s, args) =>
                {
                    Level.Id = ((MenuItem)s).Tag is int id ? id : 0;
                    //Re-Update
                    undos.Clear();
                    Level.TileSet = 0;
                    Level.AssignPallete();
                    await Level.LoadLevelTiles();
                    Level.DecodeAllTiles();
                    Level.AssignOffsets();
                    Update();
                };
                menu.Items.Add(item);
            }

            // Attach the menu to the button
            stagesBtn.ContextMenu = menu;

            // Show the menu immediately
            menu.PlacementTarget = stagesBtn;
            menu.Open();
        }
        private async void projectBtn_Click(object sender, RoutedEventArgs e)
        {
            if (SNES.rom == null)
                return;
            ProjectWindow project = new ProjectWindow();
            await project.ShowDialog(window);
        }
        private async void helpBtn_Click(object sender, RoutedEventArgs e)
        {
            HelpWindow h = new HelpWindow(0);
            await h.ShowDialog(window);
        }
        private void undoBtn_Click(object sender, RoutedEventArgs e)
        {
            ProcessUndo();
        }
        private async void Window_Loaded(object? sender, RoutedEventArgs e)
        {
            if (loadedIdock != null)
                mainDockControl.Factory.InitLayout(loadedIdock);

            if (SNES.rom != null)
            {
                await Level.LoadProject(Path.GetDirectoryName(SNES.savePath));
                await Level.LoadLevelData();
                Level.AssignOffsets();
                //Setup Editor
                Level.AssignPallete();
                window.tileE.CollectData();
                await Level.LoadLevelTiles();
                Level.DecodeAllTiles();
                window.paletteE.CollectData();
                window.spawnE.CollectData();
                window.camE.CollectData();
                window.enemyE.enemyListCanvas.InvalidateMeasure();
                window.enemyE.enemyScroll.Offset = new Vector(0, 0);
                Update();
                UnlockWindows();
            }

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
                    dynamic release = JsonConvert.DeserializeObject(json);
                    string tag = release.tag_name;
                    if (tag != Const.EditorVersion && !Settings.IsPastVersion(tag))
                    {
                        bool result = await MessageBox.Show(window, $"There is a new version of this editor ({tag}) do you want to download the update?", "New Version", MessageBoxButton.YesNo);
                        if (result)
                        {
                            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                            {
                                await MessageBox.Show(window, "You must download the update your self becuase the editor does not\nsupport downloading updates with your current operating system.");
                                return;
                            }
                            bool is64BitOS = Environment.Is64BitOperatingSystem;
                            string match = is64BitOS ? "Win64" : "Win32";

                            //Start Looking for Url to download From

                            string url = null;

                            foreach (var asset in release.assets)
                            {
                                string name = asset.name;
                                string tempUrl = asset.browser_download_url;
                                if (name.Contains(match, StringComparison.CurrentCultureIgnoreCase))
                                {
                                    url = asset.browser_download_url;
                                    break;
                                }
                            }

                            string tempPath = Path.GetTempPath();

                            if (url != null)
                            {
                                //
                                string powerShellScript = @"
param (
    [Parameter(Mandatory = $true)]
    [string]$Url,

    [Parameter(Mandatory = $true)]
    [string]$TargetExe
)

Write-Host ""Press any key to start updating...""
$null = $Host.UI.RawUI.ReadKey(""NoEcho,IncludeKeyDown"")

$tempExe = ""$TargetExe.new""

Write-Host ""Downloading update...""
Invoke-WebRequest -Uri $Url -OutFile $tempExe

Write-Host ""Waiting for application to exit...""
while (Get-Process | Where-Object { $_.Path -eq $TargetExe } -ErrorAction SilentlyContinue) {
    Start-Sleep -Milliseconds 500
}

Write-Host ""Replacing executable...""
Move-Item -Path $tempExe -Destination $TargetExe -Force

Write-Host ""Launching updated application...""
Start-Process -FilePath $TargetExe
";
                                await File.WriteAllTextAsync($"{tempPath}/TeheDownload.ps1", powerShellScript);

                                string exePath = Environment.ProcessPath!;

                                string scriptPath = Path.Combine(tempPath, "TeheDownload.ps1");
                                string updateUrl = url;

                                var psi = new ProcessStartInfo
                                {
                                    FileName = "powershell.exe",
                                    Arguments =
                                        $"-NoProfile -ExecutionPolicy Bypass -File \"{scriptPath}\" " +
                                        $"\"{updateUrl}\" \"{exePath}\"",
                                    UseShellExecute = false,
                                    CreateNoWindow = false
                                };

                                Process.Start(psi);

                                // exit so the EXE can be replaced
                                Environment.Exit(0);
                            }
                        }
                    }
                }
                catch (HttpRequestException)
                {
                }
                catch (Exception ex)
                {
                    await MessageBox.Show(window, ex.Message, "ERROR");
                }
            }
        }
        private void Window_PointerPressed(object? sender, PointerPressedEventArgs e)
        {
            mainDockControl.Focus(); //This is to make text boxes and numeric up/downs lose focus
        }
        private async void Window_Closing(object? sender, WindowClosingEventArgs e)
        {
            /*Some Wird Logic to get around Async + Await*/
            if (!close)
            {
                e.Cancel = true;

                if (SNES.edit)
                {
                    bool results = await MessageBox.Show(this, "You have edited the game without saving.\nAre you sure you want to exit the editor?", "WARNING", MessageBoxButton.YesNo);
                    if (!results)
                    {
                        e.Cancel = true;
                        return;
                    }
                }

                await SaveLayout();
                close = true;
                this.Close();
            }
            if (LayoutWindow.isOpen)
                layoutWindow.Close();
        }
        private void Window_KeyDown(object? sender, KeyEventArgs e)
        {
            MainKeyCheck(mainDockControl, e, window);
        }
        private async void Factory_DockableClosed(object? sender, Dock.Model.Core.Events.DockableClosedEventArgs e)
        {
            if (close) return;

            DocumentDock document = e.Dockable as DocumentDock;

            if (document == null) return;

            DockControl child = null;

            foreach (var item in window.grid.Children)
            {
                if (item.GetType() == typeof(DockControl))
                    child = (DockControl)item;
            }

            DocumentDock mainDocument = null;
            IDock idock = child.Layout;
            IList<IDockable>? dockableList = idock.VisibleDockables;


        SearchLoop:
            foreach (IDockable dockable in dockableList)
            {
                if (dockable.GetType() == typeof(DocumentDock))
                {
                    mainDocument = dockable as DocumentDock;
                }
                else if (dockable.GetType() == typeof(ProportionalDock))
                {
                    ProportionalDock proportional = dockable as ProportionalDock;
                    dockableList = proportional.VisibleDockables;
                    goto SearchLoop;
                }

                if (mainDocument != null)
                    break;
            }

            //Add back to Main Window
            foreach (IDockable dockable in document.VisibleDockables)
            {
                mainDocument.AddDocument(dockable);
            }
        }
        private void Factory_WindowOpened(object? sender, Dock.Model.Core.Events.WindowOpenedEventArgs e)
        {
            Window win = e.Window.Host as Window;
            Config.App app = (Config.App)Application.Current;
            if (app.RequestedThemeVariant == Avalonia.Styling.ThemeVariant.Light)
                win.Background = Brushes.White;
            else
                win.Background = Brushes.Black;

            win.Opened += SubWindowOpened;
            win.AddHandler(InputElement.KeyDownEvent, SubWindowKeyDown, Avalonia.Interactivity.RoutingStrategies.Tunnel);
            win.PointerPressed += SubWindowPointerPressed;
        }
        private void SubWindowPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            HostWindow hostWin = sender as HostWindow;
            Panel outerPanel = hostWin.Content as Panel;
            Panel innerPanel = outerPanel.Children[0] as Panel;
            DockControl dockControl = innerPanel.Children[0] as DockControl;
            dockControl.Focus(); //This is to make text boxes and numeric up/downs lose focus
        }
        private async void SubWindowKeyDown(object? sender, KeyEventArgs e)
        {
            HostWindow hostWin = sender as HostWindow;

            Panel outerPanel = hostWin.Content as Panel;
            Panel innerPanel = outerPanel.Children[0] as Panel;

            MainKeyCheck(innerPanel.Children[0] as DockControl, e, hostWin);
        }
        private void SubWindowOpened(object? sender, EventArgs e)
        {
            HostWindow hostWin = sender as HostWindow;
            hostWin.Title = "Tehe SubWindow";

            Panel outerPanel = hostWin.Content as Panel;
            Panel innerPanel = outerPanel.Children[0] as Panel;
            DockControl dockControl = innerPanel.Children[0] as DockControl;
            dockControl.Focusable = true;
            dockControl.FocusAdorner = null;
        }
        #endregion Events
    }
}