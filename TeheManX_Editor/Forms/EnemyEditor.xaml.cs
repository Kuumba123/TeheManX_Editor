using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Text;
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
        #region Fields
        static List<Rectangle> triggerRects = new List<Rectangle>();
        static List<EnemyLabel> enemyLabels = new List<EnemyLabel>();
        public static double scale = 1;
        #endregion Fields

        #region Properties
        internal WriteableBitmap layoutBMP = new WriteableBitmap(768, 512, 96, 96, PixelFormats.Bgra32, null);
        public int viewerX = 0x400;
        public int viewerY = 0;
        UIElement obj;
        public FrameworkElement control = new FrameworkElement();
        bool down = false;
        Point point;
        //For Moving Camera Via Mouse
        private bool isPanning = false;
        private Point panStartMouse;
        private int panStartViewerX;
        private int panStartViewerY;
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
                for (int i = 0; i < (768 * 512); i++)
                {
                    *ptr = 0xFF000000;
                    ptr++;
                }
            }

            for (int sy = 0; sy < 3; sy++)
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

            layoutBMP.AddDirtyRect(new Int32Rect(0, 0, 768, 512));
            layoutBMP.Unlock();
        }
        public void DrawEnemies()
        {
            DisableSelect();

            for (int i = 0; i < enemyLabels.Count; i++)
                enemyLabels[i].Visibility = Visibility.Collapsed;

            for (int i = 0; i < triggerRects.Count; i++)
                triggerRects[i].Visibility = Visibility.Collapsed;

            if (Level.Id >= Const.PlayableLevelsCount || (Const.Id == Const.GameId.MegaManX3 && Level.Id > 0xE))
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
                enemyLabels[i].text.Text = Level.Enemies[Level.Id][i].Id.ToString("X");
                enemyLabels[i].AssignTypeBorder(Level.Enemies[Level.Id][i].Type);
                Canvas.SetLeft(enemyLabels[i], Level.Enemies[Level.Id][i].X - viewerX);
                Canvas.SetTop(enemyLabels[i], Level.Enemies[Level.Id][i].Y - viewerY);
                enemyLabels[i].Visibility = Visibility.Visible;
            }

            while (triggerRects.Count < (MainWindow.window.camE.triggerInt.Maximum + 1)) //Add to Canvas
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

            int listOffset = BitConverter.ToUInt16(SNES.rom, Const.CameraTriggersOffset + Level.Id * 2);

            for (int i = 0; i < (MainWindow.window.camE.triggerInt.Maximum + 1); i++)
            {
                int offset = SNES.CpuToOffset(BitConverter.ToUInt16(SNES.rom, SNES.CpuToOffset(listOffset + i * 2, Const.CameraSettingsBank)), Const.CameraSettingsBank);

                int rightSide = BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(offset));
                int leftSide = BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(offset + 2));
                int bottomSide = BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(offset + 4));
                int topSide = BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(offset + 6));

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
        public void UpdateEnemyLabelPositions()
        {
            for (int i = 0; i < enemyLabels.Count; i++)
            {
                EnemyLabel lbl = enemyLabels[i];

                if (lbl.Visibility != Visibility.Visible)
                    continue;

                Enemy en = (Enemy)lbl.Tag;

                double x = en.X - viewerX;
                double y = en.Y - viewerY;

                Canvas.SetLeft(lbl, x);
                Canvas.SetTop(lbl, y);
            }

            if (!MainWindow.window.camE.triggersEnabled)
                return;

            int listOffset = BitConverter.ToUInt16(SNES.rom, Const.CameraTriggersOffset + Level.Id * 2);

            for (int i = 0; i < (MainWindow.window.camE.triggerInt.Maximum + 1); i++)
            {
                int offset = SNES.CpuToOffset(BitConverter.ToUInt16(SNES.rom, SNES.CpuToOffset(listOffset + i * 2, Const.CameraSettingsBank)), Const.CameraSettingsBank);

                int rightSide = BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(offset));
                int leftSide = BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(offset + 2));
                int bottomSide = BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(offset + 4));
                int topSide = BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(offset + 6));

                int width = rightSide - leftSide;
                int height = bottomSide - topSide;

                if (width < 1) width = 1;
                if (height < 1) height = 1;

                triggerRects[i].Width = width;
                triggerRects[i].Height = height;

                Canvas.SetLeft(triggerRects[i], leftSide - viewerX);
                Canvas.SetTop(triggerRects[i], topSide - viewerY);
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
        private void UpdateEnemyCordLabel(short x, short y, byte col)
        {
            MainWindow.window.enemyE.xInt.Value = x + viewerX;
            MainWindow.window.enemyE.yInt.Value = y + viewerY;
            MainWindow.window.enemyE.columnInt.Value = col;
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
        private void Label_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            MainWindow.window.enemyE.control.Tag = sender;
            byte type = ((Enemy)((EnemyLabel)control.Tag).Tag).Type;
            byte id = ((Enemy)((EnemyLabel)control.Tag).Tag).Id;
            byte var = ((Enemy)((EnemyLabel)control.Tag).Tag).SubId;
            byte colmn = ((Enemy)((EnemyLabel)control.Tag).Tag).Column;

            if (e.ChangedButton == MouseButton.Left)
            {

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
        private void canvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if ((bool)MainWindow.window.camE.triggerCheck.IsChecked && MainWindow.window.camE.triggersEnabled)
            {
                point = e.GetPosition(MainWindow.window.enemyE.canvas);
                down = true;
                MainWindow.window.enemyE.canvas.CaptureMouse();
            }
        }
        private void canvas_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (!down) return;

            if ((bool)MainWindow.window.camE.triggerCheck.IsChecked && MainWindow.window.camE.triggersEnabled) //Select Trigger Size
            {
                Point mousePos = e.GetPosition(canvas);
                if (point.X < mousePos.X)
                {
                    Canvas.SetLeft(selectRect, point.X);
                    selectRect.Width = mousePos.X - point.X;
                }
                else
                {
                    Canvas.SetLeft(selectRect, mousePos.X);
                    selectRect.Width = point.X - mousePos.X;
                }

                if (point.Y < mousePos.Y)
                {
                    Canvas.SetTop(selectRect, point.Y);
                    selectRect.Height = mousePos.Y - point.Y;
                }
                else
                {
                    Canvas.SetTop(selectRect, mousePos.Y);
                    selectRect.Height = point.Y - mousePos.Y;
                }
                selectRect.Visibility = Visibility.Visible;
                return;
            }

            //Move Enemy
            if (obj == null) return;

            var pos = e.GetPosition(sender as IInputElement);

            double x = pos.X - point.X;
            double y = pos.Y - point.Y;

            // Border checks
            if (x < 0) x = 0;
            if (y < 0) y = 0;

            Enemy en = (Enemy)((EnemyLabel)obj).Tag;

            short pastLocationX = en.X;
            short pastLocationY = en.Y;

            // Calculate world-based position
            short locationX = (short)(viewerX + x);
            short locationY = (short)(viewerY + y);

            // Snap to 16-pixel grid when holding SHIFT
            if (Keyboard.IsKeyDown(Key.LeftShift))
            {
                locationX = (short)(locationX & 0xFFF0); // snap X to multiple of 16
                locationY = (short)(locationY & 0xFFF0); // snap Y to multiple of 16

                // IMPORTANT:
                // Also snap the visible label position so it moves visibly by 16px
                x = locationX - viewerX;
                y = locationY - viewerY;
            }

            // No change no work
            if (pastLocationX == locationX && pastLocationY == locationY)
                return;

            en.Column = (byte)(locationX / 32);

            en.X = locationX;
            en.Y = locationY;

            UpdateEnemyCordLabel(locationX, locationY, en.Column);

            // Move the label on the canvas
            Canvas.SetLeft(obj, x);
            Canvas.SetTop(obj, y);

            SNES.edit = true;
        }
        private void canvas_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Right)
                return;
            down = false;
            if ((bool)MainWindow.window.camE.triggerCheck.IsChecked)
            {
                if (Level.Id >= Const.PlayableLevelsCount || (Const.Id == Const.GameId.MegaManX3 && Level.Id > 0xE))
                    goto End;

                selectRect.Visibility = Visibility.Collapsed;
                int index = Level.Id;

                int leftSide = ((int)(Canvas.GetLeft(selectRect) + viewerX));
                int rightSide = ((int)(Canvas.GetLeft(selectRect) + selectRect.Width + viewerX));
                int topSide = (int)(Canvas.GetTop(selectRect) + viewerY);
                int bottomSide = (int)(Canvas.GetTop(selectRect) + selectRect.Height + viewerY);

                if (leftSide < 0)
                    leftSide = 0;
                if (rightSide > 0xFFFF)
                    rightSide = 0xFFFF;
                if (topSide < 0)
                    topSide = 0;
                if (bottomSide > 0xFFFF)
                    bottomSide = 0xFFFF;

                int listOffset = BitConverter.ToUInt16(SNES.rom, Const.CameraTriggersOffset + Level.Id * 2);
                int offset = SNES.CpuToOffset(BitConverter.ToUInt16(SNES.rom, SNES.CpuToOffset(listOffset + (int)MainWindow.window.camE.triggerInt.Value * 2, Const.CameraSettingsBank)), Const.CameraSettingsBank);

                BinaryPrimitives.WriteUInt16LittleEndian(SNES.rom.AsSpan(offset), (ushort)rightSide);
                MainWindow.window.camE.rightInt.Value = rightSide;

                BinaryPrimitives.WriteUInt16LittleEndian(SNES.rom.AsSpan(offset + 2), (ushort)leftSide);
                MainWindow.window.camE.leftInt.Value = leftSide;

                BinaryPrimitives.WriteUInt16LittleEndian(SNES.rom.AsSpan(offset + 4), (ushort)bottomSide);
                MainWindow.window.camE.bottomInt.Value = bottomSide;

                BinaryPrimitives.WriteUInt16LittleEndian(SNES.rom.AsSpan(offset + 6), (ushort)topSide);
                MainWindow.window.camE.topInt.Value = topSide;

                SNES.edit = true;
                MainWindow.window.enemyE.UpdateEnemyLabelPositions();
            }
            else
                obj = null;
        End:
            canvas.ReleaseMouseCapture();
        }
        private void LayoutImage_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            // Ignore if not middle mouse button
            if (e.ChangedButton != MouseButton.Middle)
                return;

            isPanning = true;
            panStartMouse = e.GetPosition(canvas);
            panStartViewerX = viewerX;
            panStartViewerY = viewerY;

            Mouse.OverrideCursor = Cursors.ScrollAll;
        }
        private void LayoutImage_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            // Must be actively panning AND middle button must still be pressed
            if (!isPanning)
                return;
            Point pos = e.GetPosition(canvas);

            double dx = pos.X - panStartMouse.X;
            double dy = pos.Y - panStartMouse.Y;

            viewerX = panStartViewerX - (int)dx;
            viewerY = panStartViewerY - (int)dy;

            if (viewerX < 0) viewerX = 0;
            if (viewerY < 0) viewerY = 0;

            MainWindow.window.UpdateEnemyViewerCam();
            DrawLayout();
            UpdateEnemyLabelPositions();
        }
        private void LayoutImage_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            isPanning = false;
            Mouse.OverrideCursor = null;
        }
        private void LayoutImage_MouseLeave(object sender, MouseEventArgs e)
        {
            if (isPanning)
            {
                isPanning = false;
                Mouse.OverrideCursor = null;
            }
        }
        private void idInt_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (control.Tag == null || e.NewValue == null || e.OldValue == null)
                return;
            SNES.edit = true;
            ((Enemy)((EnemyLabel)control.Tag).Tag).Id = (byte)(int)e.NewValue;
            ((EnemyLabel)control.Tag).text.Text = ((Enemy)((EnemyLabel)control.Tag).Tag).Id.ToString("X");
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
            ((Enemy)((EnemyLabel)control.Tag).Tag).Column = (byte)((int)e.NewValue);
        }
        private void xInt_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (control.Tag == null || e.NewValue == null || e.OldValue == null)
                return;
            SNES.edit = true;
            short posX = (short)(int)e.NewValue;
            Enemy en = ((Enemy)((EnemyLabel)control.Tag).Tag);
            en.X = posX;
            en.Column = (byte)((posX / 32));
            MainWindow.window.enemyE.columnInt.Value = (int)en.Column;
            Canvas.SetLeft((UIElement)control.Tag, ((int)e.NewValue) - viewerX);
        }
        private void yInt_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (control.Tag == null || e.NewValue == null || e.OldValue == null)
                return;
            SNES.edit = true;
            ((Enemy)((EnemyLabel)control.Tag).Tag).Y = (short)(int)e.NewValue;
            Canvas.SetTop((UIElement)control.Tag, ((int)e.NewValue) - viewerY);
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
            Window window = new Window() { Title = "Tools" , SizeToContent = SizeToContent.WidthAndHeight , ResizeMode = ResizeMode.NoResize, WindowStartupLocation = WindowStartupLocation.CenterScreen};

            Button expandBtn = new Button() { Content = "Expand Patch"};
            expandBtn.Click += (s, e) =>
            {
                if (SNES.rom.Length >= 0x400000 && Encoding.ASCII.GetString(SNES.rom, 0x3FFFF0, 6) == "POGYOU")
                {
                    MessageBox.Show("You already have the expand patch.");
                    return;
                }
                if (MainWindow.window.screenE.mode16)
                {
                    MessageBox.Show("You must exit 16x16 mode if you want to enable the expansion patch!");
                    return;
                }

                SNES.rom[0x7FD7] = 0xC;
                Array.Resize(ref SNES.rom, 0x400000);
                Array.Copy(Encoding.ASCII.GetBytes("POGYOU"), 0, SNES.rom, 0x3FFFF0, 6);

                int dumpOffset;
                int addrMask = 0;
                int bankCount;
                if (Const.Id == Const.GameId.MegaManX)
                {
                    dumpOffset = Const.MegaManX.BankCount * 0x8000;
                    addrMask = 0x800000;
                    bankCount = Const.MegaManX.BankCount;
                }
                else if (Const.Id == Const.GameId.MegaManX2)
                {
                    dumpOffset = Const.MegaManX2.BankCount * 0x8000;
                    bankCount = Const.MegaManX2.BankCount;
                }
                else
                {
                    dumpOffset = Const.MegaManX3.BankCount * 0x8000;
                    bankCount = Const.MegaManX3.BankCount;
                }

                int dumpAddr = 0;

                {
                    int pointerBase = (SNES.OffsetToCpu(Const.EnemyPointersOffset) & 0x7FFF) + bankCount * 0x8000;
                    int startWrite;

                    //for X1 actual enemy data should be at end of bank other games should have it at start
                    if (Const.Id == Const.GameId.MegaManX)
                        startWrite = pointerBase;
                    else
                        startWrite = bankCount * 0x8000;

                    //Assign 16-bit Enemy Data Pointers
                    for (int i = 0; i < Const.PlayableLevelsCount; i++)
                    {
                        if (Const.Id == Const.GameId.MegaManX3 && i > 0xE) break;

                        int writeOffset = pointerBase + i * 2;

                        if (Const.Id == Const.GameId.MegaManX3 && i >= 0xC)
                            dumpAddr = pointerBase + (Const.MegaManX3.LevelsCount - 2) * 2 + (i - 0xC) * 0xCC + 1 * (i - 0xC);
                        else
                            dumpAddr = SNES.OffsetToCpu(i * 0xCC * 8 + 1 * i + startWrite + Const.PlayableLevelsCount * 2);

                        BinaryPrimitives.WriteUInt16LittleEndian(SNES.rom.AsSpan(writeOffset), (ushort)(dumpAddr & 0xFFFF));
                    }
                    //Update Enemy Pointer
                    Const.EnemyPointersOffset = (SNES.OffsetToCpu(Const.EnemyPointersOffset) & 0x7FFF) + bankCount * 0x8000;
                }

                //Write Enemy Data Banks
                byte oraBank = (byte)(bankCount | (addrMask >> 16));
                SNES.rom[Const.EnemyBankAsmOffsets[0]] = oraBank;
                SNES.rom[Const.EnemyBankAsmOffsets[1]] = oraBank;
                SNES.rom[Const.EnemyBankAsmOffsets[2]] = oraBank;


                //Set Dump Offset for Start of 16x16 Tile Data & Layout Data
                if (Const.Id == Const.GameId.MegaManX3)
                    dumpOffset = Const.MegaManX3.FreeBanks[0] * 0x8000;
                else
                    dumpOffset += 0x8000 - (dumpOffset % 0x8000); //Increament Dump Offset to Next Bank Unconditionally

                //Dump 16x16 Tile Data and the Layout Data 1st!

                List<byte[]> tileData16 = new List<byte[]>();
                List<byte[]> screenData = new List<byte[]>();
                List<byte[]> screenData2 = new List<byte[]>();
                List<byte[]> tile32Data = new List<byte[]>();
                List<byte[]> tile32Data2 = new List<byte[]>();
                List<byte[]> tileCollision = new List<byte[]>();

                for (int i = 0; i < Const.PlayableLevelsCount; i++)
                {
                    tileData16.Add(new byte[Const.Tile16Count[i, 0] * 8]);
                    screenData.Add(new byte[Const.ScreenCount[i, 0] * 0x80]);
                    screenData2.Add(new byte[Const.ScreenCount[i, 1] * 0x80]);
                    tile32Data.Add(new byte[Const.Tile32Count[i, 0] * 8]);
                    tile32Data2.Add(new byte[Const.Tile32Count[i, 1] * 8]);
                    tileCollision.Add(new byte[Const.Tile16Count[i, 0]]);

                    int id;
                    if (Const.Id == Const.GameId.MegaManX3 && i == 0xE) id = 0x10; //special case for MMX3 rekt version of dophler 2
                    else if (Const.Id == Const.GameId.MegaManX3 && i > 0xE) id = (i - 0xF) + 0xE; //Buffalo or Beetle
                    else id = i;

                    Array.Copy(SNES.rom, SNES.CpuToOffset(BitConverter.ToInt32(SNES.rom, Const.Tile16DataPointersOffset[0] + id * 3)), tileData16[i], 0, Const.Tile16Count[i, 0] * 8);
                    Array.Copy(SNES.rom, SNES.CpuToOffset(BitConverter.ToInt32(SNES.rom, Const.ScreenDataPointersOffset[0] + id * 3)), screenData[i], 0, Const.ScreenCount[i, 0] * 0x80);
                    Array.Copy(SNES.rom, SNES.CpuToOffset(BitConverter.ToInt32(SNES.rom, Const.ScreenDataPointersOffset[1] + id * 3)), screenData2[i], 0, Const.ScreenCount[i, 1] * 0x80);
                    Array.Copy(SNES.rom, SNES.CpuToOffset(BitConverter.ToInt32(SNES.rom, Const.Tile32DataPointersOffset[0] + id * 3)), tile32Data[i], 0, Const.Tile32Count[i, 0] * 8);
                    Array.Copy(SNES.rom, SNES.CpuToOffset(BitConverter.ToInt32(SNES.rom, Const.Tile32DataPointersOffset[1] + id * 3)), tile32Data2[i], 0, Const.Tile32Count[i, 1] * 8);
                    Array.Copy(SNES.rom, SNES.CpuToOffset(BitConverter.ToInt32(SNES.rom, Const.TileCollisionDataPointersOffset + id * 3)), tileCollision[i], 0, Const.Tile16Count[i, 0]);
                }


                for (int i = 0; i < Const.PlayableLevelsCount; i++)
                {
                    if (Const.Id == Const.GameId.MegaManX3 && i > 0xE) //Buffalo or Beetle
                    {
                        int dumpId = (i - 0xF) + 0xE;
                        int readId = (i - 0xF) + 2;

                        //Assign Buffalo/Beetle Alternative stage to use the same 16x16 Tile Data as the orignal
                        int pointer24 = BinaryPrimitives.ReadInt32LittleEndian(SNES.rom.AsSpan(Const.Tile16DataPointersOffset[0] + readId * 3));
                        BinaryPrimitives.WriteUInt16LittleEndian(SNES.rom.AsSpan(Const.Tile16DataPointersOffset[0] + dumpId * 3), (ushort)(pointer24 & 0xFFFF));
                        SNES.rom[Const.Tile16DataPointersOffset[0] + dumpId * 3 + 2] = (byte)(pointer24 >> 16);

                        pointer24 = BinaryPrimitives.ReadInt32LittleEndian(SNES.rom.AsSpan(Const.Tile16DataPointersOffset[1] + readId * 3));
                        BinaryPrimitives.WriteUInt16LittleEndian(SNES.rom.AsSpan(Const.Tile16DataPointersOffset[1] + dumpId * 3), (ushort)(pointer24 & 0xFFFF));
                        SNES.rom[Const.Tile16DataPointersOffset[1] + dumpId * 3 + 2] = (byte)(pointer24 >> 16);

                        dumpAddr = SNES.OffsetToCpu(dumpOffset);

                        BinaryPrimitives.WriteUInt16LittleEndian(SNES.rom.AsSpan(Const.LayoutPointersOffset[0] + dumpId * 3), (ushort)(dumpAddr & 0xFFFF));
                        SNES.rom[Const.LayoutPointersOffset[0] + dumpId * 3 + 2] = (byte)(dumpAddr >> 16);

                        dumpOffset += Const.ExpandLayoutLength;
                        dumpAddr += Const.ExpandLayoutLength;

                        BinaryPrimitives.WriteUInt16LittleEndian(SNES.rom.AsSpan(Const.LayoutPointersOffset[1] + dumpId * 3), (ushort)(dumpAddr & 0xFFFF));
                        SNES.rom[Const.LayoutPointersOffset[1] + dumpId * 3 + 2] = (byte)(dumpAddr >> 16);

                        dumpOffset += Const.ExpandLayoutLength;
                        dumpAddr += Const.ExpandLayoutLength;
                    }
                    else
                    {
                        int id = i;

                        //Dump Existing 16x16 Tile Data
                        Array.Copy(tileData16[i], 0, SNES.rom, dumpOffset, Const.Tile16Count[i, 0] * 8);
                        dumpAddr = SNES.OffsetToCpu(dumpOffset) | addrMask;

                        BinaryPrimitives.WriteUInt16LittleEndian(SNES.rom.AsSpan(Const.Tile16DataPointersOffset[0] + id * 3), (ushort)(dumpAddr & 0xFFFF));
                        SNES.rom[Const.Tile16DataPointersOffset[0] + id * 3 + 2] = (byte)((dumpAddr >> 16) & 0xFF);
                        BinaryPrimitives.WriteUInt16LittleEndian(SNES.rom.AsSpan(Const.Tile16DataPointersOffset[1] + id * 3), (ushort)(dumpAddr & 0xFFFF));
                        SNES.rom[Const.Tile16DataPointersOffset[1] + id * 3 + 2] = (byte)((dumpAddr >> 16) & 0xFF);

                        //Increament Dump Offset
                        dumpOffset += Const.ExpandMaxTiles16 * 8;

                        //Assign Layouts the New Pointers (Layer 1)
                        dumpAddr = SNES.OffsetToCpu(dumpOffset) | addrMask;
                        BinaryPrimitives.WriteUInt16LittleEndian(SNES.rom.AsSpan(Const.LayoutPointersOffset[0] + id * 3), (ushort)(dumpAddr & 0xFFFF));
                        SNES.rom[Const.LayoutPointersOffset[0] + id * 3 + 2] = (byte)((dumpAddr >> 16) & 0xFF);

                        //Increament Dump Offset
                        dumpOffset += Const.ExpandLayoutLength;

                        //Layer 2
                        dumpAddr = SNES.OffsetToCpu(dumpOffset) | addrMask;
                        BinaryPrimitives.WriteUInt16LittleEndian(SNES.rom.AsSpan(Const.LayoutPointersOffset[1] + id * 3), (ushort)(dumpAddr & 0xFFFF));
                        SNES.rom[Const.LayoutPointersOffset[1] + id * 3 + 2] = (byte)((dumpAddr >> 16) & 0xFF);

                        //Increament Dump Offset
                        dumpOffset += Const.ExpandLayoutLength;

                        if (Const.Id == Const.GameId.MegaManX3)
                        {
                            if ((i & 1) != 0)
                                dumpOffset = Const.MegaManX3.FreeBanks[(i + 1) / 2] * 0x8000;
                        }
                        else
                        {
                            //Increament Dump Offset to Next Bank
                            if ((i & 1) != 0 && (dumpOffset % 0x8000) != 0)
                                dumpOffset += 0x8000 - (dumpOffset % 0x8000);
                        }
                    }
                }
                /*End of Loop*/
            

                //Then Dump the rest of the Data
                if (Const.Id == Const.GameId.MegaManX3)
                    dumpOffset = (Const.MegaManX3.BankCount + 1) * 0x8000;
                else // MegaMan X1 & X2
                {
                    //Just in case we are not at the start of a bank
                    if ((dumpOffset % 0x8000) != 0)
                        dumpOffset += 0x8000 - (dumpOffset % 0x8000);
                }

                for (int i = 0; i < Const.PlayableLevelsCount; i++)
                {
                    int id;
                    if (Const.Id == Const.GameId.MegaManX3 && i == 0xE) id = 0x10; //special case for MMX3 rekt version of dophler 2
                    else if (Const.Id == Const.GameId.MegaManX3 && i > 0xE) id = (i - 0xF) + 0xE; //Buffalo or Beetle
                    else id = i;

                    //Dump Existing Screen Data
                    Array.Copy(screenData[i] , 0, SNES.rom, dumpOffset, Const.ScreenCount[i, 0] * 0x80);
                    //Update Pointer
                    dumpAddr = SNES.OffsetToCpu(dumpOffset) | addrMask;
                    BinaryPrimitives.WriteUInt16LittleEndian(SNES.rom.AsSpan(Const.ScreenDataPointersOffset[0] + id * 3), (ushort)(dumpAddr & 0xFFFF));
                    SNES.rom[Const.ScreenDataPointersOffset[0] + id * 3 + 2] = (byte)((dumpAddr >> 16) & 0xFF);


                    //Increament Dump Offset
                    {
                        int maxScreens;
                        if (Const.Id == Const.GameId.MegaManX)
                            maxScreens = Const.ExpandMaxScreens[0];
                        else
                            maxScreens = Const.ExpandMaxScreens2[0];
                        dumpOffset += maxScreens * 0x80;
                    }

                    //Dump Existing Screen Data (Layer 2)
                    Array.Copy(screenData2[i], 0, SNES.rom, dumpOffset, Const.ScreenCount[i, 1] * 0x80);
                    //Update Pointer
                    dumpAddr = SNES.OffsetToCpu(dumpOffset) | addrMask;
                    BinaryPrimitives.WriteUInt16LittleEndian(SNES.rom.AsSpan(Const.ScreenDataPointersOffset[1] + id * 3), (ushort)(dumpAddr & 0xFFFF));
                    SNES.rom[Const.ScreenDataPointersOffset[1] + id * 3 + 2] = (byte)((dumpAddr >> 16) & 0xFF);


                    //Increament Dump Offset
                    dumpOffset += Const.ExpandMaxScreens[1] * 0x80;

                    //Dump Existing 32x32 Tile Data
                    Array.Copy(tile32Data[i], 0, SNES.rom, dumpOffset, Const.Tile32Count[i, 0] * 8);
                    //Update Pointer
                    dumpAddr = SNES.OffsetToCpu(dumpOffset) | addrMask;
                    BinaryPrimitives.WriteUInt16LittleEndian(SNES.rom.AsSpan(Const.Tile32DataPointersOffset[0] + id * 3), (ushort)(dumpAddr & 0xFFFF));
                    SNES.rom[Const.Tile32DataPointersOffset[0] + id * 3 + 2] = (byte)((dumpAddr >> 16) & 0xFF);

                    //Increament Dump Offset
                    dumpOffset += Const.ExpandMaxTiles32[0] * 8;

                    //Dump Existing 32x32 Tile Data (Layer 2)
                    Array.Copy(tile32Data2[i], 0, SNES.rom, dumpOffset, Const.Tile32Count[i, 1] * 8);
                    dumpAddr = SNES.OffsetToCpu(dumpOffset) | addrMask;
                    BinaryPrimitives.WriteUInt16LittleEndian(SNES.rom.AsSpan(Const.Tile32DataPointersOffset[1] + id * 3), (ushort)(dumpAddr & 0xFFFF));
                    SNES.rom[Const.Tile32DataPointersOffset[1] + id * 3 + 2] = (byte)((dumpAddr >> 16) & 0xFF);

                    //Increament Dump Offset
                    dumpOffset += Const.ExpandMaxTiles32[1] * 8;

                    //Dump Existing 16x16 Tile Collision Data
                    Array.Copy(tileCollision[i], 0, SNES.rom, dumpOffset, Const.Tile16Count[i, 0]);
                    dumpAddr = SNES.OffsetToCpu(dumpOffset) | addrMask;
                    BinaryPrimitives.WriteUInt16LittleEndian(SNES.rom.AsSpan(Const.TileCollisionDataPointersOffset + id * 3), (ushort)(dumpAddr & 0xFFFF));
                    SNES.rom[Const.TileCollisionDataPointersOffset + id * 3 + 2] = (byte)((dumpAddr >> 16) & 0xFF);

                    //Increament Dump Offset
                    dumpOffset += Const.ExpandMaxTiles16;

                    //Increament Dump Offset to Next Bank
                    if ((dumpOffset % 0x8000) != 0)
                        dumpOffset += 0x8000 - (dumpOffset % 0x8000);
                }
                /*End of Loop*/

                //Done
                SNES.edit = true;
                SNES.expanded = true;
                Const.AssignExpand();
                MainWindow.window.layoutE.AssignLimits();
                MainWindow.window.screenE.AssignLimits();
                MainWindow.window.tile32E.AssignLimits();
                MainWindow.window.tile16E.AssignLimits();
                MessageBox.Show("Expansion Applied for Layout , Screen , 32x32 , 16x16 Enemy tabs!");
            };
            Button deleteAllBtn = new Button() { Content = "Delete All" };
            deleteAllBtn.Click += (s, e) =>
            {
                if (Level.Id >= Const.PlayableLevelsCount || (Const.Id == Const.GameId.MegaManX3 && Level.Id > 0xE))
                {
                    MessageBox.Show("Enemies cannot be added to this level.", "Error");
                    return;
                }
                Level.Enemies[Level.Id].Clear();
                DrawEnemies();
                MessageBox.Show("All enemies have been deleted!");
                return;
            };

            StackPanel stackPanel = new StackPanel();
            stackPanel.Children.Add(expandBtn);
            stackPanel.Children.Add(deleteAllBtn);
            window.Content = stackPanel;
            window.ShowDialog();
        }
        private void Help_Click(object sender, RoutedEventArgs e)
        {
            /*HelpWindow h = new HelpWindow(3);
            h.ShowDialog();*/
        }
        #endregion Events
    }
}
