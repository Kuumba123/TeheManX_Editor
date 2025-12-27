using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Text;
using System.Windows;

namespace TeheManX_Editor.Forms
{
    /// <summary>
    /// Interaction logic for ProjectWindow.xaml
    /// </summary>
    public partial class ProjectWindow : Window
    {
        #region Constructors
        public ProjectWindow()
        {
            InitializeComponent();

            if (SNES.rom.Length >= 0x400000 && Encoding.ASCII.GetString(SNES.rom, 0x3FFFF0, 6) == "POGYOU")
                expandMB4Grid.Visibility = Visibility.Collapsed;
        }
        #endregion Constructors

        #region Events
        private void expandBtn_Click(object sender, RoutedEventArgs e)
        {
            if (MainWindow.window.screenE.mode16)
            {
                MessageBox.Show("You must exit 16x16 mode if you want to enable the 4MB expansion!");
                return;
            }

            SNES.rom[0x7FD7] = 0x12;
            Array.Resize(ref SNES.rom, 0x400000);
            Array.Copy(Encoding.ASCII.GetBytes("POGYOU"), 0, SNES.rom, 0x3FFFF0, 6);

            int dumpOffset;
            int addrMask = 0;
            int bankCount;
            if (Const.Id == Const.GameId.MegaManX)
            {
                dumpOffset = Const.MegaManX.BankCount * 0x8000;
                addrMask = 0x800000;
                bankCount = Const.MegaManX.BankCount;
            }
            else if (Const.Id == Const.GameId.MegaManX2)
            {
                dumpOffset = Const.MegaManX2.BankCount * 0x8000;
                bankCount = Const.MegaManX2.BankCount;
            }
            else
            {
                dumpOffset = Const.MegaManX3.BankCount * 0x8000;
                bankCount = Const.MegaManX3.BankCount;
            }

            int dumpAddr = 0;

            {
                int pointerBase = (SNES.OffsetToCpu(Const.EnemyPointersOffset) & 0x7FFF) + bankCount * 0x8000;
                int startWrite;

                //for X1 actual enemy data should be at end of bank other games should have it at start
                if (Const.Id == Const.GameId.MegaManX)
                    startWrite = pointerBase;
                else
                    startWrite = bankCount * 0x8000;

                //Assign 16-bit Enemy Data Pointers
                for (int i = 0; i < Const.PlayableLevelsCount; i++)
                {
                    if (Const.Id == Const.GameId.MegaManX3 && i > 0xE) break;

                    int writeOffset = pointerBase + i * 2;

                    if (Const.Id == Const.GameId.MegaManX3 && i >= 0xC)
                        dumpAddr = pointerBase + (Const.MegaManX3.LevelsCount - 2) * 2 + (i - 0xC) * 0xCC + 1 * (i - 0xC);
                    else
                        dumpAddr = SNES.OffsetToCpu(i * 0xCC * 8 + 1 * i + startWrite + Const.PlayableLevelsCount * 2);

                    BinaryPrimitives.WriteUInt16LittleEndian(SNES.rom.AsSpan(writeOffset), (ushort)(dumpAddr & 0xFFFF));
                }
                //Update Enemy Pointer
                Const.EnemyPointersOffset = (SNES.OffsetToCpu(Const.EnemyPointersOffset) & 0x7FFF) + bankCount * 0x8000;
            }

            //Write Enemy Data Banks
            byte oraBank = (byte)(bankCount | (addrMask >> 16));
            SNES.rom[Const.EnemyBankAsmOffsets[0]] = oraBank;
            SNES.rom[Const.EnemyBankAsmOffsets[1]] = oraBank;
            SNES.rom[Const.EnemyBankAsmOffsets[2]] = oraBank;


            //Set Dump Offset for Start of 16x16 Tile Data & Layout Data
            if (Const.Id == Const.GameId.MegaManX3)
                dumpOffset = Const.MegaManX3.FreeBanks[0] * 0x8000;
            else
                dumpOffset += 0x8000 - (dumpOffset % 0x8000); //Increament Dump Offset to Next Bank Unconditionally

            //Dump 16x16 Tile Data and the Layout Data 1st!

            List<byte[]> tileData16 = new List<byte[]>();
            List<byte[]> screenData = new List<byte[]>();
            List<byte[]> screenData2 = new List<byte[]>();
            List<byte[]> tile32Data = new List<byte[]>();
            List<byte[]> tile32Data2 = new List<byte[]>();
            List<byte[]> tileCollision = new List<byte[]>();

            for (int i = 0; i < Const.PlayableLevelsCount; i++)
            {
                tileData16.Add(new byte[Const.Tile16Count[i, 0] * 8]);
                screenData.Add(new byte[Const.ScreenCount[i, 0] * 0x80]);
                screenData2.Add(new byte[Const.ScreenCount[i, 1] * 0x80]);
                tile32Data.Add(new byte[Const.Tile32Count[i, 0] * 8]);
                tile32Data2.Add(new byte[Const.Tile32Count[i, 1] * 8]);
                tileCollision.Add(new byte[Const.Tile16Count[i, 0]]);

                int id;
                if (Const.Id == Const.GameId.MegaManX3 && i == 0xE) id = 0x10; //special case for MMX3 rekt version of dophler 2
                else if (Const.Id == Const.GameId.MegaManX3 && i > 0xE) id = (i - 0xF) + 0xE; //Buffalo or Beetle
                else id = i;

                Array.Copy(SNES.rom, SNES.CpuToOffset(BitConverter.ToInt32(SNES.rom, Const.Tile16DataPointersOffset[0] + id * 3)), tileData16[i], 0, Const.Tile16Count[i, 0] * 8);
                Array.Copy(SNES.rom, SNES.CpuToOffset(BitConverter.ToInt32(SNES.rom, Const.ScreenDataPointersOffset[0] + id * 3)), screenData[i], 0, Const.ScreenCount[i, 0] * 0x80);
                Array.Copy(SNES.rom, SNES.CpuToOffset(BitConverter.ToInt32(SNES.rom, Const.ScreenDataPointersOffset[1] + id * 3)), screenData2[i], 0, Const.ScreenCount[i, 1] * 0x80);
                Array.Copy(SNES.rom, SNES.CpuToOffset(BitConverter.ToInt32(SNES.rom, Const.Tile32DataPointersOffset[0] + id * 3)), tile32Data[i], 0, Const.Tile32Count[i, 0] * 8);
                Array.Copy(SNES.rom, SNES.CpuToOffset(BitConverter.ToInt32(SNES.rom, Const.Tile32DataPointersOffset[1] + id * 3)), tile32Data2[i], 0, Const.Tile32Count[i, 1] * 8);
                Array.Copy(SNES.rom, SNES.CpuToOffset(BitConverter.ToInt32(SNES.rom, Const.TileCollisionDataPointersOffset + id * 3)), tileCollision[i], 0, Const.Tile16Count[i, 0]);
            }


            for (int i = 0; i < Const.PlayableLevelsCount; i++)
            {
                if (Const.Id == Const.GameId.MegaManX3 && i > 0xE) //Buffalo or Beetle
                {
                    int dumpId = (i - 0xF) + 0xE;
                    int readId = (i - 0xF) + 2;

                    //Assign Buffalo/Beetle Alternative stage to use the same 16x16 Tile Data as the orignal
                    int pointer24 = BinaryPrimitives.ReadInt32LittleEndian(SNES.rom.AsSpan(Const.Tile16DataPointersOffset[0] + readId * 3));
                    BinaryPrimitives.WriteUInt16LittleEndian(SNES.rom.AsSpan(Const.Tile16DataPointersOffset[0] + dumpId * 3), (ushort)(pointer24 & 0xFFFF));
                    SNES.rom[Const.Tile16DataPointersOffset[0] + dumpId * 3 + 2] = (byte)(pointer24 >> 16);

                    pointer24 = BinaryPrimitives.ReadInt32LittleEndian(SNES.rom.AsSpan(Const.Tile16DataPointersOffset[1] + readId * 3));
                    BinaryPrimitives.WriteUInt16LittleEndian(SNES.rom.AsSpan(Const.Tile16DataPointersOffset[1] + dumpId * 3), (ushort)(pointer24 & 0xFFFF));
                    SNES.rom[Const.Tile16DataPointersOffset[1] + dumpId * 3 + 2] = (byte)(pointer24 >> 16);

                    dumpAddr = SNES.OffsetToCpu(dumpOffset);

                    BinaryPrimitives.WriteUInt16LittleEndian(SNES.rom.AsSpan(Const.LayoutPointersOffset[0] + dumpId * 3), (ushort)(dumpAddr & 0xFFFF));
                    SNES.rom[Const.LayoutPointersOffset[0] + dumpId * 3 + 2] = (byte)(dumpAddr >> 16);

                    dumpOffset += Const.ExpandLayoutLength;
                    dumpAddr += Const.ExpandLayoutLength;

                    BinaryPrimitives.WriteUInt16LittleEndian(SNES.rom.AsSpan(Const.LayoutPointersOffset[1] + dumpId * 3), (ushort)(dumpAddr & 0xFFFF));
                    SNES.rom[Const.LayoutPointersOffset[1] + dumpId * 3 + 2] = (byte)(dumpAddr >> 16);

                    dumpOffset += Const.ExpandLayoutLength;
                    dumpAddr += Const.ExpandLayoutLength;
                }
                else
                {
                    int id = i;

                    //Dump Existing 16x16 Tile Data
                    Array.Copy(tileData16[i], 0, SNES.rom, dumpOffset, Const.Tile16Count[i, 0] * 8);
                    dumpAddr = SNES.OffsetToCpu(dumpOffset) | addrMask;

                    BinaryPrimitives.WriteUInt16LittleEndian(SNES.rom.AsSpan(Const.Tile16DataPointersOffset[0] + id * 3), (ushort)(dumpAddr & 0xFFFF));
                    SNES.rom[Const.Tile16DataPointersOffset[0] + id * 3 + 2] = (byte)((dumpAddr >> 16) & 0xFF);
                    BinaryPrimitives.WriteUInt16LittleEndian(SNES.rom.AsSpan(Const.Tile16DataPointersOffset[1] + id * 3), (ushort)(dumpAddr & 0xFFFF));
                    SNES.rom[Const.Tile16DataPointersOffset[1] + id * 3 + 2] = (byte)((dumpAddr >> 16) & 0xFF);

                    //Increament Dump Offset
                    dumpOffset += Const.ExpandMaxTiles16 * 8;

                    //Assign Layouts the New Pointers (Layer 1)
                    dumpAddr = SNES.OffsetToCpu(dumpOffset) | addrMask;
                    BinaryPrimitives.WriteUInt16LittleEndian(SNES.rom.AsSpan(Const.LayoutPointersOffset[0] + id * 3), (ushort)(dumpAddr & 0xFFFF));
                    SNES.rom[Const.LayoutPointersOffset[0] + id * 3 + 2] = (byte)((dumpAddr >> 16) & 0xFF);

                    //Increament Dump Offset
                    dumpOffset += Const.ExpandLayoutLength;

                    //Layer 2
                    dumpAddr = SNES.OffsetToCpu(dumpOffset) | addrMask;
                    BinaryPrimitives.WriteUInt16LittleEndian(SNES.rom.AsSpan(Const.LayoutPointersOffset[1] + id * 3), (ushort)(dumpAddr & 0xFFFF));
                    SNES.rom[Const.LayoutPointersOffset[1] + id * 3 + 2] = (byte)((dumpAddr >> 16) & 0xFF);

                    //Increament Dump Offset
                    dumpOffset += Const.ExpandLayoutLength;

                    if (Const.Id == Const.GameId.MegaManX3)
                    {
                        if ((i & 1) != 0)
                            dumpOffset = Const.MegaManX3.FreeBanks[(i + 1) / 2] * 0x8000;
                    }
                    else
                    {
                        //Increament Dump Offset to Next Bank
                        if ((i & 1) != 0 && (dumpOffset % 0x8000) != 0)
                            dumpOffset += 0x8000 - (dumpOffset % 0x8000);
                    }
                }
            }
            /*End of Loop*/


            //Then Dump the rest of the Data
            if (Const.Id == Const.GameId.MegaManX3)
                dumpOffset = (Const.MegaManX3.BankCount + 1) * 0x8000;
            else // MegaMan X1 & X2
            {
                //Just in case we are not at the start of a bank
                if ((dumpOffset % 0x8000) != 0)
                    dumpOffset += 0x8000 - (dumpOffset % 0x8000);
            }

            for (int i = 0; i < Const.PlayableLevelsCount; i++)
            {
                int id;
                if (Const.Id == Const.GameId.MegaManX3 && i == 0xE) id = 0x10; //special case for MMX3 rekt version of dophler 2
                else if (Const.Id == Const.GameId.MegaManX3 && i > 0xE) id = (i - 0xF) + 0xE; //Buffalo or Beetle
                else id = i;

                //Dump Existing Screen Data
                Array.Copy(screenData[i], 0, SNES.rom, dumpOffset, Const.ScreenCount[i, 0] * 0x80);
                //Update Pointer
                dumpAddr = SNES.OffsetToCpu(dumpOffset) | addrMask;
                BinaryPrimitives.WriteUInt16LittleEndian(SNES.rom.AsSpan(Const.ScreenDataPointersOffset[0] + id * 3), (ushort)(dumpAddr & 0xFFFF));
                SNES.rom[Const.ScreenDataPointersOffset[0] + id * 3 + 2] = (byte)((dumpAddr >> 16) & 0xFF);


                //Increament Dump Offset
                {
                    int maxScreens;
                    if (Const.Id == Const.GameId.MegaManX)
                        maxScreens = Const.ExpandMaxScreens[0];
                    else
                        maxScreens = Const.ExpandMaxScreens2[0];
                    dumpOffset += maxScreens * 0x80;
                }

                //Dump Existing Screen Data (Layer 2)
                Array.Copy(screenData2[i], 0, SNES.rom, dumpOffset, Const.ScreenCount[i, 1] * 0x80);
                //Update Pointer
                dumpAddr = SNES.OffsetToCpu(dumpOffset) | addrMask;
                BinaryPrimitives.WriteUInt16LittleEndian(SNES.rom.AsSpan(Const.ScreenDataPointersOffset[1] + id * 3), (ushort)(dumpAddr & 0xFFFF));
                SNES.rom[Const.ScreenDataPointersOffset[1] + id * 3 + 2] = (byte)((dumpAddr >> 16) & 0xFF);


                //Increament Dump Offset
                dumpOffset += Const.ExpandMaxScreens[1] * 0x80;

                //Dump Existing 32x32 Tile Data
                Array.Copy(tile32Data[i], 0, SNES.rom, dumpOffset, Const.Tile32Count[i, 0] * 8);
                //Update Pointer
                dumpAddr = SNES.OffsetToCpu(dumpOffset) | addrMask;
                BinaryPrimitives.WriteUInt16LittleEndian(SNES.rom.AsSpan(Const.Tile32DataPointersOffset[0] + id * 3), (ushort)(dumpAddr & 0xFFFF));
                SNES.rom[Const.Tile32DataPointersOffset[0] + id * 3 + 2] = (byte)((dumpAddr >> 16) & 0xFF);

                //Increament Dump Offset
                dumpOffset += Const.ExpandMaxTiles32[0] * 8;

                //Dump Existing 32x32 Tile Data (Layer 2)
                Array.Copy(tile32Data2[i], 0, SNES.rom, dumpOffset, Const.Tile32Count[i, 1] * 8);
                dumpAddr = SNES.OffsetToCpu(dumpOffset) | addrMask;
                BinaryPrimitives.WriteUInt16LittleEndian(SNES.rom.AsSpan(Const.Tile32DataPointersOffset[1] + id * 3), (ushort)(dumpAddr & 0xFFFF));
                SNES.rom[Const.Tile32DataPointersOffset[1] + id * 3 + 2] = (byte)((dumpAddr >> 16) & 0xFF);

                //Increament Dump Offset
                dumpOffset += Const.ExpandMaxTiles32[1] * 8;

                //Dump Existing 16x16 Tile Collision Data
                Array.Copy(tileCollision[i], 0, SNES.rom, dumpOffset, Const.Tile16Count[i, 0]);
                dumpAddr = SNES.OffsetToCpu(dumpOffset) | addrMask;
                BinaryPrimitives.WriteUInt16LittleEndian(SNES.rom.AsSpan(Const.TileCollisionDataPointersOffset + id * 3), (ushort)(dumpAddr & 0xFFFF));
                SNES.rom[Const.TileCollisionDataPointersOffset + id * 3 + 2] = (byte)((dumpAddr >> 16) & 0xFF);

                //Increament Dump Offset
                dumpOffset += Const.ExpandMaxTiles16;

                //Increament Dump Offset to Next Bank
                if ((dumpOffset % 0x8000) != 0)
                    dumpOffset += 0x8000 - (dumpOffset % 0x8000);
            }
            /*End of Loop*/

            //Done
            SNES.edit = true;
            SNES.expanded = true;
            Const.AssignExpand();
            MainWindow.window.layoutE.AssignLimits();
            MainWindow.window.screenE.AssignLimits();
            MainWindow.window.tile32E.AssignLimits();
            MainWindow.window.tile16E.AssignLimits();
            MessageBox.Show("Expansion Applied for Layout , Screen , 32x32 , 16x16 Enemy tabs!");
            expandMB4Grid.Visibility = Visibility.Collapsed;
        }
        #endregion Events
    }
}
