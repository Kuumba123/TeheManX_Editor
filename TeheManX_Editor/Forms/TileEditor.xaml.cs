using System;
using System.Buffers.Binary;
using System.Windows;
using System.Windows.Controls;

namespace TeheManX_Editor.Forms
{
    /// <summary>
    /// Interaction logic for TileEditor.xaml
    /// </summary>
    public partial class TileEditor : UserControl
    {
        #region Properties
        private bool _suppressBgSrcBoxTextChanged;
        #endregion Properties

        #region Constructor
        public TileEditor()
        {
            InitializeComponent();
        }
        #endregion Constructor

        #region Methods
        public void AssignLimits()
        {
            if (Level.Id >= Const.PlayableLevelsCount)
            {
                // Disable UI
                MainWindow.window.tileE.bgTileSetInt.IsEnabled = false;
                MainWindow.window.tileE.bgLengthInt.IsEnabled = false;
                MainWindow.window.tileE.bgAddressInt.IsEnabled = false;
                MainWindow.window.tileE.bgSrcBox.IsEnabled = false;
                MainWindow.window.tileE.bgPalInt.IsEnabled = false;
                return;
            }

            // Take of Background Tile UI
            MainWindow.window.tileE.bgTileSetInt.IsEnabled = true;


            //Get Max Amount of BG Tile Settings
            ushort[] offsets = new ushort[Const.PlayableLevelsCount];
            Buffer.BlockCopy(SNES.rom, Const.BackgroundTileInfoOffset, offsets, 0, offsets.Length * 2);
            ushort toFindOffset = offsets[Level.Id];
            int index = Array.IndexOf(offsets, toFindOffset);
            int maxBGTiles = ((offsets[index + 1] - toFindOffset) / 2) - 1;

            if (maxBGTiles >= 0)
            {
                MainWindow.window.tileE.bgTileSetInt.Maximum = maxBGTiles;
                if (MainWindow.window.tileE.bgTileSetInt.Value > maxBGTiles)
                    MainWindow.window.tileE.bgTileSetInt.Value = maxBGTiles;
            }
            else
            {
                MainWindow.window.tileE.bgTileSetInt.Maximum = 0;
                MainWindow.window.tileE.bgTileSetInt.Value = 0;
            }
            // Set Background Tile Values
            int listOffset = BitConverter.ToUInt16(SNES.rom, Const.BackgroundTileInfoOffset + Level.Id * 2) + Const.BackgroundTileInfoOffset;
            int offset = BitConverter.ToUInt16(SNES.rom, listOffset + (int)MainWindow.window.tileE.bgTileSetInt.Value * 2) + Const.BackgroundTileInfoOffset;

            if (BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(offset)) != 0)
            {
                MainWindow.window.tileE.bgLengthInt.IsEnabled = true;
                MainWindow.window.tileE.bgAddressInt.IsEnabled = true;
                MainWindow.window.tileE.bgSrcBox.IsEnabled = true;
                MainWindow.window.tileE.bgPalInt.IsEnabled = true;
                SetBackgroundValues(offset);
            }
            else
            {
                MainWindow.window.tileE.bgLengthInt.IsEnabled = false;
                MainWindow.window.tileE.bgAddressInt.IsEnabled = false;
                MainWindow.window.tileE.bgSrcBox.IsEnabled = false;
                MainWindow.window.tileE.bgPalInt.IsEnabled = false;
            }
        }
        private void SetBackgroundValues(int offset)
        {
            int length = BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(offset));
            int dest = BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(offset + 2));
            int srcAddr = BinaryPrimitives.ReadInt32LittleEndian(SNES.rom.AsSpan(offset + 4)) & 0xFFFFFF;
            int palId = BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(offset + 7));

            if (romOffsetCheck.IsChecked == true)
                srcAddr = SNES.CpuToOffset(srcAddr);

            MainWindow.window.tileE.bgLengthInt.Value = length;
            MainWindow.window.tileE.bgAddressInt.Value = dest;
            _suppressBgSrcBoxTextChanged = true;
            MainWindow.window.tileE.bgSrcBox.Text = srcAddr.ToString("X6");
            _suppressBgSrcBoxTextChanged = false;
            MainWindow.window.tileE.bgPalInt.Value = palId;
        }
        #endregion Methods

        #region Events
        private void RedrawBtn_Click(object sender, RoutedEventArgs e)
        {
            if (Level.Id >= Const.PlayableLevelsCount || MainWindow.window.tileE.bgTileSetInt.Value == null)
                return;

            int listOffset = BitConverter.ToUInt16(SNES.rom, Const.BackgroundTileInfoOffset + Level.Id * 2) + Const.BackgroundTileInfoOffset;
            int offset = BitConverter.ToUInt16(SNES.rom, listOffset + (int)MainWindow.window.tileE.bgTileSetInt.Value * 2) + Const.BackgroundTileInfoOffset;

            if (BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(offset)) == 0) return;

                Level.TileSet = (int)MainWindow.window.tileE.bgTileSetInt.Value;

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
        private void bgTileSetInt_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue == null || Level.Id >= Const.PlayableLevelsCount || SNES.rom == null)
                return;
            int listOffset = BitConverter.ToUInt16(SNES.rom, Const.BackgroundTileInfoOffset + Level.Id * 2) + Const.BackgroundTileInfoOffset;
            int offset = BitConverter.ToUInt16(SNES.rom, listOffset + (int)MainWindow.window.tileE.bgTileSetInt.Value * 2) + Const.BackgroundTileInfoOffset;
            if (BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(offset)) != 0)
            {
                MainWindow.window.tileE.bgLengthInt.IsEnabled = true;
                MainWindow.window.tileE.bgAddressInt.IsEnabled = true;
                MainWindow.window.tileE.bgSrcBox.IsEnabled = true;
                MainWindow.window.tileE.bgPalInt.IsEnabled = true;
                SetBackgroundValues(offset);
            }
            else
            {
                MainWindow.window.tileE.bgLengthInt.IsEnabled = false;
                MainWindow.window.tileE.bgAddressInt.IsEnabled = false;
                MainWindow.window.tileE.bgSrcBox.IsEnabled = false;
                MainWindow.window.tileE.bgPalInt.IsEnabled = false;
            }
        }
        private void bgLengthInt_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue == null || Level.Id >= Const.PlayableLevelsCount || SNES.rom == null)
                return;

            int listOffset = BitConverter.ToUInt16(SNES.rom, Const.BackgroundTileInfoOffset + Level.Id * 2) + Const.BackgroundTileInfoOffset;
            int offset = BitConverter.ToUInt16(SNES.rom, listOffset + (int)MainWindow.window.tileE.bgTileSetInt.Value * 2) + Const.BackgroundTileInfoOffset;

            ushort val = (ushort)(int)e.NewValue;

            if (BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(offset)) == val)
                return;

            BinaryPrimitives.WriteUInt16LittleEndian(SNES.rom.AsSpan(offset), val);
            SNES.edit = true;
        }
        private void bgAddressInt_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue == null || Level.Id >= Const.PlayableLevelsCount || SNES.rom == null)
                return;

            int listOffset = BitConverter.ToUInt16(SNES.rom, Const.BackgroundTileInfoOffset + Level.Id * 2) + Const.BackgroundTileInfoOffset;
            int offset = BitConverter.ToUInt16(SNES.rom, listOffset + (int)MainWindow.window.tileE.bgTileSetInt.Value * 2) + Const.BackgroundTileInfoOffset;

            ushort val = (ushort)(int)e.NewValue;

            if (BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(offset + 2)) == val)
                return;

            BinaryPrimitives.WriteUInt16LittleEndian(SNES.rom.AsSpan(offset + 2), val);
            SNES.edit = true;
        }
        private void bgSrcBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (Level.Id >= Const.PlayableLevelsCount || SNES.rom == null || _suppressBgSrcBoxTextChanged)
                return;
            int srcAddr = 0;
            try
            {
                srcAddr = int.Parse(bgSrcBox.Text, System.Globalization.NumberStyles.HexNumber) & 0xFFFFFF;
                if (romOffsetCheck.IsChecked == true)
                    srcAddr = SNES.OffsetToCpu(srcAddr);
            }
            catch (Exception)
            {
                return;
            }
            int listOffset = BitConverter.ToUInt16(SNES.rom, Const.BackgroundTileInfoOffset + Level.Id * 2) + Const.BackgroundTileInfoOffset;
            int offset = BitConverter.ToUInt16(SNES.rom, listOffset + (int)MainWindow.window.tileE.bgTileSetInt.Value * 2) + Const.BackgroundTileInfoOffset;


            if ((BinaryPrimitives.ReadInt32LittleEndian(SNES.rom.AsSpan(offset + 4)) & 0x7FFFFF) == (srcAddr & 0x7FFFFF))
                return;
            if (Const.Id == Const.GameId.MegaManX == false)
                srcAddr |= 0x800000;
            BinaryPrimitives.WriteInt32LittleEndian(SNES.rom.AsSpan(offset + 4), (srcAddr & 0x7FFFFF));
            SNES.edit = true;
        }
        private void bgPalInt_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue == null || Level.Id >= Const.PlayableLevelsCount || SNES.rom == null)
                return;

            int listOffset = BitConverter.ToUInt16(SNES.rom, Const.BackgroundTileInfoOffset + Level.Id * 2) + Const.BackgroundTileInfoOffset;
            int offset = BitConverter.ToUInt16(SNES.rom, listOffset + (int)MainWindow.window.tileE.bgTileSetInt.Value * 2) + Const.BackgroundTileInfoOffset;

            ushort val = (ushort)(int)e.NewValue;

            if (BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(offset + 7)) == val)
                return;

            BinaryPrimitives.WriteUInt16LittleEndian(SNES.rom.AsSpan(offset + 7), val);
            SNES.edit = true;
        }
        private void romOffsetCheck_Changed(object sender, RoutedEventArgs e)
        {
            if (Level.Id >= Const.PlayableLevelsCount || SNES.rom == null)
                return;

            int listOffset = BitConverter.ToUInt16(SNES.rom, Const.BackgroundTileInfoOffset + Level.Id * 2) + Const.BackgroundTileInfoOffset;
            int offset = BitConverter.ToUInt16(SNES.rom, listOffset + (int)MainWindow.window.tileE.bgTileSetInt.Value * 2) + Const.BackgroundTileInfoOffset;

            if (romOffsetCheck.IsChecked == true)
                bgSrcText.Text = "ROM Offset:";
            else
                bgSrcText.Text = "CPU Address:";

            if (BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(offset)) == 0) return;

            int valNew = 0;
            _suppressBgSrcBoxTextChanged = true;
            try
            {
                valNew = int.Parse(bgSrcBox.Text, System.Globalization.NumberStyles.HexNumber) & 0xFFFFFF;
                if (romOffsetCheck.IsChecked == true)
                    valNew = SNES.CpuToOffset(valNew);
                else
                    valNew = SNES.OffsetToCpu(valNew);

                if (Const.Id == Const.GameId.MegaManX && romOffsetCheck.IsChecked == false) valNew |= 0x800000;
                bgSrcBox.Text = valNew.ToString("X6");
            }
            catch (Exception)
            {
                return;
            }
            _suppressBgSrcBoxTextChanged = false;
        }
        #endregion Events
    }
}