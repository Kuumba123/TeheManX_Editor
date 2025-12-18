using System;
using System.Buffers.Binary;
using System.IO;
using System.Windows;

namespace TeheManX_Editor.Forms
{
    /// <summary>
    /// Interaction logic for GameSettingsWindow.xaml
    /// </summary>
    public partial class GameSettingsWindow : Window
    {
        #region Properties
        bool enable;
        string saveLocation;
        byte[] rom;
        Const.GameId gameId;
        Const.GameVersion gameVersion;

        //Offsets
        int CapsulePositionOffset;
        int CapsuleArmorIndexesOffset;
        int UpgradeMovementOffset;
        int CapsuleCameraPositionOffset;
        int CapsuleTextOffset;

        int[] HadoukenAsmOffsets;
        #endregion Properties

        #region Constructors
        public GameSettingsWindow(string path,byte[] rom,int id,int version)
        {
            InitializeComponent();
            this.saveLocation = path;
            this.rom = rom;
            this.gameId = (Const.GameId)id;
            this.gameVersion = (Const.GameVersion)version;

            switch (gameId)
            {
                case Const.GameId.MegaManX:
                    hadoukenStageInt.Maximum = 8;
                    stageInt.Maximum = 8;
                    unkownInt.Visibility = Visibility.Collapsed;
                    fixPannelX2.Visibility = Visibility.Collapsed;

                    if (this.gameVersion == Const.GameVersion.NA)
                    {
                        CapsulePositionOffset = Const.MegaManX.NA.CapsulePositionOffset;
                        CapsuleArmorIndexesOffset = Const.MegaManX.NA.CapsuleArmorIndexesOffset;
                        UpgradeMovementOffset = Const.MegaManX.NA.UpgradeMovementOffset;
                        CapsuleCameraPositionOffset = Const.MegaManX.NA.CapsuleCameraPositionOffset;
                        CapsuleTextOffset = Const.MegaManX.NA.CapsuleTextOffset;
                    }
                    else
                    {
                        CapsulePositionOffset = Const.MegaManX.JP.CapsulePositionOffset;
                        CapsuleArmorIndexesOffset = Const.MegaManX.JP.CapsuleArmorIndexesOffset;
                        UpgradeMovementOffset = Const.MegaManX.JP.UpgradeMovementOffset;
                        CapsuleCameraPositionOffset = Const.MegaManX.JP.CapsuleCameraPositionOffset;
                        CapsuleTextOffset = Const.MegaManX.JP.CapsuleTextOffset;
                    }
                    HadoukenAsmOffsets = Const.MegaManX.HadoukenAsmOffsets;
                    revistInt.Value = SNES.rom[Const.MegaManX.RevistsAsmOffset[0]];
                    break;
                case Const.GameId.MegaManX2:
                    hadoukenStageInt.Maximum = 0xB;
                    stageInt.Maximum = 0xB;
                    hadoukenVistPannel.Visibility = Visibility.Collapsed;
                    unkownInt.Visibility = Visibility.Collapsed;
                    hadoukenStageLbl.Content = "Shoruken Stage";

                    if (this.gameVersion == Const.GameVersion.NA)
                    {
                        CapsulePositionOffset = Const.MegaManX2.NA.CapsulePositionOffset;
                        CapsuleArmorIndexesOffset = Const.MegaManX2.NA.CapsuleArmorIndexesOffset;
                        UpgradeMovementOffset = Const.MegaManX2.NA.UpgradeMovementOffset;
                        CapsuleCameraPositionOffset = Const.MegaManX2.NA.CapsuleCameraPositionOffset;
                        CapsuleTextOffset = Const.MegaManX2.NA.CapsuleTextOffset;
                        HadoukenAsmOffsets = Const.MegaManX2.NA.ShoryukenAsmOffsets;
                    }
                    else
                    {
                        CapsulePositionOffset = Const.MegaManX2.JP.CapsulePositionOffset;
                        CapsuleArmorIndexesOffset = Const.MegaManX2.JP.CapsuleArmorIndexesOffset;
                        UpgradeMovementOffset = Const.MegaManX2.JP.UpgradeMovementOffset;
                        CapsuleCameraPositionOffset = Const.MegaManX2.JP.CapsuleCameraPositionOffset;
                        CapsuleTextOffset = Const.MegaManX2.JP.CapsuleTextOffset;
                        HadoukenAsmOffsets = Const.MegaManX2.JP.ShoryukenAsmOffsets;
                    }
                    break;
                case Const.GameId.MegaManX3:
                    hadoukenStageInt.Maximum = 0xA;
                    stageInt.Maximum = 0xA;
                    hadoukenVistPannel.Visibility = Visibility.Collapsed;
                    fixPannelX2.Visibility = Visibility.Collapsed;
                    hadoukenStageLbl.Content = "Gold Armor Stage";

                    if (this.gameVersion == Const.GameVersion.NA)
                    {
                        CapsulePositionOffset = Const.MegaManX3.NA.CapsulePositionOffset;
                        CapsuleArmorIndexesOffset = Const.MegaManX3.NA.CapsuleArmorIndexesOffset;
                        UpgradeMovementOffset = Const.MegaManX3.NA.UpgradeMovementOffset;
                        CapsuleCameraPositionOffset = Const.MegaManX3.NA.CapsuleCameraPositionOffset;
                        CapsuleTextOffset = Const.MegaManX3.NA.CapsuleTextOffset;
                    }
                    else
                    {
                        CapsulePositionOffset = Const.MegaManX3.JP.CapsulePositionOffset;
                        CapsuleArmorIndexesOffset = Const.MegaManX3.JP.CapsuleArmorIndexesOffset;
                        UpgradeMovementOffset = Const.MegaManX3.JP.UpgradeMovementOffset;
                        CapsuleCameraPositionOffset = Const.MegaManX3.JP.CapsuleCameraPositionOffset;
                        CapsuleTextOffset = Const.MegaManX3.JP.CapsuleTextOffset;
                    }
                    HadoukenAsmOffsets = new int[1];
                    HadoukenAsmOffsets[0] = Const.MegaManX3.GoldenArmorIdOffset;
                    break;
            }
            SetCapsuleStageSettings();
        }
        #endregion Constructors

