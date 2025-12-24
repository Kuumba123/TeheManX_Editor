using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace TeheManX_Editor.Forms
{
    /// <summary>
    /// Interaction logic for DataEntry.xaml
    /// </summary>
    public partial class DataEntry : UserControl
    {
        #region Properties
        public int slotCount;
        List<ObjectSetting> stageObjectSettings;
        List<BGSetting> stageBGSettings;
        #endregion

        #region Constructors
        public DataEntry(object listObj, int index)
        {
            InitializeComponent();

            if (typeof(List<ObjectSetting>) == listObj.GetType())
            {
                stageObjectSettings = (List<ObjectSetting>)listObj;
                int count = stageObjectSettings[index].Slots.Count;
                slotCountInt.Value = count;
                slotCount = count;
            }
            else if (typeof(List<BGSetting>) == listObj.GetType())
            {
                stageBGSettings = (List<BGSetting>)listObj;
                int count = stageBGSettings[index].Slots.Count;
                slotCountInt.Value = count;
                slotCount = count;
            }
        }
        #endregion Constructors

        #region Events
        private void RemoveButton_Click(object sender, RoutedEventArgs e)
        {
            var parent = VisualTreeHelper.GetParent(this);
            var items = parent as StackPanel;

            int index = items.Children.IndexOf(this);

            if (stageObjectSettings != null)
                stageObjectSettings.RemoveAt(index);
            else if (stageBGSettings != null)
                stageBGSettings.RemoveAt(index);

            items.Children.RemoveAt(index);
        }
        private void slotCountInt_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue == null) return;
            slotCount = (int)e.NewValue;
        }
        #endregion Events
    }
}
