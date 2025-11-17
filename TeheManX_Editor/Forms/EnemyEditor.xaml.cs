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
    /// Interaction logic for EnemyEditor.xaml
    /// </summary>
    public partial class EnemyEditor : UserControl
    {
        #region Fields
        static List<EnemyLabel> enemyLabels = new List<EnemyLabel>();
        #endregion Fields

        #region Properties
        internal WriteableBitmap layoutBMP = new WriteableBitmap(768, 512, 96, 96, PixelFormats.Rgb24, null);
        public int viewerX = 0x400;
        public int viewerY = 0;
        UIElement obj;
        public FrameworkElement control = new FrameworkElement();
        bool down = false;
        Point point;
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
            layoutBMP.Lock();
            for (int y = 0; y < 2; y++)
            {
                for (int x = 0; x < 3; x++)
                {
                    Level.DrawScreen(Level.Layout[Level.Id, Level.BG, ((viewerY >> 8) + y) * 32 + ((viewerX >> 8) + x)], x * 256, y * 256, layoutBMP.BackBufferStride, layoutBMP.BackBuffer);
                }
            }
            layoutBMP.AddDirtyRect(new Int32Rect(0, 0, 768, 512));
            layoutBMP.Unlock();
        }
        public void DrawEnemies()
        {
            foreach (var r in enemyLabels)
                r.Visibility = Visibility.Collapsed;

            if (Level.Id >= Const.PlayabledLevelsCount)
                return;

            while (enemyLabels.Count < Level.Enemies[Level.Id].Count)
            {
                var r = new EnemyLabel();
                r.PreviewMouseDown += Label_PreviewMouseDown;
                enemyLabels.Add(r);
                MainWindow.window.enemyE.canvas.Children.Add(r);
            }

            for (int i = 0; i < Level.Enemies[Level.Id].Count; i++) //Update Each Enemy
            {
                enemyLabels[i].Tag = Level.Enemies[Level.Id][i];
                enemyLabels[i].text.Content = Level.Enemies[Level.Id][i].Id.ToString("X");
                enemyLabels[i].AssignTypeBorder(Level.Enemies[Level.Id][i].Type);
                Canvas.SetLeft(enemyLabels[i], Level.Enemies[Level.Id][i].X - viewerX - Const.EnemyOffset);
                Canvas.SetTop(enemyLabels[i], Level.Enemies[Level.Id][i].Y - viewerY - Const.EnemyOffset);
                enemyLabels[i].Visibility = Visibility.Visible;
            }
        }
        private void DisableSelect() //Disable editing Enemy Properties
        {
            MainWindow.window.enemyE.control.Tag = null;
            //Disable
            MainWindow.window.enemyE.idInt.IsEnabled = false;
            MainWindow.window.enemyE.varInt.IsEnabled = false;
            MainWindow.window.enemyE.typeInt.IsEnabled = false;
            MainWindow.window.enemyE.xInt.IsEnabled = false;
            MainWindow.window.enemyE.yInt.IsEnabled = false;
            MainWindow.window.enemyE.columnInt.IsEnabled = false;
            //MainWindow.window.enemyE.nameLbl.Content = "";
        }
        private void UpdateEnemyCordLabel(int x, int y, byte col)
        {
            MainWindow.window.enemyE.xInt.Value = x + viewerX + Const.EnemyOffset;
            MainWindow.window.enemyE.yInt.Value = y + viewerY + Const.EnemyOffset;
            MainWindow.window.enemyE.columnInt.Value = col;
        }
        private bool ValidEnemyAdd()
        {

           if (Level.Id >= Const.PlayabledLevelsCount)
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
        private void Label_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            MainWindow.window.enemyE.control.Tag = sender;
            byte type = ((Enemy)((EnemyLabel)control.Tag).Tag).Type;
            byte id = ((Enemy)((EnemyLabel)control.Tag).Tag).Id;
            byte var = ((Enemy)((EnemyLabel)control.Tag).Tag).SubId;
            byte colmn = ((Enemy)((EnemyLabel)control.Tag).Tag).Column;

            if (e.ChangedButton == MouseButton.Left)
            {
                SNES.edit = true;

                if (!down)
                {

                    //Set Select Enemy
                    MainWindow.window.enemyE.columnInt.Value = colmn;
                    MainWindow.window.enemyE.idInt.Value = id;
                    MainWindow.window.enemyE.varInt.Value = var;
                    MainWindow.window.enemyE.typeInt.Value = type;
                    //Enable
                    MainWindow.window.enemyE.idInt.IsEnabled = true;
                    MainWindow.window.enemyE.varInt.IsEnabled = true;
                    MainWindow.window.enemyE.typeInt.IsEnabled = true;
                    MainWindow.window.enemyE.xInt.IsEnabled = true;
                    MainWindow.window.enemyE.yInt.IsEnabled = true;
                    MainWindow.window.enemyE.columnInt.IsEnabled = true;

                    //UpdateEnemyName(type, id);
                }
                down = true;
                obj = sender as UIElement;
                point = e.GetPosition(MainWindow.window.enemyE.canvas);
                point.X -= Canvas.GetLeft(obj);
                point.Y -= Canvas.GetTop(obj);
                MainWindow.window.enemyE.canvas.CaptureMouse();
            }
            else
            {
                //TODO: show pop up message box with info about the enemy
            }
        }
        private void canvas_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (!down) return;


            //Move Enemy
            if (obj == null) return;
            var pos = e.GetPosition(sender as IInputElement);
            double x = pos.X - point.X;
            double y = pos.Y - point.Y;

            //Border Checks
            if (x < 0 - Const.EnemyOffset)
                x = 0 - Const.EnemyOffset;
            if (y < 0 - Const.EnemyOffset)
                y = 0 - Const.EnemyOffset;

            Enemy en = (Enemy)((EnemyLabel)obj).Tag;
            en.Column = (byte)(short)(((viewerX + x) / 32));

            Canvas.SetLeft(obj, x);
            Canvas.SetTop(obj, y);
            UpdateEnemyCordLabel((int)x, (int)y, en.Column);
            en.X = (short)((short)(viewerX + x) + Const.EnemyOffset);
            en.Y = (short)((short)(viewerY + y) + Const.EnemyOffset);

            SNES.edit = true;
        }
        private void canvas_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            obj = null;
            down = false;
            canvas.ReleaseMouseCapture();
        }
        private void idInt_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (control.Tag == null || e.NewValue == null || e.OldValue == null)
                return;
            SNES.edit = true;
            ((Enemy)((EnemyLabel)control.Tag).Tag).Id = (byte)(int)e.NewValue;
            ((EnemyLabel)control.Tag).text.Content = Convert.ToString(((Enemy)((EnemyLabel)control.Tag).Tag).Id, 16).ToUpper();
            //UpdateEnemyName(((Enemy)((EnemyLabel)control.Tag).Tag).type, ((Enemy)((EnemyLabel)control.Tag).Tag).id);
        }
        private void varInt_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (control.Tag == null || e.NewValue == null || e.OldValue == null)
                return;
            SNES.edit = true;
            ((Enemy)((EnemyLabel)control.Tag).Tag).SubId = (byte)((int)e.NewValue & 0xFF);
        }
        private void typeInt_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (control.Tag == null || e.NewValue == null || e.OldValue == null)
                return;
            SNES.edit = true;
            ((Enemy)((EnemyLabel)control.Tag).Tag).Type = (byte)((int)e.NewValue & 0xFF);
            ((EnemyLabel)control.Tag).AssignTypeBorder(((Enemy)((EnemyLabel)control.Tag).Tag).Type);
            //UpdateEnemyName(((Enemy)((EnemyLabel)control.Tag).Tag).type, ((Enemy)((EnemyLabel)control.Tag).Tag).id);
        }
        private void colInt_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (control.Tag == null || e.NewValue == null || e.OldValue == null)
                return;
            SNES.edit = true;
            ((Enemy)((EnemyLabel)control.Tag).Tag).Column = (byte)((int)e.NewValue / 32);
        }
        private void xInt_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (control.Tag == null || e.NewValue == null || e.OldValue == null)
                return;
            SNES.edit = true;
            ((Enemy)((EnemyLabel)control.Tag).Tag).X = (short)(int)e.NewValue;
            Canvas.SetLeft((UIElement)control.Tag, ((int)e.NewValue) - viewerX - Const.EnemyOffset);
        }
        private void yInt_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (control.Tag == null || e.NewValue == null || e.OldValue == null)
                return;
            SNES.edit = true;
            ((Enemy)((EnemyLabel)control.Tag).Tag).Y = (short)(int)e.NewValue;
            Canvas.SetTop((UIElement)control.Tag, ((int)e.NewValue) - viewerY - Const.EnemyOffset);
        }
        private void AddEnemy_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidEnemyAdd()) return;
            var en = new Enemy();
            en.X = (short)(viewerX + 0x100);
            en.Y = (short)(viewerY + 0x100);
            en.Id = 0xF; //Default is Met
            en.Type = 0;
            Level.Enemies[Level.Id].Add(en);
            SNES.edit = true;
            DrawEnemies();
        }
        private void RemoveEnemy_Click(object sender, RoutedEventArgs e)
        {
            if (control.Tag == null)
                return;
            SNES.edit = true;
            Level.Enemies[Level.Id].Remove((Enemy)((EnemyLabel)control.Tag).Tag);
            DrawEnemies();
        }
        private void ToolsBtn_Click(object sender, RoutedEventArgs e)
        {
            /*if (PSX.levels[Level.Id].GetIndex() > 25)
            {
                MessageBox.Show("There are not suppoused to be enemies in this level");
                return;
            }
            ListWindow l = new ListWindow(3);
            l.ShowDialog();*/
        }
        private void Help_Click(object sender, RoutedEventArgs e)
        {
            /*HelpWindow h = new HelpWindow(3);
            h.ShowDialog();*/
        }
        #endregion Events
    }
}
