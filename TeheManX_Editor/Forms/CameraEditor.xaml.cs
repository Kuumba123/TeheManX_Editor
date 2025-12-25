using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace TeheManX_Editor.Forms
{
    /// <summary>
    /// Interaction logic for CameraEditor.xaml
    /// </summary>
    public partial class CameraEditor : UserControl
    {
        #region Properties
        List<NumInt> borderInts = new List<NumInt>();
        public bool triggersEnabled;
        private bool suppressInts;
        #endregion Properties

        #region Fields
        private static bool raidoEnable;
        private static bool added;
        public static List<List<CameraTrigger>> CameraTriggers = new List<List<CameraTrigger>>();
        public static int[] CameraBorderSettings;

        private static int cameraTriggerId;
        private static int cameraBorderSettingId;
        #endregion Fields

        #region Constructors
        public CameraEditor()
        {
            suppressInts = true;
            raidoEnable = false;
            InitializeComponent();

            if (!added)
            {
                added = true;

                for (int i = 0; i < 4; i++)
                {
                    NumInt num = new NumInt();
                    num.Uid = i.ToString();
                    num.FontSize = 28;
                    num.Width = 95;
                    num.ButtonSpinnerWidth = 25;
                    num.Minimum = 0;
                    num.Maximum = 73;
                    num.ValueChanged += BorderSettingListInt_ValueChanged;
                    borderInts.Add(num);
                    borderPannel.Children.Add(num);
                }
            }
            suppressInts = false;
            raidoEnable = true;
        }
        #endregion Constructors

        #region Methods
        public void CollectData()
        {
            CameraBorderSettings = new int[Const.MaxTotalCameraSettings];
            Buffer.BlockCopy(SNES.rom, Const.CameraSettingsOffset, CameraBorderSettings, 0, Const.MaxTotalCameraSettings * 4);

            int cameraStages = Const.Id == Const.GameId.MegaManX3 ? 0xF : Const.PlayableLevelsCount;
            int[] maxAmount = new int[cameraStages];
            int[] shared = new int[cameraStages];
            GetMaxCameraTriggersFromRom(maxAmount, shared);
            CameraTriggers = CollectCameraTriggersFromRom(maxAmount, shared);

            suppressInts = true;
            raidoEnable = false;

            int max = Const.MaxTotalCameraSettings - 1;

            MainWindow.window.camE.borderSettingInt.Maximum = max;
            MainWindow.window.camE.borderSettingInt.Value = 0;
            cameraBorderSettingId = 0;

            for (int i = 0; i < 4; i++)
                borderInts[i].Maximum = max;

            ushort param = (ushort)(CameraBorderSettings[0]);
            ushort value = (ushort)(CameraBorderSettings[0] >> 16);

            if (param == Const.CameraBorderLeftWRAM)
                leftBtn.IsChecked = true;
            else if (param == Const.CameraBorderRightWRAM)
                rightBtn.IsChecked = true;
            else if (param == Const.CameraBorderTopWRAM)
                topBtn.IsChecked = true;
            else
                bottomBtn.IsChecked = true;

            MainWindow.window.camE.valueInt.Value = value;
            raidoEnable = true;
            suppressInts = false;
        }
        public void AssignTriggerLimits()
        {
            if (Level.Id >= Const.PlayableLevelsCount || (Const.Id == Const.GameId.MegaManX3 && Level.Id > 0xE) || CameraTriggers[Level.Id].Count == 0)
            {
                triggersEnabled = false;
                MainWindow.window.camE.triggerInt.IsEnabled = false;
                for (int i = 0; i < 4; i++)
                    MainWindow.window.camE.borderInts[i].IsEnabled = false;
                MainWindow.window.camE.rightInt.IsEnabled = false;
                MainWindow.window.camE.leftInt.IsEnabled = false;
                MainWindow.window.camE.bottomInt.IsEnabled = false;
                MainWindow.window.camE.topInt.IsEnabled = false;
                return;
            }
            suppressInts = true;
            int max = CameraTriggers[Level.Id].Count - 1;
            triggerInt.Maximum = max;
            MainWindow.window.camE.triggerInt.Value = 0;
            cameraTriggerId = 0;
            SetTriggerIntValues();
            suppressInts = false;
        }
        private void SetTriggerIntValues()
        {
            triggersEnabled = true;
            MainWindow.window.camE.triggerInt.IsEnabled = true;
            for (int i = 0; i < 4; i++)
                MainWindow.window.camE.borderInts[i].IsEnabled = true;

            int id = Level.Id;

            for (int i = 0; i < CameraTriggers[id][cameraTriggerId].BorderSettings.Count; i++)
            {
                //Get Camera Setting Ids
                borderInts[i].Value = CameraTriggers[id][cameraTriggerId].BorderSettings[i] - 1;
                borderInts[i].IsEnabled = true;
            }

            MainWindow.window.camE.rightInt.Value = CameraTriggers[id][cameraTriggerId].RightSide;
            MainWindow.window.camE.leftInt.Value = CameraTriggers[id][cameraTriggerId].LeftSide;
            MainWindow.window.camE.bottomInt.Value = CameraTriggers[id][cameraTriggerId].BottomSide;
            MainWindow.window.camE.topInt.Value = CameraTriggers[id][cameraTriggerId].TopSide;

            MainWindow.window.camE.rightInt.IsEnabled = true;
            MainWindow.window.camE.leftInt.IsEnabled = true;
            MainWindow.window.camE.bottomInt.IsEnabled = true;
            MainWindow.window.camE.topInt.IsEnabled = true;
        }
        public static byte[] CreateCameraTriggersData(List<List<CameraTrigger>> sourceSettings, int[] sharedList, int baseCpu)
        {
            baseCpu &= 0xFFFF;

            Dictionary<byte[], int> dict = new Dictionary<byte[], int>(ByteArrayComparer.Default);

            /*
             * Step 1. Create a dictionary of unique camera trigger settings data & keep track of stage keys
             */

            int nextKey = 0; //used as an offset into the camera trigger settings data table

            List<List<int>> keyList = new List<List<int>>(sourceSettings.Count);

            foreach (var innerList in sourceSettings)
                keyList.Add(Enumerable.Repeat(0, innerList.Count).ToList());


            for (int id = 0; id < sourceSettings.Count; id++)
            {
                if (sharedList[id] != -1)
                    continue;
                for (int s = 0; s < sourceSettings[id].Count; s++)
                {
                    byte[] slotsData = new byte[sourceSettings[id][s].BorderSettings.Count + 9];
                    BinaryPrimitives.WriteUInt16LittleEndian(slotsData.AsSpan(0), sourceSettings[id][s].RightSide);
                    BinaryPrimitives.WriteUInt16LittleEndian(slotsData.AsSpan(2), sourceSettings[id][s].LeftSide);
                    BinaryPrimitives.WriteUInt16LittleEndian(slotsData.AsSpan(4), sourceSettings[id][s].BottomSide);
                    BinaryPrimitives.WriteUInt16LittleEndian(slotsData.AsSpan(6), sourceSettings[id][s].TopSide);

                    if (slotsData.Length != 9)
                    {
                        for (int t = 0; t < sourceSettings[id][s].BorderSettings.Count; t++)
                            slotsData[8 + t] = sourceSettings[id][s].BorderSettings[t];
                    }
                    if (!dict.ContainsKey(slotsData))
                    {
                        dict.Add(slotsData, nextKey);
                        nextKey += slotsData.Length;
                    }
                    int value = dict[slotsData];
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

                if (sharedList[id] != -1)
                    continue;

                for (int s = 0; s < sourceSettings[id].Count; s++)
                    totalPointersLength += 2;
            }

            /*
            * Step 3. Create the byte array and setup the pointers
            */

            int stagePointersLength = sourceSettings.Count * 2;
            int nextOffset = stagePointersLength + baseCpu;

            byte[] exportData = new byte[nextKey + totalPointersLength];

            //Fix the stage pointers
            for (int i = 0; i < sourceSettings.Count; i++)
            {
                if (sharedList[i] == -1)
                {
                    BinaryPrimitives.WriteUInt16LittleEndian(exportData.AsSpan(i * 2), (ushort)nextOffset);
                    nextOffset += sourceSettings[i].Count * 2;
                }
                else
                {
                    ushort writeOffset = BinaryPrimitives.ReadUInt16LittleEndian(exportData.AsSpan(sharedList[i] * 2));
                    BinaryPrimitives.WriteUInt16LittleEndian(exportData.AsSpan(i * 2), writeOffset);
                }
            }
            //Fix the camera triggers setting pointers
            nextOffset = stagePointersLength;
            for (int i = 0; i < sourceSettings.Count; i++)
            {
                if (sharedList[i] != -1)
                    continue;

                for (int st = 0; st < sourceSettings[i].Count; st++)
                    BinaryPrimitives.WriteUInt16LittleEndian(exportData.AsSpan(nextOffset + st * 2), (ushort)(keyList[i][st] + totalPointersLength + baseCpu));
                nextOffset += sourceSettings[i].Count * 2;
            }

            /*
            * Step 4. Copy the unique camera trigger settings data
            */
            nextOffset = totalPointersLength;
            foreach (var kvp in dict)
            {
                kvp.Key.CopyTo(exportData.AsSpan(nextOffset));
                nextOffset += kvp.Key.Length;
            }
            return exportData;
        }
        public static void GetMaxCameraTriggersFromRom(int[] destAmount, int[] shared = null)
        {
            int cameraStages = Const.Id == Const.GameId.MegaManX3 ? 0xF : Const.PlayableLevelsCount;

            if (shared == null)
                shared = new int[cameraStages];

            for (int i = 0; i < cameraStages; i++)
                shared[i] = -1;

            ushort[] offsets = new ushort[cameraStages];
            ushort[] sortedOffsets = new ushort[cameraStages];
            Buffer.BlockCopy(SNES.rom, Const.CameraTriggersOffset, offsets, 0, cameraStages * 2);
            Array.Copy(offsets, sortedOffsets, cameraStages);
            Array.Sort(sortedOffsets);

            for (int i = 0; i < cameraStages; i++)
            {
                if (i == 0) continue;

                ushort stageOffset = offsets[i];

                for (int j = i; j != 0; j--)
                {
                    if (i == j) continue;
                    ushort currentOffset = offsets[j];
                    if (stageOffset == currentOffset)
                    {
                        shared[i] = j;
                        break;
                    }
                }
            }

            int[] maxAmounts = destAmount;

            int maxIndex = 0;
            for (int j = 0; j < offsets.Length; j++)
            {
                if (sortedOffsets[j] > sortedOffsets[maxIndex])
                    maxIndex = j;
            }

            for (int i = 0; i < cameraStages; i++)
            {
                if (shared[i] != -1)
                {
                    maxAmounts[i] = maxAmounts[shared[i]];
                    continue;
                }

                ushort toFindOffset = offsets[i];

                if (Array.IndexOf(sortedOffsets, toFindOffset) != maxIndex)
                {
                    int index = Array.IndexOf(sortedOffsets, toFindOffset);

                    while (sortedOffsets[index] == sortedOffsets[index + 1])
                        index++;
                    maxAmounts[i] = ((sortedOffsets[index + 1] - toFindOffset) / 2);
                }
                else //Last Stage
                {
                    int tempOffset = Const.CameraTriggersOffset + cameraStages * 2;
                    int endOffset = SNES.CpuToOffset(offsets[maxIndex], Const.CameraSettingsBank);

                    int lowestPointer = BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(tempOffset));

                    while (tempOffset != endOffset)
                    {
                        int addr = BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(tempOffset));

                        if (addr < lowestPointer)
                            lowestPointer = addr;
                        tempOffset += 2;
                    }
                    ushort currentOffset = offsets[i];
                    int max = ((lowestPointer - currentOffset) / 2);

                    for (int j = 0; j < max; j++)
                    {
                        if (BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(currentOffset + j * 2)) == 0)
                        {
                            max = j;
                            break;
                        }
                    }

                    maxAmounts[i] = max;
                }
            }
        }
        public static List<List<CameraTrigger>> CollectCameraTriggersFromRom(int[] destAmount, int[] shared)
        {
            List<List<CameraTrigger>> sourceSettings = new List<List<CameraTrigger>>();
            int cameraStages = Const.Id == Const.GameId.MegaManX3 ? 0xF : Const.PlayableLevelsCount;

            for (int i = 0; i < cameraStages; i++)
            {
                List<CameraTrigger> triggerSettings = new List<CameraTrigger>();
                for (int j = 0; j < destAmount[i]; j++)
                {
                    int listOffset = SNES.CpuToOffset(BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(Const.CameraTriggersOffset + i * 2)), Const.CameraSettingsBank);
                    int offset = SNES.CpuToOffset(BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(listOffset + j * 2)), Const.CameraSettingsBank);

                    CameraTrigger setting = new CameraTrigger();
                    setting.RightSide = BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(offset));
                    setting.LeftSide = BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(offset + 2));
                    setting.BottomSide = BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(offset + 4));
                    setting.TopSide = BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(offset + 6));
                    offset += 8;

                    while (true)
                    {
                        byte borderSetting = SNES.rom[offset];

                        if (borderSetting == 0)
                            break;
                        offset++;
                        setting.BorderSettings.Add(borderSetting);
                    }
                    triggerSettings.Add(setting);
                }
                sourceSettings.Add(triggerSettings);
            }
            return sourceSettings;
        }
        #endregion Methods

        #region Events
        private void triggerInt_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue == null || SNES.rom == null || suppressInts) return;
            cameraTriggerId = (int)e.NewValue;
            suppressInts = true;
            SetTriggerIntValues();
            suppressInts = false;
        }
        private void BorderSettingListInt_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e) //For Trigger Settings
        {
            if (e.NewValue == null || SNES.rom == null || suppressInts) return;
            
            int index = int.Parse(((NumInt)sender).Uid);
            byte valueNew = (byte)(int)e.NewValue;
            int id = Level.Id;

            if (CameraTriggers[id][cameraTriggerId].BorderSettings[index] == valueNew)
                return;

            CameraTriggers[id][cameraTriggerId].BorderSettings[index] = valueNew;
            SNES.edit = true;
        }
        private void SideInt_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e) //RLBT
        {
            if (e.NewValue == null || SNES.rom == null || suppressInts) return;

            int spec = int.Parse(((NumInt)sender).Uid);
            ushort valueNew = (ushort)(int)e.NewValue;
            int id = Level.Id;

            if (spec == 0)
            {
                if (CameraTriggers[id][cameraTriggerId].RightSide == valueNew) return;
                CameraTriggers[id][cameraTriggerId].RightSide = valueNew;
            }
            else if (spec == 2)
            {
                if (CameraTriggers[id][cameraTriggerId].LeftSide == valueNew) return;
                CameraTriggers[id][cameraTriggerId].LeftSide = valueNew;
            }
            else if (spec == 4)
            {
                if (CameraTriggers[id][cameraTriggerId].BottomSide == valueNew) return;
                CameraTriggers[id][cameraTriggerId].BottomSide = valueNew;
            }
            else
            {
                if (CameraTriggers[id][cameraTriggerId].TopSide == valueNew) return;
                CameraTriggers[id][cameraTriggerId].TopSide = valueNew;
            }
            SNES.edit = true;
        }
        private void borderSettingInt_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue == null || SNES.rom == null || suppressInts) return;

            cameraBorderSettingId = (int)e.NewValue;

            ushort param = (ushort)(CameraBorderSettings[cameraBorderSettingId]);
            ushort value = (ushort)(CameraBorderSettings[cameraBorderSettingId] >> 16);

            raidoEnable = false;

            if (param == Const.CameraBorderLeftWRAM)
                leftBtn.IsChecked = true;
            else if (param == Const.CameraBorderRightWRAM) 
                rightBtn.IsChecked = true;
            else if (param == Const.CameraBorderTopWRAM)
                topBtn.IsChecked = true;
            else
                bottomBtn.IsChecked = true;

            raidoEnable = true;
            MainWindow.window.camE.valueInt.Value = value;
        }
        private void valueInt_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue == null || SNES.rom == null || suppressInts) return;

            ushort param = (ushort)(CameraBorderSettings[cameraBorderSettingId]);
            ushort value = (ushort)(CameraBorderSettings[cameraBorderSettingId] >> 16);

            ushort valueNew = (ushort)(int)e.NewValue;

            if (valueNew == value) return;

            CameraBorderSettings[cameraBorderSettingId] = param | (valueNew << 16);
            SNES.edit = true;
        }
        private void RadioBtn_Click(object sender, RoutedEventArgs e)
        {
            if (!raidoEnable) return;

            ushort value = (ushort)(CameraBorderSettings[cameraBorderSettingId] >> 16);

            ushort setting = Convert.ToUInt16(((RadioButton)sender).Uid); //New Setting

            ushort param = setting == 0 ? Const.CameraBorderLeftWRAM : setting == 1 ? Const.CameraBorderRightWRAM : setting == 2 ? Const.CameraBorderTopWRAM : Const.CameraBorderBottomWRAM;

            if ((CameraBorderSettings[cameraBorderSettingId] & 0xFFFF) == param) return;

            CameraBorderSettings[cameraBorderSettingId] = param | (value << 16);
            SNES.edit = true;
        }
        #endregion Events
    }
}
