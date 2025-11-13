using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace TeheManX_Editor.Forms
{
    /// <summary>
    /// Interaction logic for GameSettings.xaml
    /// </summary>
    public partial class GameSettings : Window
    {
        #region Properties
        private bool enable = false;
        private bool saved = true;
        private BinaryWriter bw;
        private BinaryReader br;
        private MemoryStream ms;
        private string path;
        #endregion Properties

        #region Constructors
        public GameSettings(string path)
        {
            ms = new MemoryStream(File.ReadAllBytes(path));
            bw = new BinaryWriter(ms);
            br = new BinaryReader(ms);
            this.path = path;
            InitializeComponent();
            //Setup Controls
            foreach (var c in mainPannel.Children)
            {
                if (c.GetType() != typeof(Expander))
                    continue;
                foreach (var c2 in ((StackPanel)((Expander)c).Content).Children)
                {
                    foreach (var c3 in ((StackPanel)c2).Children)
                    {
                        if (c3.GetType() != typeof(Xceed.Wpf.Toolkit.IntegerUpDown))
                            continue;
                        Xceed.Wpf.Toolkit.IntegerUpDown t = (Xceed.Wpf.Toolkit.IntegerUpDown)c3;
                        if (t.Uid.Trim() == "")
                            break;
                        var words = t.Uid.Split();
                        int addr = Convert.ToInt32(words[0], 16);
                        int type = 0;
                        if (words.Length != 1)
                            type = Convert.ToInt32(words[1]);
                        switch (type)
                        {
                            case 1:
                                br.BaseStream.Position = SNES.CpuToOffset(addr);
                                t.Value = br.ReadByte();
                                break;
                            case 2:
                                br.BaseStream.Position = SNES.CpuToOffset(addr);
                                t.Value = br.ReadUInt16();
                                break;
                            default:
                                bw.BaseStream.Position = SNES.CpuToOffset(addr);
                                t.Value = br.Read();
                                break;
                        }

                    }
                }
            }
            enable = true;
        }
        #endregion Constructors

        #region Events
        private void SaveBtn_Click(object sender, RoutedEventArgs e)
        {
            if (saved && File.Exists(path))
                return;
            try
            {
                var fs = File.Create(path);
                ms.WriteTo(fs);
                fs.Close();
                saved = true;
                MessageBox.Show("Parameters Saved!");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "ERROR");
                throw;
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (saved)
                return;
            var result = MessageBox.Show("You edited certain Parameters without saving.\nAre you sure you wanna quit out without saving?", "WARNING", MessageBoxButton.YesNo);
            if (result != MessageBoxResult.Yes)
                e.Cancel = true;
            else
                return;
        }
        #endregion Events
    }
}