        #region Methods
        private void SetCapsuleStageSettings()
        {
            enable = false;
            int positionOffset = CapsulePositionOffset + (stageInt.Value.Value * 4);
            positionIntX.Value = BinaryPrimitives.ReadUInt16LittleEndian(rom.AsSpan(positionOffset));
            positionIntY.Value = BinaryPrimitives.ReadUInt16LittleEndian(rom.AsSpan(positionOffset + 2));
            int cameraOffset = CapsuleCameraPositionOffset + (stageInt.Value.Value * 4);
            cameraIntX.Value = BinaryPrimitives.ReadUInt16LittleEndian(rom.AsSpan(cameraOffset));
            cameraIntY.Value = BinaryPrimitives.ReadUInt16LittleEndian(rom.AsSpan(cameraOffset + 2));

            armorIndexInt.Value = rom[CapsuleArmorIndexesOffset + stageInt.Value.Value];
            if (gameId != Const.GameId.MegaManX3)
                textBoxIdInt.Value = rom[CapsuleTextOffset + stageInt.Value.Value];
            else
            {
                textBoxIdInt.Value = rom[CapsuleTextOffset + stageInt.Value.Value * 2];
                unkownInt.Value = rom[CapsuleTextOffset + stageInt.Value.Value * 2 + 1];
            }

            movementIndexInt.Value = rom[UpgradeMovementOffset + stageInt.Value.Value];
            enable = true;
        }
        #endregion Methods

        #region Events
        private void helpBtn_Click(object sender, RoutedEventArgs e)
        {

        }
        private void applyFixBtn_Click(object sender, RoutedEventArgs e)
        {
            if (gameId != Const.GameId.MegaManX2 || !enable) return;
            int offset = gameVersion == Const.GameVersion.NA ? Const.MegaManX2.NA.OstrichIdOffset : Const.MegaManX2.JP.OstrichIdOffset;
            SNES.rom[offset] = 0xFF;
            MessageBox.Show("Ostrich Fix Applied!");
        }
        private void hadoukenStageInt_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (!enable || e.NewValue == null) return;
            foreach (int offset in HadoukenAsmOffsets)
                rom[offset] = (byte)hadoukenStageInt.Value.Value;
        }
        private void revistInt_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (!enable || e.NewValue == null) return;
            foreach (int offset in Const.MegaManX.RevistsAsmOffset)
                rom[offset] = (byte)revistInt.Value.Value;
        }
        private void stageInt_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (!enable || e.NewValue == null) return;
            SetCapsuleStageSettings();
        }
        private void positionIntX_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (!enable || e.NewValue == null) return;
            BinaryPrimitives.WriteUInt16LittleEndian(rom.AsSpan(CapsulePositionOffset + stageInt.Value.Value * 4), (ushort)e.NewValue);
        }
        private void positionIntY_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (!enable || e.NewValue == null) return;
            BinaryPrimitives.WriteUInt16LittleEndian(rom.AsSpan(CapsulePositionOffset + stageInt.Value.Value * 4 + 2), (ushort)e.NewValue);
        }
        private void cameraIntX_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (!enable || e.NewValue == null) return;
            BinaryPrimitives.WriteUInt16LittleEndian(rom.AsSpan(CapsuleCameraPositionOffset + stageInt.Value.Value * 4), (ushort)e.NewValue);
        }
        private void cameraIntY_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (!enable || e.NewValue == null) return;
            BinaryPrimitives.WriteUInt16LittleEndian(rom.AsSpan(CapsuleCameraPositionOffset + stageInt.Value.Value * 4 + 2), (ushort)e.NewValue);
        }
        private void armorIndexInt_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (!enable || e.NewValue == null) return;
            rom[CapsuleArmorIndexesOffset + stageInt.Value.Value] = (byte)e.NewValue;
        }
        private void textBoxIdInt_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (!enable || e.NewValue == null) return;
            rom[CapsuleTextOffset + stageInt.Value.Value * (gameId == Const.GameId.MegaManX3 ? 2 : 1)] = (byte)e.NewValue;
        }
        private void movementIndexInt_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (!enable || e.NewValue == null) return;
            rom[UpgradeMovementOffset + stageInt.Value.Value] = (byte)e.NewValue;
        }
        private void unkownInt_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (!enable || e.NewValue == null) return;
            unkownInt.Value = rom[CapsuleTextOffset + stageInt.Value.Value * 2 + 1];
        }
        private void SaveBtn_Click(object sender, RoutedEventArgs e)
        {
            File.WriteAllBytes(saveLocation, rom);
            MessageBox.Show("Changes Saved!");
        }
        #endregion Events
    }
}
