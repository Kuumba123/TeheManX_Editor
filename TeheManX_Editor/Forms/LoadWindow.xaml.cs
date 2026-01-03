using System;
using System.Buffers.Binary;
using System.Windows;

namespace TeheManX_Editor.Forms
{
    /// <summary>
    /// Interaction logic for LoadWindow.xaml
    /// </summary>
    public partial class LoadWindow : Window
    {
        #region Properties
        bool shouldUpdate = false;
        #endregion Properties

        #region Constructor
        public LoadWindow()
        {
            InitializeComponent();

            if (Const.Id == Const.GameId.MegaManX)
            {
                tilesInt.Maximum = Const.MegaManX.CompressedTilesAmount - 1;
                //paletteInt.Maximum = Const.MegaManX.PalettesAmount - 1;
            }
            else if (Const.Id == Const.GameId.MegaManX2)
            {
                tilesInt.Maximum = Const.MegaManX2.CompressedTilesAmount - 1;
                //paletteInt.Maximum = Const.MegaManX2.PalettesAmount - 1;
            }
            else if (Const.Id == Const.GameId.MegaManX3)
            {
                tilesInt.Maximum = Const.MegaManX3.CompressedTilesAmount - 1;
                //paletteInt.Maximum = Const.MegaManX3.PalettesAmount - 1;
            }
        }
        #endregion Constructor

        #region Events
        private void palBtn_Click(object sender, RoutedEventArgs e)
        {
            if (paletteInt.Value == null) return;

            if (((int)paletteInt.Value & 1) != 0)
            {
                MessageBox.Show("You must input an even number when selecting a Palette to load.", "ERROR");
                return;
            }

            int id = (int)paletteInt.Value;
            int infoOffset = SNES.CpuToOffset(BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(Const.PaletteInfoOffset + id)), Const.PaletteBank);

            while (SNES.rom[infoOffset] != 0)
            {
                int colorCount = SNES.rom[infoOffset]; //how many colors are going to be dumped
                byte colorIndex = SNES.rom[infoOffset + 3]; //which color index to start dumping at
                int colorOffset = SNES.CpuToOffset(BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(infoOffset + 1)) + (Const.PaletteColorBank << 16)); //where the colors are located

                for (int c = 0; c < colorCount; c++)
                {
                    if ((colorIndex + c) > 0x7F)
                        break;

                    ushort color = BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(colorOffset + c * 2));
                    byte R = (byte)(color % 32 * 8);
                    byte G = (byte)(color / 32 % 32 * 8);
                    byte B = (byte)(color / 1024 % 32 * 8);

                    Level.Palette[colorIndex + c] = (uint)(0xFF000000 | (R << 16) | (G << 8) | B);
                }
                infoOffset += 4;
            }
            shouldUpdate = true;
            MessageBox.Show("Palette loaded!");
        }
        private void tileBtn_Click(object sender, RoutedEventArgs e)
        {
            if (tilesInt.Value == null) return;

            int compressId = (int)tilesInt.Value;

            if (Const.Id == Const.GameId.MegaManX) // compression algorithem just for MMX
            {
                int addr_W = 0x200;
                int controlB;
                byte copyB;

                ushort size = BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(compressId * 5 + Const.CompressedTileInfoOffset));
                size = (ushort)((size + 7) >> 3);
                int addr_R = SNES.CpuToOffset(BinaryPrimitives.ReadInt32LittleEndian(SNES.rom.AsSpan((compressId * 5) + Const.CompressedTileInfoOffset + 2)));
                try
                {
                    while (size != 0)
                    {
                        controlB = SNES.rom[addr_R];
                        addr_R++;
                        copyB = SNES.rom[addr_R];
                        addr_R++;
                        for (int i = 0; i < 8; i++)
                        {
                            controlB <<= 1;
                            if ((controlB & 0x100) != 0x100)
                            {
                                Level.Tiles[addr_W] = copyB;
                                addr_W++;
                            }
                            else
                            {
                                Level.Tiles[addr_W] = SNES.rom[addr_R];
                                addr_R++;
                                addr_W++;
                            }
                        }
                        size--;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error happened when decompress Tile Graphics" + ex.Message + "\nCorrupted ROM ?", "ERROR");
                    return;
                }
            }
            else // compression algorithem for MMX2 and MMX3
            {
                int addr_R = SNES.CpuToOffset(BinaryPrimitives.ReadInt32LittleEndian(SNES.rom.AsSpan(compressId * 5 + Const.CompressedTileInfoOffset)));
                int size = BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(compressId * 5 + Const.CompressedTileInfoOffset + 3));
                int addr_W = 0x200;

                try
                {
                    byte controlB = SNES.rom[addr_R];
                    addr_R++;
                    byte controlC = 8;

                    while (true)
                    {
                        if ((controlB & 0x80) == 0)
                        {
                            Level.Tiles[addr_W] = SNES.rom[addr_R];
                            addr_R++;
                            addr_W++;
                            size--;
                        }
                        else // Copy from Window
                        {
                            int windowPosition = (SNES.rom[addr_R] & 3) << 8;
                            windowPosition |= SNES.rom[addr_R + 1];
                            int length = SNES.rom[addr_R] >> 2;

                            for (int i = 0; i < length; i++)
                            {
                                Level.Tiles[addr_W] = Level.Tiles[addr_W - windowPosition];
                                addr_W++;
                            }
                            size -= length;
                            addr_R += 2;
                        }
                        controlB <<= 1;
                        controlC--;

                        if (size < 1)
                            break;

                        if (controlC == 0)
                        {
                            //Reload Control Byte
                            controlB = SNES.rom[addr_R];
                            addr_R++;
                            controlC = 8;
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error happened when decompress Tile Graphics" + ex.Message + "\nCorrupted ROM ?", "ERROR");
                    return;
                }
            }
            shouldUpdate = true;
            Level.DecodeAllTiles();
            MessageBox.Show("Tiles loaded!");
        }
        private void Window_Closed(object sender, EventArgs e)
        {
            if (shouldUpdate) MainWindow.window.Update();
        }
        #endregion Events
    }
}
