using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;

namespace TeheManX_Editor.Forms;

public partial class CameraEditor : UserControl
{
    #region Properties
    public List<NumInt> borderInts = new List<NumInt>();
    public List<Button> manualButtons = new List<Button>();
    public bool triggersEnabled;
    public bool suppressInts;
    #endregion Properties

    #region Fields
    private static bool raidoEnable;
    private static bool added;
    public static List<List<CameraTrigger>> CameraTriggers = new List<List<CameraTrigger>>();
    public static int[] CameraBorderSettings;

    public static int cameraTriggerId;
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
                num.Tag = i.ToString();
                num.FontSize = 28;
                num.Width = 95;
                num.SpinnerButtonWidth = 25;
                num.Minimum = 0;
                num.Maximum = 73;
                num.ValueChanged += BorderSettingListInt_ValueChanged;
                borderInts.Add(num);

                Button button = new Button();
                button.Tag = i;
                button.FontSize = 18;
                button.Width = 167;
                button.Content = "Manual Border";
                button.Click += ManualButton_Click;
                manualButtons.Add(button);

                StackPanel innerPannel = new StackPanel() { Orientation = Avalonia.Layout.Orientation.Horizontal};
                innerPannel.Children.Add(num);
                innerPannel.Children.Add(button);

                borderPannel.Children.Add(innerPannel);
            }
        }
        suppressInts = false;
        raidoEnable = true;
    }
    #endregion Constructors

    #region Methods
    public void CollectData()
    {
        if (Level.Project.CameraTriggers == null)
        {
            CameraBorderSettings = new int[Const.MaxTotalCameraSettings];
            Buffer.BlockCopy(SNES.rom, Const.CameraSettingsOffset, CameraBorderSettings, 0, Const.MaxTotalCameraSettings * 4);

            int cameraStages = Const.Id == Const.GameId.MegaManX3 ? 0xF : Const.PlayableLevelsCount;
            int[] maxAmount = new int[cameraStages];
            int[] shared = new int[cameraStages];
            GetMaxCameraTriggersFromRom(maxAmount, shared);
            CameraTriggers = CollectCameraTriggersFromRom(maxAmount, shared);
        }
        else
        {
            CameraBorderSettings = Level.Project.CameraBorderSettings;
            CameraTriggers = Level.Project.CameraTriggers;
        }

        suppressInts = true;
        raidoEnable = false;

        int max = Level.Project.CameraTriggers != null ? 254 : Const.MaxTotalCameraSettings - 1;

        borderSettingInt.Maximum = max;
        borderSettingInt.Value = 0;
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

        valueInt.Value = value;
        raidoEnable = true;
        suppressInts = false;
    }
    public void AssignTriggerLimits()
    {
        if (Level.Id >= Const.PlayableLevelsCount || (Const.Id == Const.GameId.MegaManX3 && Level.Id > 0xE) || CameraTriggers[Level.Id].Count == 0)
        {
            triggersEnabled = false;
            triggerInt.IsEnabled = false;
            for (int i = 0; i < 4; i++)
            {
                borderInts[i].IsEnabled = false;
                manualButtons[i].IsEnabled = false;
            }
            rightInt.IsEnabled = false;
            leftInt.IsEnabled = false;
            bottomInt.IsEnabled = false;
            topInt.IsEnabled = false;
            return;
        }
        suppressInts = true;

        int max = CameraTriggers[Level.Id].Count - 1;
        triggerInt.Maximum = max;
        if (triggerInt.Value > max)
            triggerInt.Value = max;

        cameraTriggerId = triggerInt.Value;
        SetTriggerIntValues();
        suppressInts = false;
    }
    public void SetTriggerIntValues()
    {
        triggersEnabled = true;
        triggerInt.IsEnabled = true;
        for (int i = 0; i < 4; i++)
            borderInts[i].IsEnabled = false;
        for (int i = 0; i < 4; i++)
            manualButtons[i].IsEnabled = false;

        int id = Level.Id;

        for (int i = 0; i < CameraTriggers[id][cameraTriggerId].BorderSettings.Count; i++)
        {
            //Get Camera Setting Ids
            borderInts[i].Value = CameraTriggers[id][cameraTriggerId].BorderSettings[i] - 1;
            borderInts[i].IsEnabled = true;
            manualButtons[i].IsEnabled = true;
        }

        rightInt.Value = CameraTriggers[id][cameraTriggerId].RightSide;
        leftInt.Value = CameraTriggers[id][cameraTriggerId].LeftSide;
        bottomInt.Value = CameraTriggers[id][cameraTriggerId].BottomSide;
        topInt.Value = CameraTriggers[id][cameraTriggerId].TopSide;

        rightInt.IsEnabled = true;
        leftInt.IsEnabled = true;
        bottomInt.IsEnabled = true;
        topInt.IsEnabled = true;
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
    private void triggerInt_ValueChanged(object sender, int e)
    {
        if (SNES.rom == null || suppressInts) return;
        cameraTriggerId = e;
        suppressInts = true;
        SetTriggerIntValues();
        suppressInts = false;
    }
    private void BorderSettingListInt_ValueChanged(object sender, int e) //For Trigger Settings
    {
        if (SNES.rom == null || suppressInts) return;

        int index = int.Parse((string)((Control)sender).Tag);
        byte valueNew = (byte)(e + 1);
        int id = Level.Id;

        if (CameraTriggers[id][cameraTriggerId].BorderSettings[index] == valueNew)
            return;

        CameraTriggers[id][cameraTriggerId].BorderSettings[index] = valueNew;
        SNES.edit = true;
    }
    private async void ManualButton_Click(object sender, EventArgs e)
    {
        int borderListIndex = (int)(((Control)sender).Tag);

        Window window = new Window() { Title = "Border" , CanResize = false , SizeToContent = SizeToContent.WidthAndHeight , WindowStartupLocation = WindowStartupLocation.CenterScreen};

        TextBlock borderBlock = new TextBlock() { Text = "New Value:" , FontSize = 26 };

        NumInt borderValueInt = new NumInt();
        borderValueInt.Minimum = 0;
        borderValueInt.Maximum = 0x1FFF;
        borderValueInt.FontSize = 28;
        borderValueInt.Width = 102;
        borderValueInt.SpinnerButtonWidth = 25;

        StackPanel borderPanel = new StackPanel() { Orientation = Avalonia.Layout.Orientation.Horizontal};
        borderPanel.Children.Add(borderBlock);
        borderPanel.Children.Add(borderValueInt);

        RadioButton[] raidoButtons = new RadioButton[4];
        for (int i = 0; i < 4; i++)
            raidoButtons[i] = new RadioButton();

        raidoButtons[0].Content = "Left Border Change";
        raidoButtons[1].Content = "Right Border Change";
        raidoButtons[2].Content = "Top Border Change";
        raidoButtons[3].Content = "Bottom Border Change";

        int rawBorderSetting = CameraBorderSettings[CameraTriggers[Level.Id][triggerInt.Value].BorderSettings[borderListIndex] - 1];
        borderValueInt.Value = rawBorderSetting >> 16;
        int wramAddr = rawBorderSetting & 0xFFFF;

        if (wramAddr == Const.CameraBorderLeftWRAM)
            raidoButtons[0].IsChecked = true;
        else if (wramAddr == Const.CameraBorderRightWRAM)
            raidoButtons[1].IsChecked = true;
        else if (wramAddr == Const.CameraBorderTopWRAM)
            raidoButtons[2].IsChecked = true;
        else
            raidoButtons[3].IsChecked = true;

        StackPanel radioPanel = new StackPanel();

        for (int i = 0; i < 4; i++)
            radioPanel.Children.Add(raidoButtons[i]);

        Button confirmButton = new Button() { Content = "Confirm"};
        confirmButton.Click += async (s, e) =>
        {
            int id = Level.Id;
            int cameraStages = Const.Id == Const.GameId.MegaManX3 ? 0xF : Const.PlayableLevelsCount;

            ushort param = raidoButtons[0].IsChecked == true ? Const.CameraBorderLeftWRAM : raidoButtons[1].IsChecked == true ? Const.CameraBorderRightWRAM : raidoButtons[2].IsChecked == true ? Const.CameraBorderTopWRAM : Const.CameraBorderBottomWRAM;
            int createdSetting = param | (borderValueInt.Value << 16);

            //Step 1. Check/Collect all the unique Border Settings that are actually being used!
            HashSet<int> usedBorderSettings = new HashSet<int>();
            for (int stage = 0; stage < cameraStages; stage++)
            {
                for (int t = 0; t < CameraTriggers[stage].Count; t++)
                {
                    for (int i = 0; i < CameraTriggers[stage][t].BorderSettings.Count; i++)
                    {
                        int value;
                        if (stage == id && t == triggerInt.Value && i == borderListIndex)
                            value = createdSetting;
                        else
                            value = CameraBorderSettings[CameraTriggers[stage][t].BorderSettings[i] - 1];

                        usedBorderSettings.Add(value);
                    }
                }
            }

            if (usedBorderSettings.Count > CameraBorderSettings.Length) //Size Check
            {
                await MessageBox.Show(window, $"Max amount of Border Settings is {CameraBorderSettings.Length} vs {usedBorderSettings.Count:X}");
                return;
            }

            //Step 2. Create the New Border Data an re assign the values
            int[] createdBorderSettings = usedBorderSettings.ToArray();

            if (createdBorderSettings.Length != CameraBorderSettings.Length) //Increase Size in case we used less than the orignal
            {
                Array.Resize(ref createdBorderSettings, CameraBorderSettings.Length);
                //Fix up new Empty Entries
                for (int i = 0; i < createdBorderSettings.Length; i++)
                {
                    if (createdBorderSettings[i] != 0) continue;
                    createdBorderSettings[i] = Const.CameraBorderLeftWRAM;
                }
            }

            for (int stage = 0; stage < cameraStages; stage++)
            {
                for (int t = 0; t < CameraTriggers[stage].Count; t++)
                {
                    for (int i = 0; i < CameraTriggers[stage][t].BorderSettings.Count; i++)
                    {
                        int value;
                        if (stage == id && t == triggerInt.Value && i == borderListIndex)
                            value = createdSetting;
                        else
                            value = CameraBorderSettings[CameraTriggers[stage][t].BorderSettings[i] - 1];

                        int index = Array.IndexOf(createdBorderSettings, value);
                        CameraTriggers[stage][t].BorderSettings[i] = (byte)(index + 1);
                    }
                }
            }
            CameraBorderSettings = createdBorderSettings;
            SNES.edit = true;

            //Step 3. Update the UI
            suppressInts = true;
            ushort settingVal = (ushort)(CameraBorderSettings[borderSettingInt.Value]);
            ushort paramVal = (ushort)(CameraBorderSettings[borderSettingInt.Value] >> 16);

            if (settingVal == Const.CameraBorderLeftWRAM)
                leftBtn.IsChecked = true;
            else if (settingVal == Const.CameraBorderRightWRAM)
                rightBtn.IsChecked = true;
            else if (settingVal == Const.CameraBorderTopWRAM)
                topBtn.IsChecked = true;
            else
                bottomBtn.IsChecked = true;
            valueInt.Value = paramVal;
            SetTriggerIntValues();
            suppressInts = false;

            //Done
            await MessageBox.Show(window, "Border Settings Updated!");
            window.Close();
        };

        StackPanel mainPannel = new StackPanel();
        mainPannel.Children.Add(borderPanel);
        mainPannel.Children.Add(radioPanel);
        mainPannel.Children.Add(confirmButton);

        window.Content = mainPannel;
        await window.ShowDialog(MainWindow.window);
    }
    private void SideInt_ValueChanged(object sender, int e) //RLBT
    {
        if (SNES.rom == null || suppressInts) return;

        int spec = int.Parse((string)((Control)sender).Tag);
        ushort valueNew = (ushort)e;
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
        MainWindow.window.enemyE.DrawLayout();
    }
    private void borderSettingInt_ValueChanged(object sender, int e)
    {
        if (SNES.rom == null || suppressInts) return;

        cameraBorderSettingId = e;

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
        valueInt.Value = value;
    }
    private void valueInt_ValueChanged(object sender, int e)
    {
        if (SNES.rom == null || suppressInts) return;

        ushort param = (ushort)(CameraBorderSettings[cameraBorderSettingId]);
        ushort value = (ushort)(CameraBorderSettings[cameraBorderSettingId] >> 16);

        ushort valueNew = (ushort)e;

        if (valueNew == value) return;

        CameraBorderSettings[cameraBorderSettingId] = param | (valueNew << 16);
        SNES.edit = true;
    }
    private void RadioBtn_Click(object sender, RoutedEventArgs e)
    {
        if (!raidoEnable) return;

        ushort value = (ushort)(CameraBorderSettings[cameraBorderSettingId] >> 16);

        ushort setting = Convert.ToUInt16((string)(((RadioButton)sender).Tag)); //New Setting

        ushort param = setting == 0 ? Const.CameraBorderLeftWRAM : setting == 1 ? Const.CameraBorderRightWRAM : setting == 2 ? Const.CameraBorderTopWRAM : Const.CameraBorderBottomWRAM;

        if ((CameraBorderSettings[cameraBorderSettingId] & 0xFFFF) == param) return;

        CameraBorderSettings[cameraBorderSettingId] = param | (value << 16);
        SNES.edit = true;
    }
    private async void EditTriggerCountBtn_Click(object sender, RoutedEventArgs e)
    {
        if (SNES.rom == null || Level.Id >= Const.PlayableLevelsCount || (Const.Id == Const.GameId.MegaManX3 && Level.Id > 0xE))
            return;

        List<CameraTrigger> trueCopy = CameraTriggers[Level.Id].Select(os => new CameraTrigger(os)).ToList();

        Window window = new Window() { WindowStartupLocation = WindowStartupLocation.CenterScreen, Title = "Camera Triggers Settings"};
        window.Width = 310;
        window.MinWidth = 310;
        window.MaxWidth = 310;
        window.Height = 760;

        StackPanel stackPanel = new StackPanel();

        for (int i = 0; i < trueCopy.Count; i++)
        {
            DataEntry entry = new DataEntry(window, trueCopy, i);
            stackPanel.Children.Add(entry);
        }

        ScrollViewer scrollViewer = new ScrollViewer() { AllowAutoHide = false };
        scrollViewer.Content = stackPanel;

        Button confirmBtn = new Button() { Content = "Confirm" };
        confirmBtn.Click += async (s, ev) =>
        {
            for (int i = 0; i < trueCopy.Count; i++)
            {
                int neededSlots = ((DataEntry)(stackPanel.Children[i])).slotCount;

                while (trueCopy[i].BorderSettings.Count < neededSlots)
                    trueCopy[i].BorderSettings.Add(1);

                while (trueCopy[i].BorderSettings.Count > neededSlots)
                    trueCopy[i].BorderSettings.RemoveAt(trueCopy[i].BorderSettings.Count - 1);
            }

            List<CameraTrigger> uneditedList = CameraTriggers[Level.Id];
            CameraTriggers[Level.Id] = trueCopy;

            int cameraStages = Const.Id == Const.GameId.MegaManX3 ? 0xF : Const.PlayableLevelsCount;

            int[] maxAmount = new int[cameraStages];
            int[] shared = new int[cameraStages];

            if (Level.Project.CameraTriggers != null) //no stages share data when using json
            {
                for (int i = 0; i < cameraStages; i++)
                    shared[i] = -1;
            }
            else
                GetMaxCameraTriggersFromRom(maxAmount, shared);

            int length = CreateCameraTriggersData(CameraTriggers, shared, 0).Length;

            if (length > Const.CameraTriggersLength && Level.Project.CameraTriggers == null)
            {
                CameraTriggers[Level.Id] = uneditedList;
                await MessageBox.Show(MainWindow.window,$"The new Camera Triggers length exceeds the maximum allowed space in the ROM (0x{length:X} vs max of 0x{Const.CameraTriggersLength:X}). Please lower some counts for this or another stage.");
                return;
            }

            AssignTriggerLimits();
            MainWindow.window.enemyE.DrawLayout();
            SNES.edit = true;
            await MessageBox.Show(MainWindow.window,"Camera Trigger counts updated!");
            window.Close();
        };
        Grid.SetRow(confirmBtn, 2);

        Button addBtn = new Button() { Content = "Add Setting" };
        addBtn.Click += (s, e) =>
        {
            int newIndex = trueCopy.Count;
            CameraTrigger camTrigger = new CameraTrigger();
            camTrigger.BorderSettings.Add(1);

            trueCopy.Add(camTrigger);

            DataEntry entry = new DataEntry(window, trueCopy, newIndex);
            stackPanel.Children.Add(entry);
        };
        Grid.SetRow(addBtn, 1);

        Grid grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition());
        grid.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });
        grid.Children.Add(scrollViewer);
        grid.Children.Add(confirmBtn);
        grid.Children.Add(addBtn);
        grid.Background = Brushes.Black;
        window.Content = grid;
        await window.ShowDialog(MainWindow.window);
    }
    #endregion Events
}