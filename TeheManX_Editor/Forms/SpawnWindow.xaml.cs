using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace TeheManX_Editor.Forms
{
    /// <summary>
    /// Interaction logic for SpawnWindow.xaml
    /// </summary>
    public partial class SpawnWindow : UserControl
    {
        #region Constructors
        public SpawnWindow()
        {
            InitializeComponent();
        }
        #endregion Constructors

        #region Methods
        public void SetSpawnSettings()
        {
            if (Level.Id >= Const.PlayableLevelsCount)
            {
                spawnInt.IsEnabled = false;
                objectTileInt.IsEnabled = false;
                backgroundTileInt.IsEnabled = false;
                backgroundPalInt.IsEnabled = false;
                megaIntX.IsEnabled = false;
                megaIntY.IsEnabled = false;
                camIntX.IsEnabled = false;
                camIntY.IsEnabled = false;
                bg2IntX.IsEnabled = false;
                bg2IntY.IsEnabled = false;
                camBorderIntL.IsEnabled = false;
                camBorderIntR.IsEnabled = false;
                camBorderIntT.IsEnabled = false;
                camBorderIntB.IsEnabled = false;
                bg2IntBaseX.IsEnabled = false;
                bg2IntBaseY.IsEnabled = false;
                megaFlipInt.IsEnabled = false;
                collisionInt.IsEnabled = false;
            }
            else
            {
                //calculate the max amount of checkpoints for the level
                int maxCheckpoints = 0;
                int checkpointSize = (Const.Id == Const.GameId.MegaManX) ? 0x1D : 0x1F;
                if (Level.Id != (Const.PlayableLevelsCount - 1))
                {
                    //get the start of the next level's checkpoint list
                    int nextOffset = BitConverter.ToUInt16(SNES.rom, Const.CheckpointOffset + (Level.Id + 1) * 2);
                    int currentOffset = BitConverter.ToUInt16(SNES.rom, Const.CheckpointOffset + Level.Id * 2); 

                    if (Const.Id != Const.GameId.MegaManX)
                    {
                        //Note: MegaMan X2 does not keep the offsets in order
                        ushort[] offsetList = new ushort[Const.PlayableLevelsCount];
                        for (int i = 0; i < Const.PlayableLevelsCount; i++)
                            offsetList[i] = BitConverter.ToUInt16(SNES.rom, Const.CheckpointOffset + i * 2);
                        Array.Sort(offsetList);
                        nextOffset = offsetList[Array.IndexOf(offsetList,(ushort)currentOffset) + 1];
                    }
                    maxCheckpoints = ((nextOffset - currentOffset) / 2) - 1;
                }
                else
                {
                    //use the first levels offsets to determine the last level's max checkpoints
                    int firstOffset = BitConverter.ToUInt16(SNES.rom, Const.CheckpointOffset + 0 * 2) + Const.CheckpointOffset;
                    int firstDataOffset = BitConverter.ToUInt16(SNES.rom, firstOffset);
                    ushort lastOffset = BitConverter.ToUInt16(SNES.rom, Level.Id * 2 + Const.CheckpointOffset);
                    maxCheckpoints = ((firstDataOffset - lastOffset) / 2) - 1;
                }
                if (spawnInt.Value > maxCheckpoints)
                    spawnInt.Value = maxCheckpoints;
                spawnInt.Maximum = maxCheckpoints;
                spawnInt.IsEnabled = true;
                objectTileInt.IsEnabled = true;
                if (Const.Id != Const.GameId.MegaManX)
                {
                    unknownInt.IsEnabled = true;
                    unknown2Int.IsEnabled = true;
                    unknownInt.Visibility = Visibility.Visible;
                    unknown2Int.Visibility = Visibility.Visible;
                    unknownText.Visibility = Visibility.Visible;
                    unknownText2.Visibility = Visibility.Visible;
                }
                else
                {
                    unknownInt.Visibility = Visibility.Collapsed;
                    unknown2Int.Visibility = Visibility.Collapsed;
                    unknownText.Visibility = Visibility.Collapsed;
                    unknownText2.Visibility = Visibility.Collapsed;
                }
                backgroundPalInt.IsEnabled = true;
                megaIntX.IsEnabled = true;
                megaIntY.IsEnabled = true;
                camIntX.IsEnabled = true;
                camIntY.IsEnabled = true;
                bg2IntX.IsEnabled = true;
                bg2IntY.IsEnabled = true;
                camBorderIntL.IsEnabled = true;
                camBorderIntR.IsEnabled = true;
                camBorderIntT.IsEnabled = true;
                camBorderIntB.IsEnabled = true;
                bg2IntBaseX.IsEnabled = true;
                bg2IntBaseY.IsEnabled = true;
                megaFlipInt.IsEnabled = true;
                collisionInt.IsEnabled = true;

                int listOffset = BitConverter.ToUInt16(SNES.rom, Const.CheckpointOffset + Level.Id * 2) + Const.CheckpointOffset;
                int offset = BitConverter.ToUInt16(SNES.rom, listOffset + (int)spawnInt.Value * 2) + Const.CheckpointOffset;
                SetIntValues(offset);
            }
        }
        private void SetIntValues(int offset)
        {
            objectTileInt.Value = SNES.rom[offset + 0];
            backgroundTileInt.Value = SNES.rom[offset + 1];
            backgroundPalInt.Value = SNES.rom[offset + 2];
            if (Const.Id != Const.GameId.MegaManX)
            {
                unknownInt.Value = SNES.rom[offset + 3];
                unknown2Int.Value = SNES.rom[offset + 0x1C];
                megaFlipInt.Value = SNES.rom[offset  + 0x1E];
                collisionInt.Value = SNES.rom[offset + 0x1D];
                offset++;
            }
            else
            {
                megaFlipInt.Value = SNES.rom[offset + 27];
                collisionInt.Value = SNES.rom[offset + 28];
            }
            megaIntX.Value = BitConverter.ToUInt16(SNES.rom, offset + 3);
            megaIntY.Value = BitConverter.ToUInt16(SNES.rom, offset + 5);
            camIntX.Value = BitConverter.ToUInt16(SNES.rom, offset + 7);
            camIntY.Value = BitConverter.ToUInt16(SNES.rom, offset + 9);
            bg2IntX.Value = BitConverter.ToUInt16(SNES.rom, offset + 11);
            bg2IntY.Value = BitConverter.ToUInt16(SNES.rom, offset + 13);
            camBorderIntL.Value = BitConverter.ToUInt16(SNES.rom, offset + 15);
            camBorderIntR.Value = BitConverter.ToUInt16(SNES.rom, offset + 17);
            camBorderIntT.Value = BitConverter.ToUInt16(SNES.rom, offset + 19);
            camBorderIntB.Value = BitConverter.ToUInt16(SNES.rom, offset + 21);
            bg2IntBaseX.Value = BitConverter.ToUInt16(SNES.rom, offset + 23);
            bg2IntBaseY.Value = BitConverter.ToUInt16(SNES.rom, offset + 25);
        }
        #endregion Methods

        #region Events
        private void spawnInt_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue == null || SNES.rom == null || Level.Id >= Const.PlayableLevelsCount)
                return;
            int listOffset = BitConverter.ToUInt16(SNES.rom, Const.CheckpointOffset + Level.Id * 2) + Const.CheckpointOffset;
            int offset = BitConverter.ToUInt16(SNES.rom, listOffset + (int)e.NewValue * 2) + Const.CheckpointOffset;
            SetIntValues(offset);
        }
        private void objectTileInt_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue == null || SNES.rom == null || Level.Id >= Const.PlayableLevelsCount)
                return;
            int listOffset = BitConverter.ToUInt16(SNES.rom, Const.CheckpointOffset + Level.Id * 2) + Const.CheckpointOffset;
            int offset = BitConverter.ToUInt16(SNES.rom, listOffset + (int)spawnInt.Value * 2) + Const.CheckpointOffset;
            byte value = (byte)(int)e.NewValue;
            if (value == SNES.rom[offset])
                return;
            SNES.rom[offset] = value;
            SNES.edit = true;
        }
        private void backgroundTileInt_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue == null || SNES.rom == null || Level.Id >= Const.PlayableLevelsCount)
                return;
            int listOffset = BitConverter.ToUInt16(SNES.rom, Const.CheckpointOffset + Level.Id * 2) + Const.CheckpointOffset;
            int offset = BitConverter.ToUInt16(SNES.rom, listOffset + (int)spawnInt.Value * 2) + Const.CheckpointOffset;
            byte value = (byte)(int)e.NewValue;
            if (value == SNES.rom[offset + 1])
                return;
            SNES.rom[offset + 1] = value;
            SNES.edit = true;
        }
        private void backgroundPalInt_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue == null || SNES.rom == null || Level.Id >= Const.PlayableLevelsCount)
                return;
            int listOffset = BitConverter.ToUInt16(SNES.rom, Const.CheckpointOffset + Level.Id * 2) + Const.CheckpointOffset;
            int offset = BitConverter.ToUInt16(SNES.rom, listOffset + (int)spawnInt.Value * 2) + Const.CheckpointOffset;
            byte value = (byte)(int)e.NewValue;
            if (value== SNES.rom[offset + 2])
                return;
            SNES.rom[offset + 2] = value;
            SNES.edit = true;
        }
        private void unknownInt_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue == null || SNES.rom == null || Level.Id >= Const.PlayableLevelsCount)
                return;
            int listOffset = BitConverter.ToUInt16(SNES.rom, Const.CheckpointOffset + Level.Id * 2) + Const.CheckpointOffset;
            int offset = BitConverter.ToUInt16(SNES.rom, listOffset + (int)spawnInt.Value * 2) + Const.CheckpointOffset;

            if ((byte)(int)e.NewValue == SNES.rom[offset + 3])
                return;
            SNES.rom[offset + 3] = (byte)(int)e.NewValue;
            SNES.edit = true;
        }
        private void megaIntX_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue == null || SNES.rom == null || Level.Id >= Const.PlayableLevelsCount)
                return;
            int listOffset = BitConverter.ToUInt16(SNES.rom, Const.CheckpointOffset + Level.Id * 2) + Const.CheckpointOffset;
            int offset = BitConverter.ToUInt16(SNES.rom, listOffset + (int)spawnInt.Value * 2) + Const.CheckpointOffset;

            if (Const.Id != Const.GameId.MegaManX) offset++;
            ushort value = (ushort)(int)e.NewValue;
            if (value == BitConverter.ToUInt16(SNES.rom, offset + 0x3))
                return;
            BinaryPrimitives.WriteUInt16LittleEndian(SNES.rom.AsSpan(offset + 0x3), value);
            SNES.edit = true;
        }
        private void megaIntY_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue == null || SNES.rom == null || Level.Id >= Const.PlayableLevelsCount)
                return;
            int listOffset = BitConverter.ToUInt16(SNES.rom, Const.CheckpointOffset + Level.Id * 2) + Const.CheckpointOffset;
            int offset = BitConverter.ToUInt16(SNES.rom, listOffset + (int)spawnInt.Value * 2) + Const.CheckpointOffset;

            if (Const.Id != Const.GameId.MegaManX) offset++;
            ushort value = (ushort)(int)e.NewValue;
            if (value == BitConverter.ToUInt16(SNES.rom,offset + 0x5))
                return;
            BinaryPrimitives.WriteUInt16LittleEndian(SNES.rom.AsSpan(offset + 0x5), value);
            SNES.edit = true;
        }
        private void camIntX_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue == null || SNES.rom == null || Level.Id >= Const.PlayableLevelsCount)
                return;
            int listOffset = BitConverter.ToUInt16(SNES.rom, Const.CheckpointOffset + Level.Id * 2) + Const.CheckpointOffset;
            int offset = BitConverter.ToUInt16(SNES.rom, listOffset + (int)spawnInt.Value * 2) + Const.CheckpointOffset;

            if (Const.Id != Const.GameId.MegaManX) offset++;
            ushort value = (ushort)(int)e.NewValue;
            if (value == BitConverter.ToUInt16(SNES.rom, offset + 0x7))
                return;
            BinaryPrimitives.WriteUInt16LittleEndian(SNES.rom.AsSpan(offset + 0x7), value);
            SNES.edit = true;
        }
        private void camIntY_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue == null || SNES.rom == null || Level.Id >= Const.PlayableLevelsCount)
                return;
            int listOffset = BitConverter.ToUInt16(SNES.rom, Const.CheckpointOffset + Level.Id * 2) + Const.CheckpointOffset;
            int offset = BitConverter.ToUInt16(SNES.rom, listOffset + (int)spawnInt.Value * 2) + Const.CheckpointOffset;

            if (Const.Id != Const.GameId.MegaManX) offset++;
            ushort value = (ushort)(int)e.NewValue;
            if (value == BitConverter.ToUInt16(SNES.rom, offset + 0x9))
                return;
            BinaryPrimitives.WriteUInt16LittleEndian(SNES.rom.AsSpan(offset + 0x9), value);
            SNES.edit = true;
        }
        private void bg2IntX_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue == null || SNES.rom == null || Level.Id >= Const.PlayableLevelsCount)
                return;
            int listOffset = BitConverter.ToUInt16(SNES.rom, Const.CheckpointOffset + Level.Id * 2) + Const.CheckpointOffset;
            int offset = BitConverter.ToUInt16(SNES.rom, listOffset + (int)spawnInt.Value * 2) + Const.CheckpointOffset;

            if (Const.Id != Const.GameId.MegaManX) offset++;
            ushort value = (ushort)(int)e.NewValue;
            if (value == BitConverter.ToUInt16(SNES.rom, offset + 0xB))
                return;
            BinaryPrimitives.WriteUInt16LittleEndian(SNES.rom.AsSpan(offset + 0xB), value);
            SNES.edit = true;
        }
        private void bg2IntY_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue == null || SNES.rom == null || Level.Id >= Const.PlayableLevelsCount)
                return;
            int listOffset = BitConverter.ToUInt16(SNES.rom, Const.CheckpointOffset + Level.Id * 2) + Const.CheckpointOffset;
            int offset = BitConverter.ToUInt16(SNES.rom, listOffset + (int)spawnInt.Value * 2) + Const.CheckpointOffset;

            if (Const.Id != Const.GameId.MegaManX) offset++;
            ushort value = (ushort)(int)e.NewValue;
            if (value == BitConverter.ToUInt16(SNES.rom, offset + 0xD))
                return;
            BinaryPrimitives.WriteUInt16LittleEndian(SNES.rom.AsSpan(offset + 0xD), value);
            SNES.edit = true;
        }
        private void camBorderIntL_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue == null || SNES.rom == null || Level.Id >= Const.PlayableLevelsCount)
                return;
            int listOffset = BitConverter.ToUInt16(SNES.rom, Const.CheckpointOffset + Level.Id * 2) + Const.CheckpointOffset;
            int offset = BitConverter.ToUInt16(SNES.rom, listOffset + (int)spawnInt.Value * 2) + Const.CheckpointOffset;

            if (Const.Id != Const.GameId.MegaManX) offset++;
            ushort value = (ushort)(int)e.NewValue;
            if (value == BitConverter.ToUInt16(SNES.rom, offset + 0xF))
                return;
            BinaryPrimitives.WriteUInt16LittleEndian(SNES.rom.AsSpan(offset + 0xF), value);
            SNES.edit = true;
        }
        private void camBorderIntR_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue == null || SNES.rom == null || Level.Id >= Const.PlayableLevelsCount)
                return;
            int listOffset = BitConverter.ToUInt16(SNES.rom, Const.CheckpointOffset + Level.Id * 2) + Const.CheckpointOffset;
            int offset = BitConverter.ToUInt16(SNES.rom, listOffset + (int)spawnInt.Value * 2) + Const.CheckpointOffset;

            if (Const.Id != Const.GameId.MegaManX) offset++;
            ushort value = (ushort)(int)e.NewValue;
            if (value == BitConverter.ToUInt16(SNES.rom, offset + 0x11))
                return;
            BinaryPrimitives.WriteUInt16LittleEndian(SNES.rom.AsSpan(offset + 0x11), value);
            SNES.edit = true;
        }
        private void camBorderIntT_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue == null || SNES.rom == null || Level.Id >= Const.PlayableLevelsCount)
                return;
            int listOffset = BitConverter.ToUInt16(SNES.rom, Const.CheckpointOffset + Level.Id * 2) + Const.CheckpointOffset;
            int offset = BitConverter.ToUInt16(SNES.rom, listOffset + (int)spawnInt.Value * 2) + Const.CheckpointOffset;

            if (Const.Id != Const.GameId.MegaManX) offset++;
            ushort value = (ushort)(int)e.NewValue;
            if (value == BitConverter.ToUInt16(SNES.rom, offset + 0x13))
                return;
            BinaryPrimitives.WriteUInt16LittleEndian(SNES.rom.AsSpan(offset + 0x13), value);
            SNES.edit = true;
        }
        private void camBorderIntB_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue == null || SNES.rom == null || Level.Id >= Const.PlayableLevelsCount)
                return;
            int listOffset = BitConverter.ToUInt16(SNES.rom, Const.CheckpointOffset + Level.Id * 2) + Const.CheckpointOffset;
            int offset = BitConverter.ToUInt16(SNES.rom, listOffset + (int)spawnInt.Value * 2) + Const.CheckpointOffset;

            if (Const.Id != Const.GameId.MegaManX) offset++;
            ushort value = (ushort)(int)e.NewValue;
            if (value == BitConverter.ToUInt16(SNES.rom, offset + 0x15))
                return;
            BinaryPrimitives.WriteUInt16LittleEndian(SNES.rom.AsSpan(offset + 0x15), value);
            SNES.edit = true;
        }
        private void bg2IntBaseX_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue == null || SNES.rom == null || Level.Id >= Const.PlayableLevelsCount)
                return;
            int listOffset = BitConverter.ToUInt16(SNES.rom, Const.CheckpointOffset + Level.Id * 2) + Const.CheckpointOffset;
            int offset = BitConverter.ToUInt16(SNES.rom, listOffset + (int)spawnInt.Value * 2) + Const.CheckpointOffset;

            if (Const.Id != Const.GameId.MegaManX) offset++;
            ushort value = (ushort)(int)e.NewValue;
            if (value == BitConverter.ToUInt16(SNES.rom, offset + 0x17))
                return;
            BinaryPrimitives.WriteUInt16LittleEndian(SNES.rom.AsSpan(offset + 0x17), value);
            SNES.edit = true;
        }
        private void bg2IntBaseY_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue == null || SNES.rom == null || Level.Id >= Const.PlayableLevelsCount)
                return;
            int listOffset = BitConverter.ToUInt16(SNES.rom, Const.CheckpointOffset + Level.Id * 2) + Const.CheckpointOffset;
            int offset = BitConverter.ToUInt16(SNES.rom, listOffset + (int)spawnInt.Value * 2) + Const.CheckpointOffset;

            if (Const.Id != Const.GameId.MegaManX) offset++;
            ushort value = (ushort)(int)e.NewValue;
            if (value == BitConverter.ToUInt16(SNES.rom, offset + 0x19))
                return;
            BinaryPrimitives.WriteUInt16LittleEndian(SNES.rom.AsSpan(offset + 0x19), value);
            SNES.edit = true;
        }
        private void unknown2Int_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue == null || SNES.rom == null || Level.Id >= Const.PlayableLevelsCount)
                return;
            int listOffset = BitConverter.ToUInt16(SNES.rom, Const.CheckpointOffset + Level.Id * 2) + Const.CheckpointOffset;
            int offset = BitConverter.ToUInt16(SNES.rom, listOffset + (int)spawnInt.Value * 2) + Const.CheckpointOffset;

            if ((byte)(int)e.NewValue == SNES.rom[offset + 0x1C])
                return;
            SNES.rom[offset + 0x1C] = (byte)(int)e.NewValue;
            SNES.edit = true;
        }
        private void megaFlipInt_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue == null || SNES.rom == null || Level.Id >= Const.PlayableLevelsCount)
                return;
            int listOffset = BitConverter.ToUInt16(SNES.rom, Const.CheckpointOffset + Level.Id * 2) + Const.CheckpointOffset;
            int offset = BitConverter.ToUInt16(SNES.rom, listOffset + (int)spawnInt.Value * 2) + Const.CheckpointOffset;

            if (Const.Id != Const.GameId.MegaManX)
                offset += 0x1E;
            else
                offset += 0x1B;
            byte value = (byte)(int)e.NewValue;

            if (value == SNES.rom[offset])
                return;
            SNES.rom[offset] = value;
            SNES.edit = true;
        }
        private void collisionInt_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue == null || SNES.rom == null || Level.Id >= Const.PlayableLevelsCount)
                return;
            int listOffset = BitConverter.ToUInt16(SNES.rom, Const.CheckpointOffset + Level.Id * 2) + Const.CheckpointOffset;
            int offset = BitConverter.ToUInt16(SNES.rom, listOffset + (int)spawnInt.Value * 2) + Const.CheckpointOffset;

            if (Const.Id != Const.GameId.MegaManX)
                offset += 0x1D;
            else
                offset += 0x1C;
            byte value = (byte)(int)e.NewValue;

            if (value == SNES.rom[offset])
                return;
            SNES.rom[offset] = value;
            SNES.edit = true;
        }
        private void GearBtn_Click(object sender, RoutedEventArgs e) // Configure Max Checkpoints in the current stage
        {
            if (Level.Id >= Const.PlayableLevelsCount)
            {
                MessageBox.Show("This level does not support checkpoints.", "ERROR");
                return;
            }
            // 1st we are getting the amount of checkpoints in each level
            byte[] checkpointsCount = new byte[Const.PlayableLevelsCount];


            for (int s = 0; s < Const.PlayableLevelsCount; s++)
            {
                int maxCheckpoints = 0;
                if (s != (Const.PlayableLevelsCount - 1))
                {
                    int nextOffset = BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(Const.CheckpointOffset + (s + 1) * 2));
                    int currentOffset = BitConverter.ToUInt16(SNES.rom, Const.CheckpointOffset + s * 2);

                    if (true)
                    {
                        //Note: MegaMan X2 does not keep the offsets in order
                        ushort[] offsetList = new ushort[Const.PlayableLevelsCount];
                        for (int i = 0; i < Const.PlayableLevelsCount; i++)
                            offsetList[i] = BitConverter.ToUInt16(SNES.rom, Const.CheckpointOffset + i * 2);
                        Array.Sort(offsetList);
                        nextOffset = offsetList[Array.IndexOf(offsetList, (ushort)currentOffset) + 1];
                    }
                    maxCheckpoints = (nextOffset - currentOffset) / 2;
                }
                else
                {
                    //use the first levels offsets to determine the last level's max checkpoints
                    int firstOffset = BitConverter.ToUInt16(SNES.rom, Const.CheckpointOffset + 0 * 2) + Const.CheckpointOffset;
                    int firstDataOffset = BitConverter.ToUInt16(SNES.rom, firstOffset);
                    ushort lastOffset = BitConverter.ToUInt16(SNES.rom, s * 2 + Const.CheckpointOffset);
                    maxCheckpoints = (firstDataOffset - lastOffset) / 2;
                }
                checkpointsCount[s] = (byte)maxCheckpoints;
            }


            // Create UI elements
            Window window = new Window
            {
                Title = "Info",
                SizeToContent = SizeToContent.WidthAndHeight,
                ResizeMode = ResizeMode.NoResize,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = Application.Current.MainWindow
            };
            TextBlock textBlock = new TextBlock() { Text = $"Max Total Check Points: {Const.MaxTotalCheckpoints}" , FontSize = 18, Foreground = Brushes.White , FontFamily = new FontFamily("Consolas")};
            NumInt numInt = new NumInt() { Margin = new Thickness(5), Minimum = 1, Maximum = Const.MaxTotalCheckpoints, Value = spawnInt.Maximum + 1, Width = 100, FontFamily = new FontFamily("Consolas"), FontSize = 16 };
            Grid.SetColumn(numInt, 1);
            Grid.SetRow(numInt, 1);
            Button confirmBtn = new Button() { Content = "OK", Width = 75, Height = 30, Margin = new Thickness(5) };
            confirmBtn.Click += (se, ev) =>
            {
                if (numInt.Value == null)
                    return;
                int total = 0;
                for (int i = 0; i < Const.PlayableLevelsCount; i++)
                {
                    if (i != Level.Id)
                        total += checkpointsCount[i];
                    else
                        total += (int)numInt.Value;
                }
                if (total > Const.MaxTotalCheckpoints)
                {
                    MessageBox.Show($"The total amount of checkpoints across all levels cannot exceed {Const.MaxTotalCheckpoints}.\nCurrent Total: {total}", "ERROR");
                    return;
                }
                //update the current level's max checkpoints
                List<byte[]>[] checkpointData = new List<byte[]>[Const.PlayableLevelsCount];

                // initialize each List<>
                for (int i = 0; i < checkpointData.Length; i++)
                    checkpointData[i] = new List<byte[]>();

                int checkpointSize = (Const.Id == Const.GameId.MegaManX) ? 0x1D : 0x1F;

                // now fill in the orignal checkpoint data
                for (int s = 0; s < Const.PlayableLevelsCount; s++)
                {
                    int first = BitConverter.ToUInt16(SNES.rom, Const.CheckpointOffset + s * 2);
                    int listOffset = BitConverter.ToUInt16(SNES.rom, Const.CheckpointOffset + s * 2) + Const.CheckpointOffset;

                    for (int c = 0; c < checkpointsCount[s]; c++)
                    {
                        int offset = BitConverter.ToUInt16(SNES.rom, listOffset + c * 2) + Const.CheckpointOffset;
                        byte[] checkpointBytes = new byte[checkpointSize];
                        Array.Copy(SNES.rom, offset, checkpointBytes, 0, checkpointSize);
                        checkpointData[s].Add(checkpointBytes);
                    }
                }

                // now rebuild the checkpoint list
                if (checkpointData[Level.Id].Count < (int)numInt.Value)
                {
                    while (checkpointData[Level.Id].Count < (int)numInt.Value)
                        checkpointData[Level.Id].Add(new byte[checkpointSize]); //add a blank checkpoint
                }
                else if (checkpointData[Level.Id].Count > (int)numInt.Value)
                {
                    while (checkpointData[Level.Id].Count > (int)numInt.Value)
                        checkpointData[Level.Id].RemoveAt(checkpointData[Level.Id].Count - 1); //remove last checkpoint
                }

                // Now test outputting the checkpoint data
                int checkpointDataStartOffset = Const.CheckpointOffset + Const.PlayableLevelsCount * 2 + Const.MaxTotalCheckpoints * 2;
                int checkpointPointersStartOffset = Const.CheckpointOffset + Const.PlayableLevelsCount * 2;


                for (int s = 0; s < Const.PlayableLevelsCount; s++)
                {
                    for (int i = 0; i < checkpointData[s].Count; i++)
                    {
                        Array.Copy(checkpointData[s][i], 0, SNES.rom, checkpointDataStartOffset, checkpointSize);
                        // Now write the pointer
                        BinaryPrimitives.WriteUInt16LittleEndian(SNES.rom.AsSpan(checkpointPointersStartOffset), (ushort)(checkpointDataStartOffset - Const.CheckpointOffset));

                        checkpointPointersStartOffset += 2;
                        checkpointDataStartOffset += checkpointSize;
                    }
                }


                // Now write the start of each level's checkpoint pointers
                checkpointPointersStartOffset = Const.CheckpointOffset + Const.PlayableLevelsCount * 2;

                for (int s = 0; s < Const.PlayableLevelsCount; s++)
                {
                    BinaryPrimitives.WriteUInt16LittleEndian(SNES.rom.AsSpan(Const.CheckpointOffset + s * 2), (ushort)(checkpointPointersStartOffset - Const.CheckpointOffset));
                    checkpointPointersStartOffset += checkpointData[s].Count * 2;
                }
                SNES.edit = true;
                spawnInt.Maximum = (int)numInt.Value - 1;
                if (spawnInt.Value > spawnInt.Maximum)
                    spawnInt.Value = spawnInt.Maximum;
                MessageBox.Show("Checkpoint configuration updated successfully!");
                window.Close();
            };
            Grid.SetRow(confirmBtn, 1);
            Grid grid = new Grid();
            grid.Background = Brushes.Black;
            grid.ColumnDefinitions.Add(new ColumnDefinition());
            grid.ColumnDefinitions.Add(new ColumnDefinition());
            grid.RowDefinitions.Add(new RowDefinition());
            grid.RowDefinitions.Add(new RowDefinition());
            grid.Children.Add(textBlock);
            grid.Children.Add(numInt);
            grid.Children.Add(confirmBtn);


            window.Content = grid;
            window.ShowDialog();
        }
        #endregion Events
    }
}
