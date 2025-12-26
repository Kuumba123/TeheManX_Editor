using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
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
        #region Fields
        public static List<List<Checkpoint>> Checkpoints = new List<List<Checkpoint>>();
        private static bool supressInts;
        private static int checkpointId;
        #endregion Fields

        #region Constructors
        public SpawnWindow()
        {
            supressInts = true;
            InitializeComponent();
            supressInts = false;
        }
        #endregion Constructors

        #region Methods
        public void CollectData()
        {
            int maxLevels = Const.Id == Const.GameId.MegaManX3 ? maxLevels = 0xF : Const.PlayableLevelsCount;

            Checkpoints.Clear();

            int[] checkpointAmount = new int[maxLevels];

            for (int Id = 0; Id < maxLevels; Id++)
            {
                //calculate the max amount of checkpoints for the level
                int maxCheckpoints = 0;

                if (Id != (maxLevels - 1))
                {
                    //get the start of the next level's checkpoint list
                    int nextOffset = BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(Const.CheckpointOffset + (Id + 1) * 2));
                    int currentOffset = BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(Const.CheckpointOffset + Id * 2));

                    if (Const.Id != Const.GameId.MegaManX)
                    {
                        //Note: MegaMan X2 does not keep the offsets in order
                        ushort[] offsetList = new ushort[maxLevels];
                        for (int i = 0; i < maxLevels; i++)
                            offsetList[i] = BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(Const.CheckpointOffset + i * 2));
                        Array.Sort(offsetList);
                        nextOffset = offsetList[Array.IndexOf(offsetList, (ushort)currentOffset) + 1];
                    }
                    maxCheckpoints = ((nextOffset - currentOffset) / 2);
                }
                else
                {
                    //use the first levels offsets to determine the last level's max checkpoints
                    int firstOffset = BinaryPrimitives.ReadInt16LittleEndian(SNES.rom.AsSpan(Const.CheckpointOffset)) + Const.CheckpointOffset;
                    int firstDataOffset = BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(firstOffset));
                    ushort lastOffset = BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(Id * 2 + Const.CheckpointOffset));
                    maxCheckpoints = ((firstDataOffset - lastOffset) / 2);
                }
                checkpointAmount[Id] = maxCheckpoints;
            }
            for (int Id = 0; Id < maxLevels; Id++)
            {
                List<Checkpoint> list = new List<Checkpoint>();
                int listOffset = BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(Const.CheckpointOffset + Id * 2)) + Const.CheckpointOffset;
                for (int i = 0; i < checkpointAmount[Id]; i++)
                {
                    int offset = BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(listOffset)) + Const.CheckpointOffset;
                    listOffset += 2;

                    Checkpoint point = new Checkpoint();

                    point.ObjectTileSetting = SNES.rom[offset];
                    point.BackgroundTileSetting = SNES.rom[offset + 1];
                    point.BackgroundPaletteSetting = SNES.rom[offset + 2];

                    if (Const.Id == Const.GameId.MegaManX)
                    {
                        point.MegaX = BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(offset + 3));
                        point.MegaY = BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(offset + 5));
                        point.CameraX = BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(offset + 7));
                        point.CameraY = BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(offset + 9));
                        point.BG2X = BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(offset + 0xB));
                        point.BG2Y = BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(offset + 0xD));
                        point.BorderLeft = BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(offset + 0xF));
                        point.BorderRight = BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(offset + 0x11));
                        point.BorderTop = BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(offset + 0x13));
                        point.BorderBottom = BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(offset + 0x15));
                        point.BG2X_Base = BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(offset + 0x17));
                        point.BG2Y_Base = BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(offset + 0x19));
                        point.MegaFlip = SNES.rom[offset + 0x1B];
                        point.CollisionTimer = SNES.rom[offset + 0x1C];
                    }
                    else
                    {
                        point.SilkShotType = SNES.rom[offset + 3];
                        point.MegaX = BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(offset + 4));
                        point.MegaY = BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(offset + 6));
                        point.CameraX = BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(offset + 8));
                        point.CameraY = BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(offset + 0xA));
                        point.BG2X = BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(offset + 0xC));
                        point.BG2Y = BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(offset + 0xE));
                        point.BorderLeft = BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(offset + 0x10));
                        point.BorderRight = BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(offset + 0x12));
                        point.BorderTop = BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(offset + 0x14));
                        point.BorderBottom = BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(offset + 0x16));
                        point.BG2X_Base = BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(offset + 0x18));
                        point.BG2Y_Base = BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(offset + 0x1A));
                        point.WramFlag = SNES.rom[offset + 0x1C];
                        point.MegaFlip = SNES.rom[offset + 0x1D];
                        point.CollisionTimer = SNES.rom[offset + 0x1E];
                    }
                    list.Add(point);
                }
                Checkpoints.Add(list);
            }
        }
        public void SetSpawnSettings()
        {
            if (Level.Id >= Const.PlayableLevelsCount || (Const.Id == Const.GameId.MegaManX3 && Level.Id > 0xE))
            {
                MainWindow.window.spawnE.spawnInt.IsEnabled = false;
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

                silkShotInt.IsEnabled = false;
                wramInt.IsEnabled = false;
                return;
            }

            if (Const.Id != Const.GameId.MegaManX)
            {
                silkShotInt.IsEnabled = true;
                wramInt.IsEnabled = true;
                silkShotInt.Visibility = Visibility.Visible;
                wramInt.Visibility = Visibility.Visible;
                silkShotTxt.Visibility = Visibility.Visible;
                wramTxt.Visibility = Visibility.Visible;
            }
            else
            {
                silkShotInt.Visibility = Visibility.Collapsed;
                wramInt.Visibility = Visibility.Collapsed;
                silkShotTxt.Visibility = Visibility.Collapsed;
                wramTxt.Visibility = Visibility.Collapsed;
            }
            spawnInt.IsEnabled = true;
            objectTileInt.IsEnabled = true;
            backgroundTileInt.IsEnabled = true;
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

            supressInts = true;
            checkpointId = 0;
            spawnInt.Value = 0;
            spawnInt.Maximum = Checkpoints[Level.Id].Count - 1;
            SetIntValues();
            supressInts = false;
        }
        private void SetIntValues()
        {
            Checkpoint point = Checkpoints[Level.Id][(int)spawnInt.Value];

            objectTileInt.Value = point.ObjectTileSetting;
            backgroundTileInt.Value = point.BackgroundTileSetting;
            backgroundPalInt.Value = point.BackgroundPaletteSetting;
            silkShotInt.Value = point.SilkShotType;
            megaIntX.Value = point.MegaX;
            megaIntY.Value = point.MegaY;
            camIntX.Value = point.CameraX;
            camIntY.Value = point.CameraY;
            bg2IntX.Value = point.BG2X;
            bg2IntY.Value = point.BG2Y;
            camBorderIntL.Value = point.BorderLeft;
            camBorderIntR.Value = point.BorderRight;
            camBorderIntT.Value = point.BorderTop;
            camBorderIntB.Value = point.BorderBottom;
            bg2IntBaseX.Value = point.BG2X_Base;
            bg2IntBaseY.Value = point.BG2Y_Base;
            wramInt.Value = point.WramFlag;
            megaFlipInt.Value = point.MegaFlip;
            collisionInt.Value = point.CollisionTimer;
        }
        public static byte[] CreateCheckpointData(List<List<Checkpoint>> sourceSettings)
        {
            //For the sake of keeping things simple this code is based off the code from the object tile data generator

            Dictionary<byte[], int> dict = new Dictionary<byte[], int>(ByteArrayComparer.Default);

            /*
             * Step 1. Create a dictionary of unique object settings data & keep track of stage keys
             */

            int nextKey = 0; //used as an offset into the background settings data table

            List<List<int>> keyList = new List<List<int>>(sourceSettings.Count);

            foreach (var innerList in sourceSettings)
                keyList.Add(Enumerable.Repeat(0, innerList.Count).ToList());

            int checkpointSize = Const.Id == Const.GameId.MegaManX ? 0x1D : 0x1F;

            for (int id = 0; id < sourceSettings.Count; id++)
            {
                for (int s = 0; s < sourceSettings[id].Count; s++)
                {
                    byte[] pointData = new byte[checkpointSize];
                    
                    Checkpoint point = sourceSettings[id][s];

                    pointData[0] = point.ObjectTileSetting;
                    pointData[1] = point.BackgroundTileSetting;
                    pointData[2] = point.BackgroundPaletteSetting;

                    if (Const.Id == Const.GameId.MegaManX)
                    {
                        BinaryPrimitives.WriteUInt16LittleEndian(pointData.AsSpan(3), point.MegaX);
                        BinaryPrimitives.WriteUInt16LittleEndian(pointData.AsSpan(5), point.MegaY);
                        BinaryPrimitives.WriteUInt16LittleEndian(pointData.AsSpan(7), point.CameraX);
                        BinaryPrimitives.WriteUInt16LittleEndian(pointData.AsSpan(9), point.CameraY);
                        BinaryPrimitives.WriteUInt16LittleEndian(pointData.AsSpan(0xB), point.BG2X);
                        BinaryPrimitives.WriteUInt16LittleEndian(pointData.AsSpan(0xD), point.BG2Y);
                        BinaryPrimitives.WriteUInt16LittleEndian(pointData.AsSpan(0xF), point.BorderLeft);
                        BinaryPrimitives.WriteUInt16LittleEndian(pointData.AsSpan(0x11), point.BorderRight);
                        BinaryPrimitives.WriteUInt16LittleEndian(pointData.AsSpan(0x13), point.BorderTop);
                        BinaryPrimitives.WriteUInt16LittleEndian(pointData.AsSpan(0x15), point.BorderBottom);
                        BinaryPrimitives.WriteUInt16LittleEndian(pointData.AsSpan(0x17), point.BG2X_Base);
                        BinaryPrimitives.WriteUInt16LittleEndian(pointData.AsSpan(0x19), point.BG2Y_Base);

                        pointData[0x1B] = point.MegaFlip;
                        pointData[0x1C] = point.CollisionTimer;
                    }
                    else
                    {
                        pointData[3] = point.SilkShotType;

                        BinaryPrimitives.WriteUInt16LittleEndian(pointData.AsSpan(4), point.MegaX);
                        BinaryPrimitives.WriteUInt16LittleEndian(pointData.AsSpan(6), point.MegaY);
                        BinaryPrimitives.WriteUInt16LittleEndian(pointData.AsSpan(8), point.CameraX);
                        BinaryPrimitives.WriteUInt16LittleEndian(pointData.AsSpan(0xA), point.CameraY);
                        BinaryPrimitives.WriteUInt16LittleEndian(pointData.AsSpan(0xC), point.BG2X);
                        BinaryPrimitives.WriteUInt16LittleEndian(pointData.AsSpan(0xE), point.BG2Y);
                        BinaryPrimitives.WriteUInt16LittleEndian(pointData.AsSpan(0x10), point.BorderLeft);
                        BinaryPrimitives.WriteUInt16LittleEndian(pointData.AsSpan(0x12), point.BorderRight);
                        BinaryPrimitives.WriteUInt16LittleEndian(pointData.AsSpan(0x14), point.BorderTop);
                        BinaryPrimitives.WriteUInt16LittleEndian(pointData.AsSpan(0x16), point.BorderBottom);
                        BinaryPrimitives.WriteUInt16LittleEndian(pointData.AsSpan(0x18), point.BG2X_Base);
                        BinaryPrimitives.WriteUInt16LittleEndian(pointData.AsSpan(0x1A), point.BG2Y_Base);

                        pointData[0x1C] = point.WramFlag;
                        pointData[0x1D] = point.MegaFlip;
                        pointData[0x1E] = point.CollisionTimer;

                    }

                    if (!dict.ContainsKey(pointData))
                    {
                        dict.Add(pointData, nextKey);
                        nextKey += pointData.Length;
                    }
                    int value = dict[pointData];
                    keyList[id][s] = value;
                }
            }

            /*
             * Step 2. Get the length of all the pointers
             */

            int totalPointersLength = 0;

            for (int id = 0; id < sourceSettings.Count; id++)
            {
                totalPointersLength += 2;

                for (int s = 0; s < sourceSettings[id].Count; s++)
                    totalPointersLength += 2;
            }

            /*
             * Step 3. Create the byte array and setup the pointers
             */

            int stagePointersLength = sourceSettings.Count * 2;
            int nextOffset = stagePointersLength;

            byte[] exportData = new byte[nextKey + totalPointersLength];

            //Fix the stage pointers
            for (int i = 0; i < sourceSettings.Count; i++)
            {
                BinaryPrimitives.WriteUInt16LittleEndian(exportData.AsSpan(i * 2), (ushort)nextOffset);
                nextOffset += sourceSettings[i].Count * 2;
            }
            //Fix the background setting pointers
            nextOffset = stagePointersLength;
            for (int i = 0; i < sourceSettings.Count; i++)
            {
                for (int st = 0; st < sourceSettings[i].Count; st++)
                    BinaryPrimitives.WriteUInt16LittleEndian(exportData.AsSpan(nextOffset + st * 2), (ushort)(keyList[i][st] + totalPointersLength));
                nextOffset += sourceSettings[i].Count * 2;
            }
            /*
             * Step 4. Copy the unique background settings data
             */
            nextOffset = totalPointersLength;
            foreach (var kvp in dict)
            {
                kvp.Key.CopyTo(exportData.AsSpan(nextOffset));
                nextOffset += kvp.Key.Length;
            }

            // Done
            return exportData;
        }
        #endregion Methods

        #region Events
        private void spawnInt_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue == null || SNES.rom == null || Level.Id >= Const.PlayableLevelsCount || supressInts)
                return;
            supressInts = true;
            checkpointId = (int)e.NewValue;
            SetIntValues();
            supressInts = false;
        }
        private void objectTileInt_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue == null || SNES.rom == null || Level.Id >= Const.PlayableLevelsCount || supressInts)
                return;

            int id = Level.Id;
            int p = checkpointId;

            byte valueNew = (byte)(int)e.NewValue;

            if (Checkpoints[id][p].ObjectTileSetting == valueNew) return;

            Checkpoints[id][p].ObjectTileSetting = valueNew;
            SNES.edit = true;
        }
        private void backgroundTileInt_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue == null || SNES.rom == null || Level.Id >= Const.PlayableLevelsCount || supressInts)
                return;

            int id = Level.Id;
            int p = checkpointId;

            byte valueNew = (byte)(int)e.NewValue;

            if (Checkpoints[id][p].BackgroundTileSetting == valueNew) return;

            Checkpoints[id][p].BackgroundTileSetting = valueNew;
            SNES.edit = true;
        }
        private void backgroundPalInt_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue == null || SNES.rom == null || Level.Id >= Const.PlayableLevelsCount || supressInts)
                return;

            int id = Level.Id;
            int p = checkpointId;

            byte valueNew = (byte)(int)e.NewValue;

            if (Checkpoints[id][p].BackgroundPaletteSetting == valueNew) return;

            Checkpoints[id][p].BackgroundPaletteSetting = valueNew;
            SNES.edit = true;
        }
        private void silkShotInt_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue == null || SNES.rom == null || Level.Id >= Const.PlayableLevelsCount || supressInts)
                return;

            int id = Level.Id;
            int p = checkpointId;

            byte valueNew = (byte)(int)e.NewValue;

            if (Checkpoints[id][p].SilkShotType == valueNew) return;

            Checkpoints[id][p].SilkShotType = valueNew;
            SNES.edit = true;
        }
        private void megaIntX_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue == null || SNES.rom == null || Level.Id >= Const.PlayableLevelsCount || supressInts)
                return;

            int id = Level.Id;
            int p = checkpointId;

            ushort valueNew = (ushort)(int)e.NewValue;

            if (Checkpoints[id][p].MegaX == valueNew) return;

            Checkpoints[id][p].MegaX = valueNew;
            SNES.edit = true;
        }
        private void megaIntY_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue == null || SNES.rom == null || Level.Id >= Const.PlayableLevelsCount || supressInts)
                return;

            int id = Level.Id;
            int p = checkpointId;

            ushort valueNew = (ushort)(int)e.NewValue;

            if (Checkpoints[id][p].MegaY == valueNew) return;

            Checkpoints[id][p].MegaY = valueNew;
            SNES.edit = true;
        }
        private void camIntX_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue == null || SNES.rom == null || Level.Id >= Const.PlayableLevelsCount || supressInts)
                return;

            int id = Level.Id;
            int p = checkpointId;

            ushort valueNew = (ushort)(int)e.NewValue;

            if (Checkpoints[id][p].CameraX == valueNew) return;

            Checkpoints[id][p].CameraX = valueNew;
            SNES.edit = true;
        }
        private void camIntY_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue == null || SNES.rom == null || Level.Id >= Const.PlayableLevelsCount || supressInts)
                return;

            int id = Level.Id;
            int p = checkpointId;

            ushort valueNew = (ushort)(int)e.NewValue;

            if (Checkpoints[id][p].CameraY == valueNew) return;

            Checkpoints[id][p].CameraY = valueNew;
            SNES.edit = true;
        }
        private void bg2IntX_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue == null || SNES.rom == null || Level.Id >= Const.PlayableLevelsCount || supressInts)
                return;

            int id = Level.Id;
            int p = checkpointId;

            ushort valueNew = (ushort)(int)e.NewValue;

            if (Checkpoints[id][p].BG2X == valueNew) return;

            Checkpoints[id][p].BG2X = valueNew;
            SNES.edit = true;
        }
        private void bg2IntY_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue == null || SNES.rom == null || Level.Id >= Const.PlayableLevelsCount || supressInts)
                return;

            int id = Level.Id;
            int p = checkpointId;

            ushort valueNew = (ushort)(int)e.NewValue;

            if (Checkpoints[id][p].BG2Y == valueNew) return;

            Checkpoints[id][p].BG2Y = valueNew;
            SNES.edit = true;
        }
        private void camBorderIntL_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue == null || SNES.rom == null || Level.Id >= Const.PlayableLevelsCount || supressInts)
                return;

            int id = Level.Id;
            int p = checkpointId;

            ushort valueNew = (ushort)(int)e.NewValue;

            if (Checkpoints[id][p].BorderLeft == valueNew) return;

            Checkpoints[id][p].BorderLeft = valueNew;
            SNES.edit = true;
        }
        private void camBorderIntR_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue == null || SNES.rom == null || Level.Id >= Const.PlayableLevelsCount || supressInts)
                return;

            int id = Level.Id;
            int p = checkpointId;

            ushort valueNew = (ushort)(int)e.NewValue;

            if (Checkpoints[id][p].BorderRight == valueNew) return;

            Checkpoints[id][p].BorderRight = valueNew;
            SNES.edit = true;
        }
        private void camBorderIntT_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue == null || SNES.rom == null || Level.Id >= Const.PlayableLevelsCount || supressInts)
                return;

            int id = Level.Id;
            int p = checkpointId;

            ushort valueNew = (ushort)(int)e.NewValue;

            if (Checkpoints[id][p].BorderTop == valueNew) return;

            Checkpoints[id][p].BorderTop = valueNew;
            SNES.edit = true;
        }
        private void camBorderIntB_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue == null || SNES.rom == null || Level.Id >= Const.PlayableLevelsCount || supressInts)
                return;

            int id = Level.Id;
            int p = checkpointId;

            ushort valueNew = (ushort)(int)e.NewValue;

            if (Checkpoints[id][p].BorderBottom == valueNew) return;

            Checkpoints[id][p].BorderBottom = valueNew;
            SNES.edit = true;
        }
        private void bg2IntBaseX_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue == null || SNES.rom == null || Level.Id >= Const.PlayableLevelsCount || supressInts)
                return;

            int id = Level.Id;
            int p = checkpointId;

            ushort valueNew = (ushort)(int)e.NewValue;

            if (Checkpoints[id][p].BG2X_Base == valueNew) return;

            Checkpoints[id][p].BG2X_Base = valueNew;
            SNES.edit = true;
        }
        private void bg2IntBaseY_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue == null || SNES.rom == null || Level.Id >= Const.PlayableLevelsCount || supressInts)
                return;

            int id = Level.Id;
            int p = checkpointId;

            ushort valueNew = (ushort)(int)e.NewValue;

            if (Checkpoints[id][p].BG2Y_Base == valueNew) return;

            Checkpoints[id][p].BG2Y_Base = valueNew;
            SNES.edit = true;
        }
        private void wramIntInt_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue == null || SNES.rom == null || Level.Id >= Const.PlayableLevelsCount || supressInts)
                return;

            int id = Level.Id;
            int p = checkpointId;

            byte valueNew = (byte)(int)e.NewValue;

            if (Checkpoints[id][p].WramFlag == valueNew) return;

            Checkpoints[id][p].WramFlag = valueNew;
            SNES.edit = true;
        }
        private void megaFlipInt_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue == null || SNES.rom == null || Level.Id >= Const.PlayableLevelsCount || supressInts)
                return;

            int id = Level.Id;
            int p = checkpointId;

            byte valueNew = (byte)(int)e.NewValue;

            if (Checkpoints[id][p].MegaFlip == valueNew) return;

            Checkpoints[id][p].MegaFlip = valueNew;
            SNES.edit = true;
        }
        private void collisionInt_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue == null || SNES.rom == null || Level.Id >= Const.PlayableLevelsCount || supressInts)
                return;

            int id = Level.Id;
            int p = checkpointId;

            byte valueNew = (byte)(int)e.NewValue;

            if (Checkpoints[id][p].CollisionTimer == valueNew) return;

            Checkpoints[id][p].CollisionTimer = valueNew;
            SNES.edit = true;
        }
        private void EditCheckpointBtn_Click(object sender, RoutedEventArgs e) // Configure Max Checkpoints in the current stage
        {
            if (Level.Id >= Const.PlayableLevelsCount || (Const.Id == Const.GameId.MegaManX3 && Level.Id > 0xE))
            {
                MessageBox.Show("This level does not support checkpoints.", "ERROR");
                return;
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
            NumInt numInt = new NumInt() { Margin = new Thickness(5), Minimum = 1, Maximum = 0xFF, Value = spawnInt.Maximum + 1, Width = 100, FontFamily = new FontFamily("Consolas"), FontSize = 16 };
            Grid.SetColumn(numInt, 1);
            Grid.SetRow(numInt, 1);
            Button confirmBtn = new Button() { Content = "OK", Width = 75, Height = 30, Margin = new Thickness(5) };
            confirmBtn.Click += (se, ev) =>
            {
                if (numInt.Value == null)
                    return;

                int id = Level.Id;

                int total = 0;
                int amount = (int)numInt.Value;

                for (int i = 0; i < Checkpoints.Count; i++)
                {
                    if (i == id)
                        total += amount;
                    else
                        total += Checkpoints[i].Count;
                }

                if (total > Const.MaxTotalCheckpoints)
                {
                    MessageBox.Show($"The total amount of checkpoints across all levels cannot exceed {Const.MaxTotalCheckpoints}.\nCurrent Total: {total}", "ERROR");
                    return;
                }

                while (Checkpoints[id].Count < amount)
                    Checkpoints[id].Add(new Checkpoint());
                while (Checkpoints[id].Count > amount)
                    Checkpoints[id].RemoveAt(Checkpoints[id].Count - 1);
                SNES.edit = true;
                MessageBox.Show("Checkpoint configuration updated successfully!");
                SetSpawnSettings();
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
