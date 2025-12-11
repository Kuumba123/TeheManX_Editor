using System;
using System.Buffers.Binary;
using System.Collections.Generic;
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
        #endregion Properties

        #region Fields
        private static bool raidoEnable;
        private static bool added;
        #endregion Fields

        #region Constructors
        public CameraEditor()
        {
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
        }
        #endregion Constructors

        #region Methods
        public void AssignBorderSettingsLimits()
        {
            raidoEnable = false;
            int max = Const.MaxTotalCameraSettings - 1;

            MainWindow.window.camE.borderSettingInt.Maximum = max;

            if (MainWindow.window.camE.borderSettingInt.Value > max)
                MainWindow.window.camE.borderSettingInt.Value = max;

            for (int i = 0; i < 4; i++)
                borderInts[i].Maximum = max;

            int offset = Const.CameraSettingsOffset + (int)MainWindow.window.camE.borderSettingInt.Value * 4;

            ushort param = BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(offset + 0));
            ushort value = BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(offset + 2));

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
        public void AssignTriggerLimits()
        {
            if (Level.Id >= Const.PlayableLevelsCount || (Const.Id == Const.GameId.MegaManX3 && Level.Id > 0xE) || BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(Const.CameraTriggersOffset + Level.Id * 2)) == 0)
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

            //Calculate the amount of Camera Triggers there are in the Stage
            int maxTriggers = 0;
            int maxLevels = (Const.Id == Const.GameId.MegaManX3) ? 0xF : Const.PlayableLevelsCount;

            ushort[] offsetList = new ushort[maxLevels];
            for (int i = 0; i < maxLevels; i++)
                offsetList[i] = BitConverter.ToUInt16(SNES.rom, Const.CameraTriggersOffset + i * 2);

            int maxIndex = 0;
            for (int i = 0; i < offsetList.Length; i++)
            {
                if (offsetList[i] > offsetList[maxIndex])
                    maxIndex = i;
            }

            int tempOffset = Const.CameraTriggersOffset + maxLevels * 2;
            int endOffset = SNES.CpuToOffset(offsetList[maxIndex], Const.CameraSettingsBank);

            int lowestPointer = BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(tempOffset));

            while (tempOffset != endOffset)
            {
                int addr = BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(tempOffset));

                if (addr < lowestPointer)
                    lowestPointer = addr;
                tempOffset += 2;
            }

            int introFirstOffset = BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(Const.CameraTriggersOffset)); //just for X2...
            ushort currentOffset = BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(Const.CameraTriggersOffset + Level.Id * 2));
            int currentIndex = Array.IndexOf(offsetList, currentOffset);

            if (Level.Id == 0xC && Const.Id == Const.GameId.MegaManX2 && BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(SNES.CpuToOffset(introFirstOffset, Const.CameraSettingsBank))) == currentOffset)
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
            else if (currentIndex == maxIndex)
                maxTriggers = ((lowestPointer - currentOffset) / 2) - 1;
            else
                maxTriggers = ((offsetList[currentIndex + 1] - currentOffset) / 2) - 1;

            
            if (MainWindow.window.camE.triggerInt.Value > maxTriggers)
                MainWindow.window.camE.triggerInt.Value = maxTriggers;
            MainWindow.window.camE.triggerInt.Maximum = maxTriggers;

            int listOffset = BitConverter.ToUInt16(SNES.rom, Const.CameraTriggersOffset + Level.Id * 2);
            int offset = SNES.CpuToOffset(BitConverter.ToUInt16(SNES.rom, SNES.CpuToOffset(listOffset + (int)MainWindow.window.camE.triggerInt.Value * 2, Const.CameraSettingsBank)), Const.CameraSettingsBank);
            SetTriggerIntValues(offset);
        }
        private void SetTriggerIntValues(int offset)
        {
            triggersEnabled = true;
            MainWindow.window.camE.triggerInt.IsEnabled = true;

            for (int i = 0; i < 4; i++)
            {
                if (SNES.rom[offset + 8 + i] == 0)
                {
                    while (i < 4)
                    {
                        borderInts[i].IsEnabled = false;
                        i++;
                    }
                    break;
                }
                //Get Camera Setting Ids
                borderInts[i].Value = SNES.rom[offset + 8 + i] - 1;
                borderInts[i].IsEnabled = true;
            }

            MainWindow.window.camE.rightInt.Value = BitConverter.ToUInt16(SNES.rom, offset);
            MainWindow.window.camE.leftInt.Value = BitConverter.ToUInt16(SNES.rom, offset + 2);
            MainWindow.window.camE.bottomInt.Value = BitConverter.ToUInt16(SNES.rom, offset + 4);
            MainWindow.window.camE.topInt.Value = BitConverter.ToUInt16(SNES.rom, offset + 6);

            MainWindow.window.camE.rightInt.IsEnabled = true;
            MainWindow.window.camE.leftInt.IsEnabled = true;
            MainWindow.window.camE.bottomInt.IsEnabled = true;
            MainWindow.window.camE.topInt.IsEnabled = true;
        }
        #endregion Methods

        #region Events
        private void triggerInt_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue == null || SNES.rom == null) return;

            int listOffset = BitConverter.ToUInt16(SNES.rom, Const.CameraTriggersOffset + Level.Id * 2);
            int offset = SNES.CpuToOffset(BitConverter.ToUInt16(SNES.rom, SNES.CpuToOffset(listOffset + (int)MainWindow.window.camE.triggerInt.Value * 2, Const.CameraSettingsBank)), Const.CameraSettingsBank);
            SetTriggerIntValues(offset);
        }
        private void BorderSettingListInt_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e) //For Trigger Settings
        {
            if (e.NewValue == null || SNES.rom == null) return;

            int listOffset = BitConverter.ToUInt16(SNES.rom, Const.CameraTriggersOffset + Level.Id * 2);
            int offset = SNES.CpuToOffset(BitConverter.ToUInt16(SNES.rom, SNES.CpuToOffset(listOffset + (int)MainWindow.window.camE.triggerInt.Value * 2, Const.CameraSettingsBank)), Const.CameraSettingsBank);

            offset += int.Parse(((NumInt)sender).Uid) + 8;

            byte value = SNES.rom[offset];

            if (value == (byte)(int)e.NewValue + 1) return;

            SNES.rom[offset] = value;
            SNES.edit = true;
        }
        private void SideInt_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e) //RLBT
        {
            if (e.NewValue == null || SNES.rom == null) return;

            int listOffset = BitConverter.ToUInt16(SNES.rom, Const.CameraTriggersOffset + Level.Id * 2);
            int offset = SNES.CpuToOffset(BitConverter.ToUInt16(SNES.rom, SNES.CpuToOffset(listOffset + (int)MainWindow.window.camE.triggerInt.Value * 2, Const.CameraSettingsBank)), Const.CameraSettingsBank);

            offset += int.Parse(((NumInt)sender).Uid);

            ushort value = BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(offset));

            if (value == (ushort)(int)e.NewValue) return;

            BinaryPrimitives.WriteUInt16LittleEndian(SNES.rom.AsSpan(offset), (ushort)(int)e.NewValue);
            SNES.edit = true;
            MainWindow.window.enemyE.UpdateEnemyLabelPositions();
        }
        private void borderSettingInt_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue == null || SNES.rom == null) return;

            ushort param = BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(Const.CameraSettingsOffset + (int)e.NewValue * 4 + 0));
            ushort value = BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(Const.CameraSettingsOffset + (int)e.NewValue * 4 + 2));

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
            if (e.NewValue == null || SNES.rom == null) return;

            int offset = Const.CameraSettingsOffset + (int)MainWindow.window.camE.borderSettingInt.Value * 4;

            ushort param = BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(offset + 0));
            ushort value = BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(offset + 2));

            if ((ushort)(int)e.NewValue == value) return;

            BinaryPrimitives.WriteUInt16LittleEndian(SNES.rom.AsSpan(offset + 2), (ushort)(int)e.NewValue);
            SNES.edit = true;
        }
        private void RadioBtn_Click(object sender, RoutedEventArgs e)
        {
            if (!raidoEnable) return;

            int offset = Const.CameraSettingsOffset + (int)MainWindow.window.camE.borderSettingInt.Value * 4;

            ushort param = BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(offset + 0));

            ushort setting = Convert.ToUInt16(((RadioButton)sender).Uid); //New Setting

            int wram = setting == 0 ? Const.CameraBorderLeftWRAM : setting == 1 ? Const.CameraBorderRightWRAM : setting == 2 ? Const.CameraBorderTopWRAM : Const.CameraBorderBottomWRAM;

            BinaryPrimitives.WriteUInt16LittleEndian(SNES.rom.AsSpan(offset + 0), (ushort)wram);
            SNES.edit = true;
        }
        #endregion Events
    }
}
