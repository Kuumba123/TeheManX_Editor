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
        #region Properties
        bool edited;
        bool enable;
        #endregion Properties

        #region Constructors
        public ProjectWindow()
        {
            InitializeComponent();
            enable = true;
        }
        #endregion Constructors

        #region Events
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!edited)
                return;

            if (enemyOffsetInt.Value == null)
            {
                MessageBox.Show("Invalid Enemy Offset", "ERROR");
                e.Cancel = true;
                return;
            }
            if (paletteOffsetInt.Value == null)
            {
                MessageBox.Show("Invalid Palette Swap Info Offset", "ERROR");
                e.Cancel = true;
                return;
            }

            if (paletteBankInt.Value == null)
            {
                MessageBox.Show("Invalid Palette Swap Bank", "ERROR");
                e.Cancel = true;
                return;
            }

            if (checkpointInt.Value == null)
            {
                MessageBox.Show("Invalid Checkpoint Offset", "ERROR");
                e.Cancel = true;
                return;
            }
            if (cameraTriggerInt.Value == null)
            {
                MessageBox.Show("Invalid Camera Trigger Offset", "ERROR");
                e.Cancel = true;
                return;
            }
            if (cameraBorderInt.Value == null)
            {
                MessageBox.Show("Invalid Camera Border Offset", "ERROR");
                e.Cancel = true;
                return;
            }
            if (bgTileInt.Value == null)
            {
                MessageBox.Show("Invalid Background Tile Setting Info Offset", "ERROR");
                e.Cancel = true;
                return;
            }
            if (objTileInt.Value == null)
            {
                MessageBox.Show("Invalid Object Tile Setting Info Offset", "ERROR");
                e.Cancel = true;
                return;
            }
            if (MessageBox.Show("Are sure your okay with this configuration?", "WARNING", MessageBoxButton.YesNo) != MessageBoxResult.Yes)
            {
                e.Cancel = true;
                return;
            }

            //Enemy
            if (enemyCheck.IsChecked == true)
            {
                Level.Project.Enemies = Level.Enemies;
                Level.Project.EnemyOffset = (int)enemyOffsetInt.Value;
            }
            else
                Level.Project.Enemies = null;

            //Palette
            if (paletteCheck.IsChecked == true)
            {
                Level.Project.BGPalettes = PaletteEditor.BGPalettes;
                Level.Project.PaletteInfoOffset = (int)paletteOffsetInt.Value;
                Level.Project.PaletteColorBank = (int)paletteBankInt.Value;
            }
            else
            {
                Level.Project.BGPalettes = null;
                MainWindow.window.paletteE.CollectData();
            }
            MainWindow.window.paletteE.AssignLimits();

            //Checkpoint
            if (checkpointCheck.IsChecked == true)
            {
                Level.Project.Checkpoints = SpawnWindow.Checkpoints;
                Level.Project.CheckpointOffset = (int)checkpointInt.Value;
            }
            else
            {
                Level.Project.Checkpoints = null;
                MainWindow.window.spawnE.CollectData();
            }
            MainWindow.window.spawnE.SetSpawnSettings();

            //Camera
            if (cameraCheck.IsChecked == true)
            {
                Level.Project.CameraTriggers = CameraEditor.CameraTriggers;
                Level.Project.CameraTriggersOffset = (int)cameraTriggerInt.Value;
                Level.Project.CameraBordersOffset = (int)cameraBorderInt.Value;

                if (CameraEditor.CameraBorderSettings.Length < 255)
                {
                    Array.Resize(ref CameraEditor.CameraBorderSettings, 255);

                    //Fix up new Empty Entries
                    for (int i = 0; i < CameraEditor.CameraBorderSettings.Length; i++)
                    {
                        if (CameraEditor.CameraBorderSettings[i] != 0) continue;
                        CameraEditor.CameraBorderSettings[i] = Const.CameraBorderLeftWRAM;
                    }
                }
                for (int i = 0; i < 4; i++)
                    MainWindow.window.camE.borderInts[i].Maximum = 254;
                Level.Project.CameraBorderSettings = CameraEditor.CameraBorderSettings;
            }
            else
            {
                Level.Project.CameraTriggers = null;
                Array.Resize(ref CameraEditor.CameraBorderSettings, Const.MaxTotalCameraSettings); //need to resize in case they dont want json any more.
                for (int i = 0; i < 4; i++)
                    MainWindow.window.camE.borderInts[i].Maximum = Const.MaxTotalCameraSettings - 1;
                MainWindow.window.camE.borderSettingInt.Value = 0;
                MainWindow.window.camE.borderSettingInt.Maximum = Const.MaxTotalCameraSettings - 1;
                MainWindow.window.camE.CollectData();
            }
            MainWindow.window.camE.AssignTriggerLimits();

            //Background Tiles
            if (bgTileCheck.IsChecked == true)
            {
                Level.Project.BGSettings = TileEditor.BGSettings;
                Level.Project.BackgroundTilesInfoOffset = (int)bgTileInt.Value;
            }
            else
            {
                Level.Project.BGSettings = null;
                MainWindow.window.tileE.CollectBGData();
            }

            //Object Tiles
            if (objTileCheck.IsChecked == true)
            {
                Level.Project.ObjectSettings = TileEditor.ObjectSettings;
                Level.Project.ObjectTilesInfoOffset = (int)objTileInt.Value;
            }
            else
            {
                Level.Project.ObjectSettings = null;
                MainWindow.window.tileE.CollectOBJData();
            }

            MainWindow.window.tileE.AssignLimits();
            SNES.edit = true;
        }
        private void expandBtn_Click(object sender, RoutedEventArgs e)
        {
            if (MainWindow.window.screenE.mode16)
            {
                MessageBox.Show("You must exit 16x16 mode if you want to enable the 4MB expansion!");
                return;
            }
            if (SNES.rom.Length >= 0x400000 && Encoding.ASCII.GetString(SNES.rom, 0x3FFFF0, 6) == "POGYOU")
            {
                MessageBox.Show("You already have the expansion patch.");
                return;
            }

            SNES.rom[0x7FD7] = 0xC;
            Array.Resize(ref SNES.rom, 0x400000);
            Array.Copy(Encoding.ASCII.GetBytes("POGYOU"), 0, SNES.rom, 0x3FFFF0, 6);

            int dumpOffset;
            int addrMask = 0;

            if (Const.Id == Const.GameId.MegaManX)
            {
                dumpOffset = Const.MegaManX.BankCount * 0x8000;
                addrMask = 0x800000;
            }
            else if (Const.Id == Const.GameId.MegaManX2)
                dumpOffset = Const.MegaManX2.BankCount * 0x8000;
            else
                dumpOffset = Const.MegaManX3.FreeBanks[0] * 0x8000;

            int dumpAddr = 0;

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
                dumpOffset = Const.MegaManX3.BankCount * 0x8000;
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
            MessageBox.Show($"4MB expansion complete! Every bank after 0x{((dumpOffset / 0x8000) | (addrMask >> 16)):X} is availble for use!");
            MessageBox.Show("The expansion was applied for Layout , Screen , 32x32 , 16x16 tabs!");

            /*
             * TODO: ask if they want patch that removes checks 408000-40FFFF (also 1.1 has differnet offsets than 1.0)
             */
        }
        private void expansionHelpBtn_Click(object sender, RoutedEventArgs e)
        {

        }
        private void projectTrackPatchBtn_Click(object sender, RoutedEventArgs e)
        {

        }
        private void projectTrackHelpBtn_Click(object sender, RoutedEventArgs e)
        {

        }
        private void CheckBox_Changed(object sender, RoutedEventArgs e)
        {
            if (!enable)
                return;
            edited = true;
        }
        private void Int_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (!enable)
                return;
            edited = true;
        }
        #endregion Events
    }
}
