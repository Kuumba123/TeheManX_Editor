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
        public EnemyEditor()
        {
            InitializeComponent();

            layoutImage.Source = layoutBMP;
        }
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

        }
        private void UpdateEnemyCordLabel(int x, int y)
        {

        }
        private void Label_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            MainWindow.window.enemyE.control.Tag = sender;
            byte type = ((Enemy)((EnemyLabel)control.Tag).Tag).Type;
            byte id = ((Enemy)((EnemyLabel)control.Tag).Tag).Id;
            byte var = ((Enemy)((EnemyLabel)control.Tag).Tag).SubId;

            if (e.ChangedButton == MouseButton.Left)
            {
                SNES.edit = true;

                if (!down)
                {

                    //Set Select Enemy
                   /*MainWindow.window.enemyE.rangeInt.Value = range;
                    MainWindow.window.enemyE.idInt.Value = id;
                    MainWindow.window.enemyE.varInt.Value = var;
                    MainWindow.window.enemyE.typeInt.Value = type;*/
                    //Enable
                   /*MainWindow.window.enemyE.idInt.IsEnabled = true;
                    MainWindow.window.enemyE.varInt.IsEnabled = true;
                    MainWindow.window.enemyE.typeInt.IsEnabled = true;
                    MainWindow.window.enemyE.xInt.IsEnabled = true;
                    MainWindow.window.enemyE.yInt.IsEnabled = true;
                    MainWindow.window.enemyE.rangeInt.IsEnabled = true;*/

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
            if (obj == null)
                return;


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


            Canvas.SetLeft(obj, x);
            Canvas.SetTop(obj, y);
            UpdateEnemyCordLabel((int)x, (int)y);
            var en = (Enemy)((EnemyLabel)obj).Tag;
            en.X = (short)((short)(viewerX + x) + Const.EnemyOffset);
            en.Y = (short)((short)(viewerY + y) + Const.EnemyOffset);

            SNES.edit = true;
        }
        private void canvas_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            obj = null;
            canvas.ReleaseMouseCapture();
        }
    }
}
