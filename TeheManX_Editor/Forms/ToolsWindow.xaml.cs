using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;

namespace TeheManX_Editor.Forms
{
    /// <summary>
    /// Interaction logic for ToolsWindow.xaml
    /// </summary>
    public partial class ToolsWindow : Window
    {
        #region Constructors
        public ToolsWindow()
        {
            InitializeComponent();
        }
        #endregion Constructors

        #region Events
        /*
         *  MegaMan X Tools Events
         */
        private void MMX1_DecompressClick(object sender, RoutedEventArgs e)
        {
            using (var fd = new OpenFileDialog())
            {
                fd.Filter = "SFC |*.sfc";
                fd.Title = "Open an MegaMan X SFC File";

                if (fd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    byte[] rom = File.ReadAllBytes(fd.FileName);

                    var sfd = new Ookii.Dialogs.Wpf.VistaFolderBrowserDialog();
                    sfd.Description = "Select Save Location";
                    sfd.UseDescriptionForTitle = true;

                    if ((bool)sfd.ShowDialog())
                    {
                        int offset = 0;
                        if (Encoding.ASCII.GetString(rom, 0x7FC0, 10) == "MEGAMAN X ")
                            offset = Const.MegaManX.NA.CompressedTileInfoOffset;
                        else if (Encoding.ASCII.GetString(rom, 0x7FC0, 10) == "ROCKMAN X ")
                            offset = Const.MegaManX.JP.CompressedTileInfoOffset;
                        else
                        {
                            System.Windows.MessageBox.Show("Invalid Rom!\nDid not find correct Cart Name.", "ERROR");
                            return;
                        }
                        for (int s = 0; s < Const.MegaManX.CompressedTilesAmount; s++)
                        {
                            int addr_R = SNES.CpuToOffset(BitConverter.ToInt32(rom, (s * 5) + offset + 2));
                            ushort size = BitConverter.ToUInt16(rom, s * 5 + offset);
                            size = (ushort)((size + 7) >> 3);
                            byte[] data = new byte[size * 8];

                            int controlB = 0;
                            byte copyB = 0;
                            int addr_W = 0;

                            try
                            {
                                while (size != 0)
                                {
                                    controlB = rom[addr_R];
                                    addr_R++;
                                    copyB = rom[addr_R];
                                    addr_R++;
                                    for (int i = 0; i < 8; i++)
                                    {
                                        controlB <<= 1;
                                        if ((controlB & 0x100) != 0x100)
                                        {
                                            data[addr_W] = copyB;
                                            addr_W++;
                                        }
                                        else
                                        {
                                            data[addr_W] = rom[addr_R];
                                            addr_R++;
                                            addr_W++;
                                        }
                                    }
                                    size--;
                                }
                                File.WriteAllBytes($"{sfd.SelectedPath}\\Tiles_{s:X2}.bin", data);
                            }
                            catch (Exception ex)
                            {
                                System.Windows.MessageBox.Show($"Error happened when decompress - {s:X}" + " Tile Graphics" + ex.Message + "\nCorrupted ROM ?", "ERROR");
                                System.Windows.Application.Current.Shutdown();
                            }
                        }
                        System.Windows.MessageBox.Show("Tiles Data Decompressed!");
                    }
                }
            }
        }
        private void MMX1_CompressClick(object sender, RoutedEventArgs e)
        {
            using (var fd = new OpenFileDialog())
            {
                fd.Title = "Open a file containing Tiles for MegaMan X";

                if (fd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    using (var sfd = new SaveFileDialog())
                    {
                        if (sfd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                        {
                            int addr_W, addr_R = 0;
                            int copyB;
                            int controlB;
                            int most; //the byte that has shown up the most
                            int most_index;
                            int dumpSize;

                            int[] byteS = new int[8];
                            int[] byteA = new int[8];
                            int[] byteC = new int[8]; //Byte Copied Flag

                            try
                            {
                                byte[] inputData = File.ReadAllBytes(fd.FileName);

                                MemoryStream ms = new MemoryStream();
                                BinaryWriter bw = new BinaryWriter(ms);

                                dumpSize = inputData.Length >> 3;

                                while (dumpSize != 0)
                                {
                                    //Reset Flags
                                    byteC[0] = 0;
                                    byteC[1] = 0;
                                    byteC[2] = 0;
                                    byteC[3] = 0;
                                    byteC[4] = 0;
                                    byteC[5] = 0;
                                    byteC[6] = 0;
                                    byteC[7] = 0;

                                    byteA[0] = 0;
                                    byteA[1] = 0;
                                    byteA[2] = 0;
                                    byteA[3] = 0;
                                    byteA[4] = 0;
                                    byteA[5] = 0;
                                    byteA[6] = 0;
                                    byteA[7] = 0;

                                    most = 0;
                                    most_index = 0;
                                    controlB = 0xFF;
                                    copyB = 0;

                                    //Copy 8 set of bytes to Arrray
                                    for (int i = 0; i < 8; i++)
                                    {
                                        byteS[i] = inputData[addr_R + i];
                                    }

                                    //Check the amount of times a byte is used
                                    for (int a = 0; a < 8; a++)
                                    {
                                        for (int b = 0; b < 8; b++)
                                        {
                                            if (byteS[a] == byteS[b])
                                            {
                                                byteA[a]++;
                                            }
                                        }
                                    }

                                    //Figure out witch one was copied the most
                                    for (int i = 0; i < 8; i++)
                                    {
                                        if (byteA[i] > most)
                                        {
                                            most = byteA[i];
                                            most_index = i;
                                        }
                                    }

                                    //Figure out witch specfic bytes were copied
                                    for (int i = 0; i < 8; i++)
                                    {
                                        if (byteS[most_index] == byteS[i])
                                        {
                                            controlB = controlB ^ (1 << (7 - i));
                                            byteC[i] = 1;
                                        }
                                    }

                                    //Write Control and Copy Bytes
                                    bw.Write((byte)controlB);
                                    copyB = byteS[most_index];
                                    bw.Write((byte)copyB);

                                    //Determine if byte is copied or unique
                                    for (int i = 0; i < 8; i++)
                                    {
                                        if (byteC[i] != 1)
                                        {
                                            bw.Write((byte)byteS[i]);
                                        }
                                    }
                                    //Increament Read Offset and Loop Counter
                                    addr_R += 8;
                                    dumpSize--;
                                }
                                bw.Close();
                                ms.Close();
                                File.WriteAllBytes(sfd.FileName, ms.ToArray());
                            }
                            catch(Exception ex)
                            {
                                System.Windows.MessageBox.Show(ex.Message, "ERROR");
                                System.Windows.Application.Current.Shutdown();
                            }
                            //Done
                            System.Windows.MessageBox.Show("MegaMan X Tile Data Compressed!");
                        }
                    }
                }
            }
        }
        private void MMX1_ReplaceCompressClick(object sender, RoutedEventArgs e)
        {
            using (var fd = new OpenFileDialog())
            {
                fd.Filter = "SFC |*.sfc";
                fd.Title = "Open an MegaMan X SFC File";

                if (fd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    byte[] rom = File.ReadAllBytes(fd.FileName);

                    int offset = 0;
                    if (Encoding.ASCII.GetString(rom, 0x7FC0, 10) == "MEGAMAN X ")
                        offset = Const.MegaManX.NA.CompressedTileInfoOffset;
                    else if (Encoding.ASCII.GetString(rom, 0x7FC0, 10) == "ROCKMAN X ")
                        offset = Const.MegaManX.JP.CompressedTileInfoOffset;
                    else
                    {
                        System.Windows.MessageBox.Show("Invalid Rom!\nDid not find correct Cart Name.", "ERROR");
                        return;
                    }
                    using (var ofd = new OpenFileDialog())
                    {
                        ofd.Title = "Open a file containing Uncompressed Tiles for MegaMan X";

                        byte[] compressedData = null;
                        byte[] inputData = null;

                        if (ofd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                        {
                            int addr_W, addr_R = 0;
                            int copyB;
                            int controlB;
                            int most; //the byte that has shown up the most
                            int most_index;
                            int dumpSize;

                            int[] byteS = new int[8];
                            int[] byteA = new int[8];
                            int[] byteC = new int[8]; //Byte Copied Flag

                            try
                            {
                                inputData = File.ReadAllBytes(ofd.FileName);
                                if ((inputData.Length % 8) != 0)
                                {
                                    System.Windows.MessageBox.Show("Input Tile Data is not a multiple of 8 bytes!", "ERROR");
                                    return;
                                }
                                MemoryStream ms = new MemoryStream();
                                BinaryWriter bw = new BinaryWriter(ms);

                                dumpSize = inputData.Length >> 3;

                                while (dumpSize != 0)
                                {
                                    //Reset Flags
                                    byteC[0] = 0;
                                    byteC[1] = 0;
                                    byteC[2] = 0;
                                    byteC[3] = 0;
                                    byteC[4] = 0;
                                    byteC[5] = 0;
                                    byteC[6] = 0;
                                    byteC[7] = 0;

                                    byteA[0] = 0;
                                    byteA[1] = 0;
                                    byteA[2] = 0;
                                    byteA[3] = 0;
                                    byteA[4] = 0;
                                    byteA[5] = 0;
                                    byteA[6] = 0;
                                    byteA[7] = 0;

                                    most = 0;
                                    most_index = 0;
                                    controlB = 0xFF;
                                    copyB = 0;

                                    //Copy 8 set of bytes to Arrray
                                    for (int i = 0; i < 8; i++)
                                    {
                                        byteS[i] = inputData[addr_R + i];
                                    }

                                    //Check the amount of times a byte is used
                                    for (int a = 0; a < 8; a++)
                                    {
                                        for (int b = 0; b < 8; b++)
                                        {
                                            if (byteS[a] == byteS[b])
                                            {
                                                byteA[a]++;
                                            }
                                        }
                                    }

                                    //Figure out witch one was copied the most
                                    for (int i = 0; i < 8; i++)
                                    {
                                        if (byteA[i] > most)
                                        {
                                            most = byteA[i];
                                            most_index = i;
                                        }
                                    }

                                    //Figure out witch specfic bytes were copied
                                    for (int i = 0; i < 8; i++)
                                    {
                                        if (byteS[most_index] == byteS[i])
                                        {
                                            controlB = controlB ^ (1 << (7 - i));
                                            byteC[i] = 1;
                                        }
                                    }

                                    //Write Control and Copy Bytes
                                    bw.Write((byte)controlB);
                                    copyB = byteS[most_index];
                                    bw.Write((byte)copyB);

                                    //Determine if byte is copied or unique
                                    for (int i = 0; i < 8; i++)
                                    {
                                        if (byteC[i] != 1)
                                        {
                                            bw.Write((byte)byteS[i]);
                                        }
                                    }
                                    //Increament Read Offset and Loop Counter
                                    addr_R += 8;
                                    dumpSize--;
                                }
                                bw.Close();
                                ms.Close();
                                compressedData = ms.ToArray();
                            }
                            catch (Exception ex)
                            {
                                System.Windows.MessageBox.Show(ex.Message, "ERROR");
                                System.Windows.Application.Current.Shutdown();
                            }
                            //Done Compressing now ask for tile index to replace

                            // Create UI elements
                            Window window = new Window
                            {
                                Title = "Info",
                                SizeToContent = SizeToContent.WidthAndHeight,
                                ResizeMode = ResizeMode.NoResize,
                                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                                Owner = System.Windows.Application.Current.MainWindow
                            };
                            TextBlock textBlock = new TextBlock() { Text = "Select Compressed Tiles Id", FontSize = 18, Foreground = System.Windows.Media.Brushes.White, FontFamily = new System.Windows.Media.FontFamily("Consolas") };
                            NumInt numInt = new NumInt() { Margin = new Thickness(5), Minimum = 1, Maximum = (Const.MegaManX.CompressedTilesAmount - 1), Value = 0, Width = 100, FontFamily = new System.Windows.Media.FontFamily("Consolas"), FontSize = 16 };
                            Grid.SetColumn(numInt, 1);
                            Grid.SetRow(numInt, 1);
                            System.Windows.Controls.Button confirmBtn = new System.Windows.Controls.Button() { Content = "OK", Width = 75, Height = 30, Margin = new Thickness(5) };
                            confirmBtn.Click += (se, ev) =>
                            {
                                if (numInt.Value == null)
                                    return;

                                int set = (int)numInt.Value;

                                int addr_R = SNES.CpuToOffset(BitConverter.ToInt32(rom, (set * 5) + offset + 2));
                                ushort size = BitConverter.ToUInt16(rom, set * 5 + offset);
                                size = (ushort)((size + 7) >> 3);

                                int controlB = 0;
                                byte copyB = 0;
                                int length = 0;


                                while (size != 0)
                                {
                                    controlB = rom[addr_R];
                                    addr_R++;
                                    length++;
                                    copyB = rom[addr_R];
                                    addr_R++;
                                    length++;
                                    for (int i = 0; i < 8; i++)
                                    {
                                        controlB <<= 1;
                                        if ((controlB & 0x100) == 0x100)
                                        {
                                            length++;
                                            addr_R++;
                                        }
                                    }
                                    size--;
                                }

                                if (compressedData.Length > length)
                                {
                                    System.Windows.MessageBox.Show($"New compressed data is larger than the original!\n{compressedData.Length:X} vs {length:X}", "ERROR");
                                    return;
                                }

                                addr_R = SNES.CpuToOffset(BitConverter.ToInt32(rom, (set * 5) + offset + 2));
                                BinaryPrimitives.WriteUInt16LittleEndian(rom.AsSpan((set * 5) + offset), (ushort)inputData.Length);
                                Array.Copy(compressedData, 0, rom, addr_R, compressedData.Length);
                                File.WriteAllBytes(fd.FileName, rom);

                                System.Windows.MessageBox.Show("Tile Data Replaced!");
                                window.Close();
                            };
                            Grid.SetRow(confirmBtn, 1);
                            Grid grid = new Grid();
                            grid.Background = System.Windows.Media.Brushes.Black;
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
                    }
                }
            }
        }
        /*
        *  MegaMan X2 Tools Events
        */
        private void MMX2_DecompressClick(object sender, RoutedEventArgs e)
        {
            using (var fd = new OpenFileDialog())
            {
                fd.Filter = "SFC |*.sfc";
                fd.Title = "Open an MegaMan X2 or MegaMan X3 SFC File";

                if (fd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    byte[] rom = File.ReadAllBytes(fd.FileName);

                    var sfd = new Ookii.Dialogs.Wpf.VistaFolderBrowserDialog();
                    sfd.Description = "Select Save Location";
                    sfd.UseDescriptionForTitle = true;

                    if ((bool)sfd.ShowDialog())
                    {
                        int offset = 0;
                        if (Encoding.ASCII.GetString(rom, 0x7FC0, 10) == "MEGAMAN X2")
                            offset = Const.MegaManX2.NA.CompressedTileInfoOffset;
                        else if (Encoding.ASCII.GetString(rom, 0x7FC0, 10) == "ROCKMAN X2")
                            offset = Const.MegaManX2.JP.CompressedTileInfoOffset;
                        else if (Encoding.ASCII.GetString(rom, 0x7FC0, 10) == "MEGAMAN X3")
                            offset = Const.MegaManX3.NA.CompressedTileInfoOffset;
                        else if (Encoding.ASCII.GetString(rom, 0x7FC0, 10) == "ROCKMAN X3")
                            offset = Const.MegaManX3.JP.CompressedTileInfoOffset;
                        else
                        {
                            System.Windows.MessageBox.Show("Invalid Rom!\nDid not find correct Cart Name.", "ERROR");
                            return;
                        }
                        int count;
                        if (rom[0x7FC0 + 9] == '2')
                            count = Const.MegaManX2.CompressedTilesAmount;
                        else
                            count = Const.MegaManX3.CompressedTilesAmount;
                        for (int s = 0; s < count; s++)
                        {
                            int addr_R = SNES.CpuToOffset(BitConverter.ToInt32(rom, (s * 5) + offset));
                            int size = BitConverter.ToUInt16(rom, s * 5 + offset + 3);
                            byte[] data = new byte[size];
                            int addr_W = 0;

                            try
                            {
                                byte controlB = rom[addr_R];
                                addr_R++;
                                byte controlC = 8;

                                while (true)
                                {
                                    if ((controlB & 0x80) == 0)
                                    {
                                        data[addr_W] = rom[addr_R];
                                        addr_R++;
                                        addr_W++;
                                        size--;
                                    }
                                    else // Copy from Window
                                    {
                                        int windowPosition = (rom[addr_R] & 3) << 8;
                                        windowPosition |= rom[addr_R + 1];
                                        int length = rom[addr_R] >> 2;

                                        for (int i = 0; i < length; i++)
                                        {
                                            data[addr_W] = data[addr_W - windowPosition];
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
                                        controlB = rom[addr_R];
                                        addr_R++;
                                        controlC = 8;
                                    }
                                }

                                File.WriteAllBytes($"{sfd.SelectedPath}\\Tiles_{s:X2}.bin", data);
                            }
                            catch (Exception ex)
                            {
                                System.Windows.MessageBox.Show($"Error happened when decompress - {s:X}" + " Tile Graphics" + ex.Message + "\nCorrupted ROM ?", "ERROR");
                                System.Windows.Application.Current.Shutdown();
                            }
                        }
                        System.Windows.MessageBox.Show("Tiles Data Decompressed!");
                    }
                }
            }
        }
        private void MMX2_CompressClick(object sender, RoutedEventArgs e)
        {
            using (var fd = new OpenFileDialog())
            {
                fd.Title = "Open a file containing Tiles for MegaMan X2 or MegaMan X3";

                if (fd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    using (var sfd = new SaveFileDialog())
                    {
                        if (sfd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                        {
                            const int WINDOW_SIZE = 0x3FF;
                            const int MAX_LENGTH = 0x3F;

                            try
                            {
                                byte[] inputData = File.ReadAllBytes(fd.FileName);
                                int dataLength = inputData.Length;

                                List<(int distance, int length, byte nextChar)> compressedData = new List<(int distance, int length, byte nextChar)>(dataLength / 4);
                                List<byte> compressedBytes = new List<byte>(dataLength);

                                //Attempting to find matches
                                int i = 0;
                                while (i < dataLength)
                                {
                                    int matchLength = 0;
                                    int matchDistance = 0;

                                    // Look back within the window to find the longest match
                                    for (int j = 1; j <= Math.Min(WINDOW_SIZE, i); j++)
                                    {
                                        int substringLength = 0;

                                        while (substringLength < Math.Min(Math.Min(dataLength - i, WINDOW_SIZE), MAX_LENGTH) &&
                                               (i - j + substringLength) >= 0 &&
                                               (i + substringLength) < dataLength &&
                                               inputData[i - j + substringLength] == inputData[i + substringLength])
                                        {
                                            substringLength++;
                                        }

                                        if (substringLength > matchLength)
                                        {
                                            matchLength = substringLength;
                                            matchDistance = j;
                                        }
                                    }

                                    // Add match tuple
                                    if (matchLength > 2 && (i + matchLength) < dataLength && (dataLength - i) > 3)
                                    {
                                        compressedData.Add((matchDistance, matchLength, inputData[i + matchLength]));
                                        i += matchLength;
                                    }
                                    else
                                    {
                                        compressedData.Add((0, 0, inputData[i]));
                                        i++;
                                    }
                                }

                                // Convert to SNES MegaMan compression format
                                int controlB = 0;
                                int controlC = 8;
                                int offset = 0;

                                for (int index = 0; index < compressedData.Count; index++)
                                {
                                    var (distance, length, nextChar) = compressedData[index];

                                    if (length == 0)
                                    {
                                        // unique byte (no match)
                                        compressedBytes.Add(nextChar);
                                    }
                                    else
                                    {
                                        controlB |= 1;
                                        compressedBytes.Add((byte)(((length) << 2) + (distance >> 8)));
                                        compressedBytes.Add((byte)(distance & 0xFF));
                                    }

                                    controlC--;

                                    if (controlC == 0)
                                    {
                                        // insert control byte at start of 8-byte block
                                        controlC = 8;
                                        compressedBytes.Insert(offset, (byte)controlB);
                                        controlB = 0;
                                        offset = compressedBytes.Count;
                                    }
                                    else
                                    {
                                        controlB <<= 1;
                                    }
                                }
                                if (controlC < 8)
                                {
                                    // write remaining control byte
                                    controlB <<= (controlC - 1);
                                    compressedBytes.Insert(offset, (byte)controlB);
                                }

                                File.WriteAllBytes(sfd.FileName, compressedBytes.ToArray());
                            }
                            catch (Exception ex)
                            {
                                System.Windows.MessageBox.Show(ex.Message, "ERROR");
                                System.Windows.Application.Current.Shutdown();
                            }
                            //Done
                            System.Windows.MessageBox.Show("MegaMan X2/MegaMan X3 Tile Data Compressed!");
                        }
                    }
                }
            }
        }
        private void MMX2_ReplaceCompressClick(object sender, RoutedEventArgs e)
        {
            using (var fd = new OpenFileDialog())
            {
                fd.Filter = "SFC |*.sfc";
                fd.Title = "Open an MegaMan X2 or MegaMan X3 SFC File";

                if (fd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    byte[] rom = File.ReadAllBytes(fd.FileName);

                    int romOffset = 0;
                    if (Encoding.ASCII.GetString(rom, 0x7FC0, 10) == "MEGAMAN X2")
                        romOffset = Const.MegaManX2.NA.CompressedTileInfoOffset;
                    else if (Encoding.ASCII.GetString(rom, 0x7FC0, 10) == "ROCKMAN X2")
                        romOffset = Const.MegaManX2.JP.CompressedTileInfoOffset;
                    else if (Encoding.ASCII.GetString(rom, 0x7FC0, 10) == "MEGAMAN X3")
                        romOffset = Const.MegaManX3.NA.CompressedTileInfoOffset;
                    else if (Encoding.ASCII.GetString(rom, 0x7FC0, 10) == "ROCKMAN X3")
                        romOffset = Const.MegaManX3.JP.CompressedTileInfoOffset;
                    else
                    {
                        System.Windows.MessageBox.Show("Invalid Rom!\nDid not find correct Cart Name.", "ERROR");
                        return;
                    }
                    int count;
                    if (rom[0x7FC0 + 9] == '2')
                        count = Const.MegaManX2.CompressedTilesAmount;
                    else
                        count = Const.MegaManX3.CompressedTilesAmount;
                    using (var ofd = new OpenFileDialog())
                    {
                        ofd.Title = "Open a file containing Uncompressed Tiles for MegaMan X";

                        byte[] compressedOutput = null;
                        byte[] inputData = null;

                        if (ofd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                        {
                            

                            try
                            {
                                const int WINDOW_SIZE = 0x3FF;
                                const int MAX_LENGTH = 0x3F;

                                try
                                {
                                    inputData = File.ReadAllBytes(ofd.FileName);
                                    int dataLength = inputData.Length;

                                    List<(int distance, int length, byte nextChar)> compressedData = new List<(int distance, int length, byte nextChar)>(dataLength / 4);
                                    List<byte> compressedBytes = new List<byte>(dataLength);

                                    //Attempting to find matches
                                    int i = 0;
                                    while (i < dataLength)
                                    {
                                        int matchLength = 0;
                                        int matchDistance = 0;

                                        // Look back within the window to find the longest match
                                        for (int j = 1; j <= Math.Min(WINDOW_SIZE, i); j++)
                                        {
                                            int substringLength = 0;

                                            while (substringLength < Math.Min(Math.Min(dataLength - i, WINDOW_SIZE), MAX_LENGTH) &&
                                                   (i - j + substringLength) >= 0 &&
                                                   (i + substringLength) < dataLength &&
                                                   inputData[i - j + substringLength] == inputData[i + substringLength])
                                            {
                                                substringLength++;
                                            }

                                            if (substringLength > matchLength)
                                            {
                                                matchLength = substringLength;
                                                matchDistance = j;
                                            }
                                        }

                                        // Add match tuple
                                        if (matchLength > 2 && (i + matchLength) < dataLength && (dataLength - i) > 3)
                                        {
                                            compressedData.Add((matchDistance, matchLength, inputData[i + matchLength]));
                                            i += matchLength;
                                        }
                                        else
                                        {
                                            compressedData.Add((0, 0, inputData[i]));
                                            i++;
                                        }
                                    }

                                    // Convert to SNES MegaMan compression format
                                    int controlB = 0;
                                    int controlC = 8;
                                    int offset = 0;

                                    for (int index = 0; index < compressedData.Count; index++)
                                    {
                                        var (distance, length, nextChar) = compressedData[index];

                                        if (length == 0)
                                        {
                                            // unique byte (no match)
                                            compressedBytes.Add(nextChar);
                                        }
                                        else
                                        {
                                            controlB |= 1;
                                            compressedBytes.Add((byte)(((length) << 2) + (distance >> 8)));
                                            compressedBytes.Add((byte)(distance & 0xFF));
                                        }

                                        controlC--;

                                        if (controlC == 0)
                                        {
                                            // insert control byte at start of 8-byte block
                                            controlC = 8;
                                            compressedBytes.Insert(offset, (byte)controlB);
                                            controlB = 0;
                                            offset = compressedBytes.Count;
                                        }
                                        else
                                        {
                                            controlB <<= 1;
                                        }
                                    }
                                    if (controlC < 8)
                                    {
                                        // write remaining control byte
                                        controlB <<= (controlC - 1);
                                        compressedBytes.Insert(offset, (byte)controlB);
                                    }

                                    compressedOutput = compressedBytes.ToArray();
                                }
                                catch (Exception ex)
                                {
                                    System.Windows.MessageBox.Show(ex.Message, "ERROR");
                                    System.Windows.Application.Current.Shutdown();
                                }
                            }
                            catch (Exception ex)
                            {
                                System.Windows.MessageBox.Show(ex.Message, "ERROR");
                                System.Windows.Application.Current.Shutdown();
                            }
                            //Done Compressing now ask for tile index to replace

                            // Create UI elements
                            Window window = new Window
                            {
                                Title = "Info",
                                SizeToContent = SizeToContent.WidthAndHeight,
                                ResizeMode = ResizeMode.NoResize,
                                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                                Owner = System.Windows.Application.Current.MainWindow
                            };
                            TextBlock textBlock = new TextBlock() { Text = "Select Compressed Tiles Id", FontSize = 18, Foreground = System.Windows.Media.Brushes.White, FontFamily = new System.Windows.Media.FontFamily("Consolas") };
                            NumInt numInt = new NumInt() { Margin = new Thickness(5), Minimum = 1, Maximum = (count - 1), Value = 0, Width = 100, FontFamily = new System.Windows.Media.FontFamily("Consolas"), FontSize = 16 };
                            Grid.SetColumn(numInt, 1);
                            Grid.SetRow(numInt, 1);
                            System.Windows.Controls.Button confirmBtn = new System.Windows.Controls.Button() { Content = "OK", Width = 75, Height = 30, Margin = new Thickness(5) };
                            confirmBtn.Click += (se, ev) =>
                            {
                                if (numInt.Value == null)
                                    return;

                                int set = (int)numInt.Value;

                                int addr_R = SNES.CpuToOffset(BitConverter.ToInt32(rom, (set * 5) + romOffset));
                                int size = BitConverter.ToUInt16(rom, set * 5 + romOffset + 3);
                                byte[] data = new byte[size];

                                byte controlB = rom[addr_R];
                                addr_R++;
                                int dataLength = 1;
                                byte controlC = 8;

                                while (true)
                                {
                                    if ((controlB & 0x80) == 0)
                                    {
                                        dataLength++;
                                        addr_R++;
                                        size--;
                                    }
                                    else // Copy from Window
                                    {
                                        int windowPosition = (rom[addr_R] & 3) << 8;
                                        windowPosition |= rom[addr_R + 1];
                                        int length = rom[addr_R] >> 2;

                                        size -= length;
                                        addr_R += 2;
                                        dataLength += 2;
                                    }
                                    controlB <<= 1;
                                    controlC--;

                                    if (size < 1)
                                        break;

                                    if (controlC == 0)
                                    {
                                        //Reload Control Byte
                                        controlB = rom[addr_R];
                                        addr_R++;
                                        dataLength++;
                                        controlC = 8;
                                    }
                                }

                                if (compressedOutput.Length > dataLength)
                                {
                                    System.Windows.MessageBox.Show($"New compressed data is larger than the original!\n{compressedOutput.Length:X} vs {dataLength:X}", "ERROR");
                                    return;
                                }

                                addr_R = SNES.CpuToOffset(BitConverter.ToInt32(rom, (set * 5) + romOffset));
                                BinaryPrimitives.WriteUInt16LittleEndian(rom.AsSpan((set * 5) + romOffset + 3), (ushort)inputData.Length);
                                Array.Copy(compressedOutput, 0, rom, addr_R, compressedOutput.Length);
                                File.WriteAllBytes(fd.FileName, rom);

                                System.Windows.MessageBox.Show("Tile Data Replaced!");
                                window.Close();
                            };
                            Grid.SetRow(confirmBtn, 1);
                            Grid grid = new Grid();
                            grid.Background = System.Windows.Media.Brushes.Black;
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
                    }
                }
            }
        }
        /*
        *  MegaMan X3 Tools Events
        */
        //...
        #endregion Events
    }
}
