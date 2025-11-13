using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace TeheManX_Editor.Forms
{
    /// <summary>
    /// Interaction logic for LayoutEditor.xaml
    /// </summary>
    public partial class LayoutEditor : UserControl
    {
        #region Properties
        WriteableBitmap layoutBMP = new WriteableBitmap(768, 768, 96, 96, PixelFormats.Rgb24, null);
        WriteableBitmap selectBMP = new WriteableBitmap(256, 256, 96, 96, PixelFormats.Rgb24, null);
        public int viewerX = 0;
        public int viewerY = 0;
        public int selectedScreen = 2;
        public List<Label> screenLabels = new List<Label>();
        public Button pastLayer;
        #endregion Properties
        
        #region Constructors
        public LayoutEditor()
        {
            InitializeComponent();

            layoutImage.Source = layoutBMP;
            selectImage.Source = selectBMP;
        }
        #endregion Constructors
        
        #region Methods
        public void DrawLayout()
        {
            layoutBMP.Lock();
            for (int y = 0; y < 3; y++)
            {
                for (int x = 0; x < 3; x++)
                {
                    Level.DrawScreen(Level.Layout[Level.Id, Level.BG, ((viewerY >> 8) + y) * 32 + ((viewerX >> 8) + x)], x * 256, y * 256, layoutBMP.BackBufferStride, layoutBMP.BackBuffer);
                }
            }
            layoutBMP.AddDirtyRect(new Int32Rect(0, 0, 768, 768));
            layoutBMP.Unlock();
        }
        public void DrawScreen()
        {
            selectBMP.Lock();
            Level.DrawScreen(selectedScreen, 768, selectBMP.BackBuffer);
            selectBMP.AddDirtyRect(new Int32Rect(0, 0, 256, 256));
            selectBMP.Unlock();
        }
        public void UpdateBtn()
        {
            if (pastLayer != null)
            {
                pastLayer.Background = Brushes.Black;
                pastLayer.Foreground = Brushes.White;
            }
            if (Level.BG == 0)
            {
                btn1.Background = Brushes.LightBlue;
                btn1.Foreground = Brushes.Black;
                pastLayer = btn1;
            }
            else
            {
                btn2.Background = Brushes.LightBlue;
                btn2.Foreground = Brushes.Black;
                pastLayer = btn2;
            }
        }
        public void AssignLimits()
        {
            int screenAmount = Const.ScreenCount[Level.Id, Level.BG] - 1;
            MainWindow.window.layoutE.screenInt.Maximum = screenAmount;
            if (MainWindow.window.layoutE.screenInt.Value > screenAmount)
                MainWindow.window.layoutE.screenInt.Value = screenAmount;

            DrawLayout();
            DrawScreen();
        }
        #endregion Methods

        #region Events
        private void layoutImage_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var p = e.GetPosition(layoutImage);
            int x = (int)p.X;
            int y = (int)p.Y;
            int cX = SNES.GetSelectedTile(x, layoutImage.ActualWidth, 3);
            int cY = SNES.GetSelectedTile(y, layoutImage.ActualHeight, 3);
            int offsetX = (MainWindow.window.layoutE.viewerX >> 8) + cX;
            int offsetY = (MainWindow.window.layoutE.viewerY >> 8) + cY;
            int i = (offsetY * 32) + offsetX;
            if (e.ChangedButton == MouseButton.Right)
            {
                if (Keyboard.IsKeyDown(Key.LeftShift))
                {
                    MainWindow.window.screenE.screenInt.Value = Level.Layout[Level.Id,Level.BG, i];
                    return;
                }
                selectedScreen = Level.Layout[Level.Id,Level.BG, i];
                MainWindow.window.layoutE.screenInt.Value = selectedScreen;
                DrawScreen();
            }
            else
            {
                Level.Layout[Level.Id,Level.BG,i] = (byte)(selectedScreen & 0xFF);
                SNES.edit = true;
                DrawLayout();
                MainWindow.window.enemyE.DrawLayout();
            }
        }
        private void IntegerUpDown_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue == null || SNES.rom == null)
                return;
            if (selectedScreen == (int)e.NewValue)
                return;
            selectedScreen = (int)e.NewValue;
            if ((uint)selectedScreen >= (Const.ScreenCount[Level.Id,Level.BG] - 1))
                selectedScreen = Const.ScreenCount[Level.Id, Level.BG] - 1;
            DrawScreen();
        }
        private void gridBtn_Click(object sender, RoutedEventArgs e)
        {
            if (layoutGrid.ShowGridLines)
                layoutGrid.ShowGridLines = false;
            else
                layoutGrid.ShowGridLines = true;
        }
        private void LayerButton_Click(object sender, RoutedEventArgs e)
        {
            var b = (Button)sender;
            int i = Convert.ToInt32(b.Content.ToString(), 16) - 1;
            if (Level.BG == i)
                return;
            Level.BG = i;
            if (pastLayer != null)
            {
                pastLayer.Background = Brushes.Black;
                pastLayer.Foreground = Brushes.White;
            }
            b.Background = Brushes.LightBlue;
            b.Foreground = Brushes.Black;
            pastLayer = b;
            MainWindow.window.layoutE.AssignLimits();
            MainWindow.window.screenE.AssignLimits();
            MainWindow.window.tile32E.AssignLimits();
            MainWindow.window.tile16E.AssignLimits();
            MainWindow.window.enemyE.DrawLayout();
            //if (MainWindow.layoutWindow != null)
            //    MainWindow.layoutWindow.DrawScreens();
        }
        private void ViewScreens_Click(object sender, RoutedEventArgs e)
        {
            /*if (ListWindow.screenViewOpen)
                return;
            MainWindow.layoutWindow = new ListWindow(0);
            MainWindow.layoutWindow.Show();*/
        }
        private void Help_Click(object sender, RoutedEventArgs e)
        {
            //HelpWindow h = new HelpWindow(1);
            //h.ShowDialog();
        }
        private void SnapButton_Click(object sender, RoutedEventArgs e)
        {
            using (var sfd = new System.Windows.Forms.SaveFileDialog())
            {
                sfd.Filter = "PNG |*.png";
                sfd.Title = "Select Level Layout Save Location";
                try
                {
                    if (sfd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    {
                        WriteableBitmap fileBmp = new WriteableBitmap(256 * 32, 256 * 32, 96, 96, PixelFormats.Rgb24, null);
                        int stride = fileBmp.BackBufferStride;
                        fileBmp.Lock();
                        IntPtr ptr = fileBmp.BackBuffer;
                        for (int y = 0; y < 32; y++) //32 Screens  Tall
                        {
                            for (int x = 0; x < 32; x++) //32 Screens Wide
                            {
                                byte screen = Level.Layout[Level.Id,Level.BG,(y * 32) + x];
                                Level.DrawScreen(screen, x * 256, y * 256, stride, ptr);
                            }
                        }
                        fileBmp.AddDirtyRect(new Int32Rect(0, 0, 256 * 32, 256 * 32));
                        fileBmp.Unlock();
                        PngBitmapEncoder encoder = new PngBitmapEncoder();
                        encoder.Frames.Add(BitmapFrame.Create(fileBmp));
                        System.IO.FileStream fs = System.IO.File.Create(sfd.FileName);
                        encoder.Save(fs);
                        fs.Close();
                        MessageBox.Show("Layout Exported");
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
        }
        private void UpButton_Click(object sender, RoutedEventArgs e)
        {
            if (MainWindow.window.layoutE.viewerY != 0)
            {
                MainWindow.window.layoutE.viewerY -= 0x100;
                MainWindow.window.layoutE.DrawLayout();
                MainWindow.window.UpdateViewrCam();
            }
        }
        private void DownButton_Click(object sender, RoutedEventArgs e)
        {
            if ((MainWindow.window.layoutE.viewerY >> 8) < (32 - 3))
            {
                MainWindow.window.layoutE.viewerY += 0x100;
                MainWindow.window.layoutE.DrawLayout();
                MainWindow.window.UpdateViewrCam();
            }
        }
        private void LeftButton_Click(object sender, RoutedEventArgs e)
        {
            if (MainWindow.window.layoutE.viewerX != 0)
            {
                MainWindow.window.layoutE.viewerX -= 0x100;
                MainWindow.window.layoutE.DrawLayout();
                MainWindow.window.UpdateViewrCam();
            }
        }
        private void RightButton_Click(object sender, RoutedEventArgs e)
        {
            if ((MainWindow.window.layoutE.viewerX >> 8) < (32 - 3))
            {
                MainWindow.window.layoutE.viewerX += 0x100;
                MainWindow.window.layoutE.DrawLayout();
                MainWindow.window.Update();
            }
        }
        #endregion Events
    }
}
