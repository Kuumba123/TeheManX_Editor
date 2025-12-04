using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Runtime.Intrinsics.Arm;
using System.Text;
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
        internal WriteableBitmap layoutBMP = new WriteableBitmap(768, 512, 96, 96, PixelFormats.Bgra32, null);
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

            if (Level.Id >= Const.PlayableLevelsCount)
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

                SNES.rom[0x7FD7] = 0xD;
                Array.Resize(ref SNES.rom, 0x400000);
                Array.Copy(Encoding.ASCII.GetBytes("POGYOU"), 0, SNES.rom, 0x3FFFF0, 6);

                int dumpOffset;
                int addrMask = 0;
                if (Const.Id == Const.GameId.MegaManX)
                {
                    dumpOffset = Const.MegaManX.BankCount * 0x8000 + 0x400; //need to add 0x400 cause of the 16-bit pointer address
                    addrMask = 0x800000;
                }
                else if (Const.Id == Const.GameId.MegaManX2)
                    dumpOffset = Const.MegaManX2.BankCount * 0x8000;
                else
                    dumpOffset = Const.MegaManX3.BankCount * 0x8000;

                int dumpAddr = 0;

                //Assign 16-bit Enemy Data Pointers
                for (int i = 0; i < Const.PlayableLevelsCount; i++)
                {
                    if (Const.Id == Const.GameId.MegaManX3 && i > 0xE) break;

                    int writeOffset = (SNES.OffsetToCpu(Const.EnemyPointersOffset) & 0xFFFF) - 0x8000 + dumpOffset + i * 2;
                    dumpAddr = SNES.OffsetToCpu(dumpOffset + i * 0xCC + 1);
                    BinaryPrimitives.WriteUInt16LittleEndian(SNES.rom.AsSpan(writeOffset), (ushort)(dumpAddr & 0xFFFF));
                }

                //Write Enemy Data Bank
                SNES.rom[Const.EnemyBankAsmOffset] = (byte)((SNES.CpuToOffset(dumpOffset) >> 16) | (addrMask >> 16));

                //Increament Dump Offset to Next Bank
                if ((dumpOffset % 0x8000) != 0)
                    dumpOffset += 0x8000 - (dumpOffset % 0x8000);

                //Dump Layout,Screen,32x32,16x16
                for (int i = 0; i < Const.PlayableLevelsCount; i++)
                {
                    int id;
                    if (Const.Id == Const.GameId.MegaManX3 && i == 0xE) id = 0x10; //special case for MMX3 rekt version of dophler 2
                    else if (Const.Id == Const.GameId.MegaManX3 && i > 0xE) id = (i - 0xF) + 0xE; //Buffalo or Beetle
                    else id = i;

                    //Dump Existing Screen Data
                    Array.Copy(SNES.rom, SNES.CpuToOffset(BitConverter.ToInt32(SNES.rom, Const.ScreenDataPointersOffset[0] + id * 3)), SNES.rom, dumpOffset, Const.ScreenCount[i, 0] * 0x80);
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

                        //Set Max Screens for Layer 1
                        Const.ScreenCount[i, 0] = maxScreens;
                    }
                    //Set Max Screens for Layer 2
                    Const.ScreenCount[i, 1] = Const.ExpandMaxScreens[1];

                    //Dump Existing Screen Data (Layer 2)
                    Array.Copy(SNES.rom, SNES.CpuToOffset(BitConverter.ToInt32(SNES.rom, Const.ScreenDataPointersOffset[1] + id * 3)), SNES.rom, dumpOffset, Const.ScreenCount[i, 1] * 0x80);
                    //Update Pointer
                    dumpAddr = SNES.OffsetToCpu(dumpOffset) | addrMask;
                    BinaryPrimitives.WriteUInt16LittleEndian(SNES.rom.AsSpan(Const.ScreenDataPointersOffset[1] + id * 3), (ushort)(dumpAddr & 0xFFFF));
                    SNES.rom[Const.ScreenDataPointersOffset[1] + id * 3 + 2] = (byte)((dumpAddr >> 16) & 0xFF);


                    //Increament Dump Offset
                    dumpOffset += Const.ExpandMaxScreens[1] * 0x80;

                    //Dump Existing 32x32 Tile Data
                    Array.Copy(SNES.rom, SNES.CpuToOffset(BitConverter.ToInt32(SNES.rom, Const.Tile32DataPointersOffset[0] + id * 3)), SNES.rom, dumpOffset, Const.Tile32Count[i, 0] * 8);
                    //Update Pointer
                    dumpAddr = SNES.OffsetToCpu(dumpOffset) | addrMask;
                    BinaryPrimitives.WriteUInt16LittleEndian(SNES.rom.AsSpan(Const.Tile32DataPointersOffset[0] + id * 3), (ushort)(dumpAddr & 0xFFFF));
                    SNES.rom[Const.Tile32DataPointersOffset[0] + id * 3 + 2] = (byte)((dumpAddr >> 16) & 0xFF);

                    //Increament Dump Offset
                    dumpOffset += Const.ExpandMaxTiles32[0];

                    //Dump Existing 32x32 Tile Data (Layer 2)
                    Array.Copy(SNES.rom, SNES.CpuToOffset(BitConverter.ToInt32(SNES.rom, Const.Tile32DataPointersOffset[1] + id * 3)), SNES.rom, dumpOffset, Const.Tile32Count[i, 1] * 8);
                    dumpAddr = SNES.OffsetToCpu(dumpOffset) | addrMask;
                    BinaryPrimitives.WriteUInt16LittleEndian(SNES.rom.AsSpan(Const.Tile32DataPointersOffset[1] + id * 3), (ushort)(dumpAddr & 0xFFFF));
                    SNES.rom[Const.Tile32DataPointersOffset[1] + id * 3 + 2] = (byte)((dumpAddr >> 16) & 0xFF);

                    //Set Max 32x32 Tiles for Layer 1 & 2
                    Const.Tile32Count[i, 0] = Const.ExpandMaxTiles32[0];
                    Const.Tile32Count[i, 1] = Const.ExpandMaxTiles32[1];

                    //Increament Dump Offset
                    dumpOffset += Const.ExpandMaxTiles32[1];

                    //Dump Existing 16x16 Tile Collision Data
                    Array.Copy(SNES.rom, SNES.CpuToOffset(BitConverter.ToInt32(SNES.rom, Const.TileCollisionDataPointersOffset + id * 3)), SNES.rom, dumpOffset, Const.Tile16Count[i, 0]);
                    dumpAddr = SNES.OffsetToCpu(dumpOffset) | addrMask;
                    BinaryPrimitives.WriteUInt16LittleEndian(SNES.rom.AsSpan(Const.TileCollisionDataPointersOffset + id * 3), (ushort)(dumpAddr & 0xFFFF));
                    SNES.rom[Const.TileCollisionDataPointersOffset + id * 3 + 2] = (byte)((dumpAddr >> 16) & 0xFF);

                    //Increament Dump Offset
                    dumpOffset += Const.ExpandMaxTiles16;

                    //Update Compressed Layout Data Pointer
                    dumpAddr = SNES.OffsetToCpu(dumpOffset) | addrMask;
                    BinaryPrimitives.WriteUInt16LittleEndian(SNES.rom.AsSpan(Const.LayoutPointersOffset[0] + id * 3), (ushort)(dumpAddr & 0xFFFF));
                    SNES.rom[Const.LayoutPointersOffset[0] + id * 3 + 2] = (byte)((dumpAddr >> 16) & 0xFF);

                    //Increament Dump Offset
                    dumpOffset += Const.ExpandLayoutLength;

                    //Update Compressed Layout 2 Data Pointer
                    dumpAddr = SNES.OffsetToCpu(dumpOffset) | addrMask;
                    BinaryPrimitives.WriteUInt16LittleEndian(SNES.rom.AsSpan(Const.LayoutPointersOffset[1] + id * 3), (ushort)(dumpAddr & 0xFFFF));
                    SNES.rom[Const.LayoutPointersOffset[1] + id * 3 + 2] = (byte)((dumpAddr >> 16) & 0xFF);

                    //Increament Dump Offset to Next Bank
                    if ((dumpOffset % 0x8000) != 0)
                        dumpOffset += 0x8000 - (dumpOffset % 0x8000);

                    //Dump Existing 16x16 Tile Data
                    Array.Copy(SNES.rom, SNES.CpuToOffset(BitConverter.ToInt32(SNES.rom, Const.Tile16DataPointersOffset[0] + id * 3)), SNES.rom, dumpOffset, Const.Tile16Count[i, 0] * 8);
                    dumpAddr = SNES.OffsetToCpu(dumpOffset) | addrMask;
                    BinaryPrimitives.WriteUInt16LittleEndian(SNES.rom.AsSpan(Const.Tile16DataPointersOffset[0] + id * 3), (ushort)(dumpAddr & 0xFFFF));
                    SNES.rom[Const.Tile16DataPointersOffset[0] + id * 3 + 2] = (byte)((dumpAddr >> 16) & 0xFF);

                    //Set Max 16x16 Tiles for Layer 1 & 2
                    Const.Tile16Count[i, 0] = Const.ExpandMaxTiles16;
                    Const.Tile16Count[i, 1] = Const.ExpandMaxTiles16;

                    //Increament Dump Offset to Next Bank
                    if ((dumpOffset % 0x8000) != 0)
                        dumpOffset += 0x8000 - (dumpOffset % 0x8000);

                    //Set New Layout Length
                    Const.LayoutLength[i, 0] = Const.ExpandLayoutLength;
                    Const.LayoutLength[i, 1] = Const.ExpandLayoutLength;
                }

                //Done
                SNES.edit = true;
                SNES.expanded = true;
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
