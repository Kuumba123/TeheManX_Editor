using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace TeheManX_Editor.Forms
{
    /// <summary>
    /// Interaction logic for EnemyEditor.xaml
    /// </summary>
    public partial class EnemyEditor : UserControl
    {
        #region Constants
        enum MouseState
        {
            None,
            Pan,            //Pan Camera
            Move,           //Enemy Move
            TriggerSelect   //Select Camera Trigger
        }
        #endregion Constants

        #region Fields
        static List<Rectangle> triggerRects = new List<Rectangle>();
        static List<EnemyLabel> enemyLabels = new List<EnemyLabel>();
        internal static WriteableBitmap layoutBMP = new WriteableBitmap(768, 768, 96, 96, PixelFormats.Bgra32, null);
        public static double scale = 2;
        #endregion Fields

        #region Properties
        public bool update;
        public int viewerX = 0x400;
        public int viewerY = 0;
        public Enemy selectedEnemy;
        MouseState mouseState;
        int mouseStartX;
        int mouseStartY;
        int referanceStartX;
        int referanceStartY;
        #endregion Properties

        #region Constructors
        public EnemyEditor()
        {
            InitializeComponent();

            layoutImage.Source = layoutBMP;
        }
        #endregion Constructors

        #region Methods
        public void DrawLayout()
        {
            update = true;
        }
        public void DrawEnemies()
        {
            for (int i = 0; i < enemyLabels.Count; i++)
                enemyLabels[i].Visibility = Visibility.Collapsed;

            for (int i = 0; i < triggerRects.Count; i++)
                triggerRects[i].Visibility = Visibility.Collapsed;

            if (Level.Id >= Const.PlayableLevelsCount || (Const.Id == Const.GameId.MegaManX3 && Level.Id > 0xE))
                return;

            while (enemyLabels.Count < Level.Enemies[Level.Id].Count)
            {
                var r = new EnemyLabel();
                enemyLabels.Add(r);
                MainWindow.window.enemyE.canvas.Children.Add(r);
            }

            for (int i = 0; i < Level.Enemies[Level.Id].Count; i++) //Update Each Enemy
            {
                enemyLabels[i].text.Text = Level.Enemies[Level.Id][i].Id.ToString("X");
                enemyLabels[i].AssignTypeBorder(Level.Enemies[Level.Id][i].Type);
                Canvas.SetLeft(enemyLabels[i], Level.Enemies[Level.Id][i].X - viewerX);
                Canvas.SetTop(enemyLabels[i], Level.Enemies[Level.Id][i].Y - viewerY);
                enemyLabels[i].Visibility = Visibility.Visible;
            }

            while (triggerRects.Count < CameraEditor.CameraTriggers[Level.Id].Count) //Add to Canvas
            {
                Rectangle r = new Rectangle()
                {
                    IsHitTestVisible = false,
                    StrokeThickness = 1,
                    Stroke = Brushes.Green,
                    Fill = new SolidColorBrush(Color.FromArgb(96, 0xAD, 0xD8, 0xE6))
                };
                triggerRects.Add(r);
                MainWindow.window.enemyE.canvas.Children.Add(r);
            }

            if (!MainWindow.window.camE.triggersEnabled)
                return;

            int id = Level.Id;

            for (int i = 0; i < CameraEditor.CameraTriggers[id].Count; i++)
            {
                int rightSide = CameraEditor.CameraTriggers[id][i].RightSide;
                int leftSide = CameraEditor.CameraTriggers[id][i].LeftSide;
                int bottomSide = CameraEditor.CameraTriggers[id][i].BottomSide;
                int topSide = CameraEditor.CameraTriggers[id][i].TopSide;

                int width = rightSide - leftSide;
                int height = bottomSide - topSide;

                if (width < 1) width = 1;
                if (height < 1) height = 1;

                triggerRects[i].Width = width;
                triggerRects[i].Height = height;

                Canvas.SetLeft(triggerRects[i], leftSide - viewerX);
                Canvas.SetTop(triggerRects[i], topSide - viewerY);

                triggerRects[i].Visibility = Visibility.Visible;
            }
        }
        public void Paint()
        {
            update = false;
            layoutBMP.Lock();

            int bmpWidth = layoutBMP.PixelWidth;
            int bmpHeight = layoutBMP.PixelHeight;
            nint bufferP = layoutBMP.BackBuffer;
            int stride = layoutBMP.BackBufferStride;

            int tileX = viewerX >> 8;   // 256-pixel screen index
            int tileY = viewerY >> 8;
            int offX = viewerX & 0xFF;  // sub-screen offset (0–255)
            int offY = viewerY & 0xFF;

            unsafe
            {
                uint* ptr = (uint*)bufferP;
                for (int i = 0; i < (768 * 768); i++)
                {
                    *ptr = 0xFF000000;
                    ptr++;
                }
            }

            for (int sy = 0; sy < 4; sy++)
            {
                for (int sx = 0; sx < 4; sx++)
                {
                    int screenIndexX = tileX + sx;
                    int screenIndexY = tileY + sy;

                    // bounds check so we never index outside Layout
                    if (screenIndexX < 0 || screenIndexX >= 32) continue;
                    if (screenIndexY < 0 || screenIndexY >= 32) continue;

                    int layoutIndex = screenIndexY * 32 + screenIndexX;

                    int drawX = sx * 256 - offX;
                    int drawY = sy * 256 - offY;

                    bool fullyInside =
                        drawX >= 0 &&
                        drawY >= 0 &&
                        (drawX + 256) <= bmpWidth &&
                        (drawY + 256) <= bmpHeight;

                    if (fullyInside)
                    {
                        // non-clamped version (no bmpWidth/bmpHeight args)
                        Level.DrawScreen(
                            Level.Layout[Level.Id, Level.BG, layoutIndex],
                            drawX, drawY,
                            stride,
                            bufferP
                        );
                    }
                    else
                    {
                        // partially outside - use clamped version
                        Level.DrawScreen_Clamped(
                            Level.Layout[Level.Id, Level.BG, layoutIndex],
                            drawX, drawY,
                            stride,
                            bufferP,
                            bmpWidth, bmpHeight
                        );
                    }
                }
            }

            layoutBMP.AddDirtyRect(new Int32Rect(0, 0, 768, 768));
            layoutBMP.Unlock();
        }
        public void UpdateEnemyLabelPositions()
        {
            DrawEnemies();
        }
        private void SelectEnemy(Enemy en)
        {
            selectedEnemy = en;
            MainWindow.window.enemyE.columnInt.Value = en.Column;
            MainWindow.window.enemyE.idInt.Value = en.Id;
            MainWindow.window.enemyE.varInt.Value = en.SubId;
            MainWindow.window.enemyE.typeInt.Value = en.Type;
            MainWindow.window.enemyE.xInt.Value = en.X;
            MainWindow.window.enemyE.yInt.Value = en.Y;

            MainWindow.window.enemyE.idInt.IsEnabled = true;
            MainWindow.window.enemyE.varInt.IsEnabled = true;
            MainWindow.window.enemyE.typeInt.IsEnabled = true;
            MainWindow.window.enemyE.xInt.IsEnabled = true;
            MainWindow.window.enemyE.yInt.IsEnabled = true;
            MainWindow.window.enemyE.columnInt.IsEnabled = true;
        }
        public void DisableSelect() //Disable editing Enemy Properties
        {
            selectedEnemy = null;
            //Disable
            MainWindow.window.enemyE.idInt.IsEnabled = false;
            MainWindow.window.enemyE.varInt.IsEnabled = false;
            MainWindow.window.enemyE.typeInt.IsEnabled = false;
            MainWindow.window.enemyE.xInt.IsEnabled = false;
            MainWindow.window.enemyE.yInt.IsEnabled = false;
            MainWindow.window.enemyE.columnInt.IsEnabled = false;
            //MainWindow.window.enemyE.nameLbl.Content = "";
        }
        private bool ValidEnemyAdd()
        {
           if (Level.Id >= Const.PlayableLevelsCount || (Const.Id == Const.GameId.MegaManX3 && Level.Id > 0xE))
            {
                MessageBox.Show("Enemies cannot be added to this level.", "Error");
                return false;
            }
            if (Level.Enemies[Level.Id].Count == 0xCC)
            {
                MessageBox.Show("The max amount of enemies you can put in a level is 0xCC.", "Error");
                return false;
            }
            return true;
        }
        #endregion Methods

        #region Events
        private void canvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Point pos = e.GetPosition(MainWindow.window.enemyE.canvas);

            if (e.MouseDevice.MiddleButton == MouseButtonState.Pressed)
            {
                mouseStartX = (int)(pos.X / 1);
                mouseStartY = (int)(pos.Y / 1);
                referanceStartX = viewerX;
                referanceStartY = viewerY;
                mouseState = MouseState.Pan;
                Mouse.OverrideCursor = Cursors.ScrollAll;
                return;
            }

            if (Level.Id >= Const.PlayableLevelsCount || (Const.Id == Const.GameId.MegaManX3 && Level.Id > 0xE))
                return;

            if (e.MouseDevice.LeftButton == MouseButtonState.Pressed && (bool)MainWindow.window.camE.triggerCheck.IsChecked && MainWindow.window.camE.triggersEnabled)
            {
                mouseStartX = (int)(pos.X / 1) + viewerX;
                mouseStartY = (int)(pos.Y / 1) + viewerY;
                mouseState = MouseState.TriggerSelect;
            }
            else if (e.MouseDevice.LeftButton == MouseButtonState.Pressed || e.MouseDevice.RightButton == MouseButtonState.Pressed)
            {
                bool leftPressed = e.LeftButton == MouseButtonState.Pressed;

                const int width = 16;
                const int height = 16;
                const int offsetX = 0;
                const int offsetY = 0;

                int id = Level.Id;

                int clickX = (int)(pos.X / 1) + viewerX;
                int clickY = (int)(pos.Y / 1) + viewerY;

                Enemy en;

                for (int i = 0; i < Level.Enemies[id].Count; i++)
                {
                    en = Level.Enemies[id][i];

                    //Bounding Box Check
                    if (clickX >= (en.X + offsetX) && clickX <= (en.X + width + offsetX) && clickY >= (en.Y + offsetY) && clickY <= (en.Y + height + offsetY))
                    {
                        if (leftPressed) // Move Enemy
                        {
                            mouseStartX = clickX;
                            mouseStartY = clickY;
                            mouseState = MouseState.Move;
                            referanceStartX = en.X;
                            referanceStartY = en.Y;
                            SelectEnemy(en);
                        }
                        else //...
                        {

                        }
                    }
                }
            }
        }
        private void canvas_MouseMove(object sender, MouseEventArgs e)
        {
            Point pos = e.GetPosition(MainWindow.window.enemyE.canvas);
            int mouseX = (int)(Math.Round(pos.X) / 1) + viewerX;
            int mouseY = (int)(Math.Round(pos.Y) / 1) + viewerY;

            if (mouseState == MouseState.TriggerSelect)
            {
                int leftSide = Math.Clamp(Math.Min(mouseX + -viewerX, mouseStartX + -viewerX), 0, 0x1FFF);
                int rightSide = Math.Clamp(Math.Max(mouseX + -viewerX, mouseStartX + -viewerX), 0, 0x1FFF);
                int topSide = Math.Clamp(Math.Min(mouseY + -viewerY, mouseStartY + -viewerY), 0, 0x1FFF);
                int bottomSide = Math.Clamp(Math.Max(mouseY + -viewerY, mouseStartY + -viewerY), 0, 0x1FFF);

                Canvas.SetLeft(selectRect, leftSide);
                selectRect.Width = rightSide - leftSide;

                Canvas.SetTop(selectRect, topSide);
                selectRect.Height = bottomSide - topSide;
                selectRect.Visibility = Visibility.Visible;
            }
            else if (mouseState == MouseState.Move)
            {
                short locationX = (short)Math.Clamp((mouseX + -mouseStartX) + referanceStartX, 0, 0x1FFF);
                short locationY = (short)Math.Clamp((mouseY + -mouseStartY) + referanceStartY, 0, 0x1FFF);

                // Snap to 16-pixel grid when holding SHIFT
                if (Keyboard.IsKeyDown(Key.LeftShift))
                {
                    locationX = (short)(locationX & 0xFFF8); // snap X to multiple of 8
                    locationY = (short)(locationY & 0xFFF8); // snap Y to multiple of 8
                }

                selectedEnemy.X = locationX;
                selectedEnemy.Y = locationY;

                byte column = (byte)(locationX / 32);
                selectedEnemy.Column = column;

                MainWindow.window.enemyE.xInt.Value = locationX;
                MainWindow.window.enemyE.yInt.Value = locationY;
                MainWindow.window.enemyE.columnInt.Value = column;

                SNES.edit = true;
                UpdateEnemyLabelPositions();
            }
            else if (mouseState == MouseState.Pan)
            {
                int screenX = (int)Math.Round(pos.X);
                int screenY = (int)Math.Round(pos.Y);

                int worldX = screenX + viewerX;
                int worldY = screenY + viewerY;

                viewerX = (short)Math.Clamp(referanceStartX - (screenX - mouseStartX),0, 0x1FFF);

                viewerY = (short)Math.Clamp(referanceStartY - (screenY - mouseStartY),0, 0x1FFF);

                MainWindow.window.UpdateEnemyViewerCam();
                DrawLayout();
                DrawEnemies();
            }
        }
        private void canvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (mouseState == MouseState.TriggerSelect)
            {
                selectRect.Visibility = Visibility.Collapsed;

                int id = Level.Id;

                if (CameraEditor.CameraTriggers[id].Count == 0)
                    return;

                Point pos = e.GetPosition(MainWindow.window.enemyE.canvas);
                int mouseX = (int)(Math.Round(pos.X) / 1) + viewerX;
                int mouseY = (int)(Math.Round(pos.Y) / 1) + viewerY;

                int leftSide = Math.Clamp(Math.Min(mouseX, mouseStartX), 0, 0x1FFF);
                int rightSide = Math.Clamp(Math.Max(mouseX, mouseStartX), 0, 0x1FFF);
                int topSide = Math.Clamp(Math.Min(mouseY, mouseStartY), 0, 0x1FFF);
                int bottomSide = Math.Clamp(Math.Max(mouseY, mouseStartY), 0, 0x1FFF);

                CameraEditor.CameraTriggers[id][CameraEditor.cameraTriggerId].RightSide = (ushort)rightSide;
                CameraEditor.CameraTriggers[id][CameraEditor.cameraTriggerId].LeftSide = (ushort)leftSide;
                CameraEditor.CameraTriggers[id][CameraEditor.cameraTriggerId].BottomSide = (ushort)bottomSide;
                CameraEditor.CameraTriggers[id][CameraEditor.cameraTriggerId].TopSide = (ushort)topSide;

                SNES.edit = true;
                MainWindow.window.enemyE.DrawEnemies();
            }
            else if (mouseState == MouseState.Pan)
                Mouse.OverrideCursor = null;
            mouseState = MouseState.None;
        }
        private void canvas_MouseLeave(object sender, MouseEventArgs e)
        {
            if (mouseState == MouseState.TriggerSelect)
                selectRect.Visibility = Visibility.Collapsed;
            else if (mouseState == MouseState.Pan)
                Mouse.OverrideCursor = null;
            mouseState = MouseState.None;
        }
        private void idInt_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (selectedEnemy == null || e.NewValue == null)
                return;
            SNES.edit = true;
            selectedEnemy.Id = (byte)(int)e.NewValue;
            DrawEnemies();
        }
        private void varInt_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (selectedEnemy == null || e.NewValue == null)
                return;
            SNES.edit = true;
            selectedEnemy.SubId = (byte)((int)e.NewValue & 0xFF);
        }
        private void typeInt_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (selectedEnemy == null || e.NewValue == null)
                return;
            SNES.edit = true;
            selectedEnemy.Type = (byte)((int)e.NewValue & 0xFF);
            DrawEnemies();
        }
        private void colInt_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (selectedEnemy == null || e.NewValue == null)
                return;
            SNES.edit = true;
            selectedEnemy.Column = (byte)((int)e.NewValue);
        }
        private void xInt_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (selectedEnemy == null || e.NewValue == null)
                return;
            SNES.edit = true;
            short posX = (short)(int)e.NewValue;
            Enemy en = selectedEnemy;
            en.X = posX;
            byte column = (byte)((posX / 32));
            en.Column = column;
            columnInt.Value = column;
            UpdateEnemyLabelPositions();
        }
        private void yInt_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (selectedEnemy == null || e.NewValue == null)
                return;
            SNES.edit = true;
            selectedEnemy.Y = (short)(int)e.NewValue;
            UpdateEnemyLabelPositions();
        }
        private void AddEnemy_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidEnemyAdd()) return;
            Enemy en = new Enemy();
            //TODO: should probably put in the centre witch would require taking scale into consideration
            en.X = (short)(viewerX + 0x100);
            en.Y = (short)(viewerY + 0x100);
            en.Id = 0xB; //Default is Heart Tank since it is the same Id across all games
            en.Type = 0;
            en.Column = (byte)(en.X / 32);
            Level.Enemies[Level.Id].Add(en);
            SNES.edit = true;
            SelectEnemy(en);
            DrawEnemies();
        }
        private void RemoveEnemy_Click(object sender, RoutedEventArgs e)
        {
            if (selectedEnemy == null)
                return;
            SNES.edit = true;
            Level.Enemies[Level.Id].Remove(selectedEnemy);
            DisableSelect();
            DrawEnemies();
        }
        private void ToolsBtn_Click(object sender, RoutedEventArgs e)
        {
            Window window = new Window() { Title = "Tools" , SizeToContent = SizeToContent.WidthAndHeight , ResizeMode = ResizeMode.NoResize, WindowStartupLocation = WindowStartupLocation.CenterScreen};

            Button deleteAllBtn = new Button() { Content = "Delete All" };
            deleteAllBtn.Click += (s, e) =>
            {
                if (Level.Id >= Const.PlayableLevelsCount || (Const.Id == Const.GameId.MegaManX3 && Level.Id > 0xE))
                {
                    MessageBox.Show("Enemies cannot be added to this level.", "Error");
                    return;
                }
                Level.Enemies[Level.Id].Clear();
                SNES.edit = true;
                DisableSelect();
                DrawEnemies();
                MessageBox.Show("All enemies have been deleted!");
                return;
            };

            StackPanel stackPanel = new StackPanel();
            stackPanel.Children.Add(deleteAllBtn);
            window.Content = stackPanel;
            window.ShowDialog();
        }
        private void zoomInBtn_Click(object sender, RoutedEventArgs e)
        {
            scale = Math.Clamp(scale + 1, 1, Const.MaxScaleUI);
            MainWindow.window.enemyE.ZoomTransform.ScaleX = scale;
            MainWindow.window.enemyE.ZoomTransform.ScaleY = scale;
        }
        private void zoomOutBtn_Click(object sender, RoutedEventArgs e)
        {
            scale = Math.Clamp(scale - 1, 1, Const.MaxScaleUI);
            MainWindow.window.enemyE.ZoomTransform.ScaleX = scale;
            MainWindow.window.enemyE.ZoomTransform.ScaleY = scale;
        }
        private void Help_Click(object sender, RoutedEventArgs e)
        {
            HelpWindow h = new HelpWindow(3);
            h.ShowDialog();
        }
        #endregion Events
    }
}
