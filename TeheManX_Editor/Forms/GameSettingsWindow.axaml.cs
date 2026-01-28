using Avalonia.Controls;
using Avalonia.Interactivity;
using System;
using System.Buffers.Binary;
using System.IO;

namespace TeheManX_Editor.Forms;

public partial class GameSettingsWindow : Window
{
    #region Properties
    bool enable;
    string saveLocation;
    byte[] rom;
    Const.GameId gameId;
    Const.GameVersion gameVersion;
    int stageId;

    //Offsets
    int CapsulePositionOffset;
    int CapsuleArmorIndexesOffset;
    int UpgradeMovementOffset;
    int CapsuleCameraPositionOffset;
    int CapsuleTextOffset;

    int[] HadoukenAsmOffsets;
    #endregion Properties

    #region Constructors
    public GameSettingsWindow(string path, byte[] rom, int id, int version)
    {
        InitializeComponent();
        this.saveLocation = path;
        this.rom = rom;
        this.gameId = (Const.GameId)id;
        this.gameVersion = (Const.GameVersion)version;

        switch (gameId)
        {
            case Const.GameId.MegaManX:
                hadoukenStageInt.Value = rom[Const.MegaManX.HadoukenAsmOffsets[0]];
                hadoukenStageInt.Maximum = 8;
                stageInt.Maximum = 8;
                unkownPannel.IsVisible = false;
                fixPannelX2.IsVisible = false;

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
                revistInt.Value = rom[Const.MegaManX.RevistsAsmOffset[0]];
                break;
            case Const.GameId.MegaManX2:
                hadoukenStageInt.Maximum = 0xB;
                stageInt.Maximum = 0xB;
                hadoukenVistPannel.IsVisible = false;
                unkownPannel.IsVisible = false;
                hadoukenStageLbl.Content = "Shoruken Stage";

                if (gameVersion == Const.GameVersion.NA)
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
                hadoukenStageInt.Value = rom[HadoukenAsmOffsets[0]];
                break;
            case Const.GameId.MegaManX3:
                hadoukenStageInt.Value = rom[Const.MegaManX3.GoldenArmorIdOffset];
                hadoukenStageInt.Maximum = 0xA;
                stageInt.Maximum = 0xA;
                hadoukenVistPannel.IsVisible = false;
                fixPannelX2.IsVisible = false;
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
        int positionOffset = CapsulePositionOffset + (stageId * 4);
        positionIntX.Value = BinaryPrimitives.ReadUInt16LittleEndian(rom.AsSpan(positionOffset));
        positionIntY.Value = BinaryPrimitives.ReadUInt16LittleEndian(rom.AsSpan(positionOffset + 2));
        int cameraOffset = CapsuleCameraPositionOffset + (stageId * 4);
        cameraIntX.Value = BinaryPrimitives.ReadUInt16LittleEndian(rom.AsSpan(cameraOffset));
        cameraIntY.Value = BinaryPrimitives.ReadUInt16LittleEndian(rom.AsSpan(cameraOffset + 2));

        armorIndexInt.Value = rom[CapsuleArmorIndexesOffset + stageId];
        if (gameId != Const.GameId.MegaManX3)
            textBoxIdInt.Value = rom[CapsuleTextOffset + stageId];
        else
        {
            textBoxIdInt.Value = rom[CapsuleTextOffset + stageId * 2];
            unkownInt.Value = rom[CapsuleTextOffset + stageId * 2 + 1];
        }

        movementIndexInt.Value = rom[UpgradeMovementOffset + stageId];
        enable = true;
    }
    #endregion Methods

    #region Events
    private async void helpBtn_Click(object sender, RoutedEventArgs e)
    {
        HelpWindow helpWindow = new HelpWindow(5);
        await helpWindow.ShowDialog(this);
    }
    private async void applyFixBtn_Click(object sender, RoutedEventArgs e)
    {
        if (gameId != Const.GameId.MegaManX2 || !enable) return;
        int offset = gameVersion == Const.GameVersion.NA ? Const.MegaManX2.NA.OstrichIdOffset : Const.MegaManX2.JP.OstrichIdOffset;
        rom[offset] = 0xFF;
        await MessageBox.Show(this, "Ostrich Fix Applied!");
    }
    private void hadoukenStageInt_ValueChanged(object sender, int e)
    {
        if (!enable) return;
        foreach (int offset in HadoukenAsmOffsets)
            rom[offset] = (byte)(int)hadoukenStageInt.Value;
    }
    private void revistInt_ValueChanged(object sender, int e)
    {
        if (!enable) return;
        foreach (int offset in Const.MegaManX.RevistsAsmOffset)
            rom[offset] = (byte)(int)revistInt.Value;
    }
    private void stageInt_ValueChanged(object sender, int e)
    {
        if (!enable) return;
        stageId = e;
        SetCapsuleStageSettings();
    }
    private void positionIntX_ValueChanged(object sender, int e)
    {
        if (!enable) return;
        BinaryPrimitives.WriteUInt16LittleEndian(rom.AsSpan(CapsulePositionOffset + stageId * 4), (ushort)e);
    }
    private void positionIntY_ValueChanged(object sender, int e)
    {
        if (!enable) return;
        BinaryPrimitives.WriteUInt16LittleEndian(rom.AsSpan(CapsulePositionOffset + stageId * 4 + 2), (ushort)e);
    }
    private void cameraIntX_ValueChanged(object sender, int e)
    {
        if (!enable) return;
        BinaryPrimitives.WriteUInt16LittleEndian(rom.AsSpan(CapsuleCameraPositionOffset + stageId * 4), (ushort)e);
    }
    private void cameraIntY_ValueChanged(object sender, int e)
    {
        if (!enable) return;
        BinaryPrimitives.WriteUInt16LittleEndian(rom.AsSpan(CapsuleCameraPositionOffset + stageId * 4 + 2), (ushort)e);
    }
    private void armorIndexInt_ValueChanged(object sender, int e)
    {
        if (!enable) return;
        rom[CapsuleArmorIndexesOffset + stageId] = (byte)e;
    }
    private void textBoxIdInt_ValueChanged(object sender, int e)
    {
        if (!enable) return;
        rom[CapsuleTextOffset + stageId * (gameId == Const.GameId.MegaManX3 ? 2 : 1)] = (byte)e;
    }
    private void movementIndexInt_ValueChanged(object sender, int e)
    {
        if (!enable) return;
        rom[UpgradeMovementOffset + stageId] = (byte)e;
    }
    private void unkownInt_ValueChanged(object sender, int e)
    {
        if (!enable) return;
        rom[CapsuleTextOffset + stageId * 2 + 1] = (byte)e;
    }
    private async void SaveBtn_Click(object sender, RoutedEventArgs e)
    {
        await File.WriteAllBytesAsync(saveLocation, rom);
        await MessageBox.Show(this, "Changes Saved!");
    }
    #endregion Events
}