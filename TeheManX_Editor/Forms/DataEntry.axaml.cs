using Avalonia.Controls;
using Avalonia.Interactivity;
using System;
using System.Collections.Generic;
using TeheManX_Editor.Forms;

namespace TeheManX_Editor;

public partial class DataEntry : UserControl
{
    #region Properties
    public int slotCount;
    List<ObjectSetting> stageObjectSettings;
    List<BGSetting> stageBGSettings;
    List<CameraTrigger> stageCameraTriggerSettings;
    List<BGPalette> stagePaletteSettings;
    Window window;
    #endregion

    #region Constructor
    public DataEntry(Window window, object listObj, int index)
    {
        InitializeComponent();
        this.window = window;

        Type type = listObj.GetType();

        if (typeof(List<ObjectSetting>) == type)
        {
            stageObjectSettings = (List<ObjectSetting>)listObj;
            int count = stageObjectSettings[index].Slots.Count;
            slotCountInt.Value = count;
            slotCount = count;
        }
        else if (typeof(List<BGSetting>) == type)
        {
            stageBGSettings = (List<BGSetting>)listObj;
            int count = stageBGSettings[index].Slots.Count;
            slotCountInt.Value = count;
            slotCount = count;
        }
        else if (typeof(List<CameraTrigger>) == type)
        {
            stageCameraTriggerSettings = (List<CameraTrigger>)listObj;
            int count = stageCameraTriggerSettings[index].BorderSettings.Count;
            slotCountInt.Value = count;
            slotCount = count;
            slotCountInt.Maximum = 4;
        }
        else
        {
            stagePaletteSettings = (List<BGPalette>)listObj;
            int count = stagePaletteSettings[index].Slots.Count;
            slotCountInt.Value = count;
            slotCount = count;
        }
    }
    #endregion Constructor

    #region Events
    private async void RemoveButton_Click(object sender, RoutedEventArgs e)
    {
        Avalonia.StyledElement? parent = this.Parent;
        StackPanel? items = parent as StackPanel;

        if (Const.Id == Const.GameId.MegaManX2 && Level.Id == 0xC && stageCameraTriggerSettings != null)
        {
            //Special Logic for Camera Triggers for X2
            
            /*
             * we must allow for no entries if you are in X2's re-fights.
             */
        }
        else //In any other situation dont allow 0 entries
        {
            if (items.Children.Count == 1)
            {
                await MessageBox.Show(window, "You must have atleast 1 entry in the data set");
                return;
            }
        }
        int index = items.Children.IndexOf(this);

        if (stageObjectSettings != null)
            stageObjectSettings.RemoveAt(index);
        else if (stageBGSettings != null)
            stageBGSettings.RemoveAt(index);
        else if (stageCameraTriggerSettings != null)
            stageCameraTriggerSettings.RemoveAt(index);
        else
            stagePaletteSettings.RemoveAt(index);

        items.Children.RemoveAt(index);
    }
    private void slotCountInt_ValueChanged(object sender, int e)
    {
        slotCount = e;
    }
    #endregion Events
}