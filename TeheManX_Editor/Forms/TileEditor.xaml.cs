using System;
using System.Windows;
using System.Windows.Controls;

namespace TeheManX_Editor.Forms
{
    /// <summary>
    /// Interaction logic for TileEditor.xaml
    /// </summary>
    public partial class TileEditor : UserControl
    {
        public TileEditor()
        {
            InitializeComponent();
        }
        public void AssignLimits()
        {
            if (Level.Id >= Const.PlayableLevelsCount)
            {
                // Disable UI
                tileSetInt.IsEnabled = false;
                return;
            }

            tileSetInt.IsEnabled = true;

            ushort[] offsets = new ushort[Const.PlayableLevelsCount];
            Buffer.BlockCopy(SNES.rom, Const.BackgroundTileInfoOffset, offsets, 0, offsets.Length * 2);
            ushort toFindOffset = offsets[Level.Id];
            int index = Array.IndexOf(offsets, toFindOffset);
            int maxBGTiles = ((offsets[index + 1] - toFindOffset) / 2) - 1;

            if (maxBGTiles > 0)
            {
                MainWindow.window.tileE.tileSetInt.Maximum = maxBGTiles;
                if (MainWindow.window.tileE.tileSetInt.Value > maxBGTiles)
                    MainWindow.window.tileE.tileSetInt.Value = maxBGTiles;
            }
            else
            {
                MainWindow.window.tileE.tileSetInt.Maximum = 0;
                MainWindow.window.tileE.tileSetInt.Value = 0;
            }
        }

        private void RedrawBtn_Click(object sender, RoutedEventArgs e)
        {
            if (Level.Id >= Const.PlayableLevelsCount || MainWindow.window.tileE.tileSetInt.Value == null)
                return;

            Level.TileSet = (int)MainWindow.window.tileE.tileSetInt.Value;

            if (freshCheck.IsChecked == true)
                Level.LoadLevelTiles();
            else
                Level.LoadDynamicBackgroundTiles();

            MainWindow.window.layoutE.DrawLayout();
            MainWindow.window.layoutE.DrawScreen();

            MainWindow.window.screenE.DrawScreen();
            MainWindow.window.screenE.DrawTiles();
            MainWindow.window.screenE.DrawTile();

            MainWindow.window.tile32E.DrawTiles();
            MainWindow.window.tile32E.Draw16xTiles();
            MainWindow.window.tile32E.DrawTile();

            MainWindow.window.tile16E.Draw16xTiles();
            MainWindow.window.tile16E.DrawVramTiles();

            MainWindow.window.paletteE.DrawPalette();
            MainWindow.window.paletteE.DrawVramTiles();
        }
    }
}
