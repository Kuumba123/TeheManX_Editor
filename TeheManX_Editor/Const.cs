using System.Collections.Frozen;
using System.Collections.Generic;

namespace TeheManX_Editor
{
    static class Const
    {
        public static readonly string ReproURL = "https://api.github.com/repos/Kuumba123/TeheManX_Editor/releases/latest";
        public const string EditorVersion = "1.1";
        public static readonly string[] PastVersions =
        {
            "1.1",
            "1.0.1",
            "1.0"
        };
        public const int MaxUndo = 512;
        public const double MaxScaleUI = 8;
        public enum GameId
        {
            MegaManX,
            MegaManX2,
            MegaManX3
        }
        public enum GameVersion
        {
            NA,
            JP
        }
        public const int MaxLevels = 0x30;

        /*Expansion Constants*/
        public static readonly int[] ExpandMaxScreens = { 0x43, 0x21 };     //for MMX1 only
        public static readonly int[] ExpandMaxScreens2 = { 0x3F, 0x21 };    //for MMX2 & MMX3
        public static readonly int[] ExpandMaxTiles32 = { 0x600, 0x300 };
        public static int ExpandMaxTiles16 = 0x600; //shared between both layers
        public static int ExpandLayoutLength = 0x300; //for both layers

        /*Variable Constants*/
        public static GameId Id = 0;    //Game ID (0=MMX1, 1=MMX2, 2=MMX3)
        public static GameVersion Version = 0; //Game Version (0=US, 1=JP)

        public static int LevelsCount;
        public static int PlayableLevelsCount;

        public static int PaletteBank;
        public static int PaletteColorBank;
        public static int PaletteInfoOffset;
        public static int PaletteStageBase; //Base Id for Stage Palettes

        public static int SwapPaletteColorBank;

        public static int BackgroundPaletteOffset;
        public static int BackgroundPaletteInfoLength;

        public static int LoadTileSetBank;
        public static int LoadTileSetInfoOffset;
        public static int LoadTileSetStageBase; //Base Id for Stage Load CHR Info

        public static int CompressedTileInfoOffset;

        public static int EnemyPointersOffset;
        public static int EnemyDataBank;
        public static int TotalEnemyDataLength;

        public static int TotalLayoutDataLength;
        public static int LayoutDataOffset;

        public static int MaxTotalCheckpoints;
        public static int CheckpointOffset;
        public static int[] MaxCheckpoints = new int[MaxLevels];

        public static int CameraTriggersOffset;
        public static int CameraSettingsOffset;
        public static int CameraSettingsBank;
        public static int CameraTriggersLength;
        public static int MaxTotalCameraSettings;

        public static ushort CameraBorderLeftWRAM;
        public static ushort CameraBorderRightWRAM;
        public static ushort CameraBorderTopWRAM;
        public static ushort CameraBorderBottomWRAM;

        public static int BackgroundTileInfoOffset;
        public static int ObjectTileInfoOffset;
        public static int CompressedTilesSwapInfoOffset;

        public static int ObjectTileInfoLength;
        public static int BackgroundTileInfoLength;

        public static int ObjectSpriteInfoOffset;
        public static int SpriteArrangmentPointersOffset;

        public static int MegaManTilesOffset; //Offset to MegaMan X sprite tiles
        public static int[] MegaManGreenChargeShotTilesOffset = new int[2]; //Offset to MegaMan X Pink Green Shot sprite tiles

        public static int[] LayoutPointersOffset = new int[2]; //Layout Pointers Offset to compressed data
        public static int[] ScreenDataPointersOffset = new int[2];
        public static int[] Tile32DataPointersOffset = new int[2];
        public static int[] Tile16DataPointersOffset = new int[2];
        public static int TileCollisionDataPointersOffset;

        public static FrozenDictionary<int, ObjectIcon> EnemyIcons;
        public static FrozenDictionary<int, ObjectIcon> ItemIcons;

        public static FrozenDictionary<int, string> EffectNames;

        public static int[,] LayoutLength;
        public static int[,] ScreenCount = new int[MaxLevels, 2];
        public static int[,] Tile32Count;
        public static int[,] Tile16Count;

        public static byte[] VRAM_B = {
    0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
    0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
    0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xFF, 0x00, 0xFF, 0x00,
    0xFF, 0x00, 0xFF, 0x00, 0xFF, 0x00, 0xFF, 0x00, 0xFF, 0x00, 0xFF, 0x00,
    0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
    0x00, 0x00, 0x00, 0x00, 0x00, 0xFF, 0x00, 0xFF, 0x00, 0xFF, 0x00, 0xFF,
    0x00, 0xFF, 0x00, 0xFF, 0x00, 0xFF, 0x00, 0xFF, 0x00, 0x00, 0x00, 0x00,
    0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
    0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
    0xFF, 0xFF, 0xFF, 0xFF, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
    0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
    0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
    0xFF, 0x00, 0xFF, 0x00, 0xFF, 0x00, 0xFF, 0x00, 0xFF, 0x00, 0xFF, 0x00,
    0xFF, 0x00, 0xFF, 0x00, 0xFF, 0x00, 0xFF, 0x00, 0xFF, 0x00, 0xFF, 0x00,
    0xFF, 0x00, 0xFF, 0x00, 0xFF, 0x00, 0xFF, 0x00, 0xFF, 0x00, 0xFF, 0x00,
    0xFF, 0x00, 0xFF, 0x00, 0xFF, 0x00, 0xFF, 0x00, 0xFF, 0x00, 0xFF, 0x00,
    0x00, 0xFF, 0x00, 0xFF, 0x00, 0xFF, 0x00, 0xFF, 0x00, 0xFF, 0x00, 0xFF,
    0x00, 0xFF, 0x00, 0xFF, 0xFF, 0x00, 0xFF, 0x00, 0xFF, 0x00, 0xFF, 0x00,
    0xFF, 0x00, 0xFF, 0x00, 0xFF, 0x00, 0xFF, 0x00, 0xFF, 0xFF, 0xFF, 0xFF,
    0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
    0xFF, 0x00, 0xFF, 0x00, 0xFF, 0x00, 0xFF, 0x00, 0xFF, 0x00, 0xFF, 0x00,
    0xFF, 0x00, 0xFF, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
    0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xFF, 0x00, 0xFF,
    0x00, 0xFF, 0x00, 0xFF, 0x00, 0xFF, 0x00, 0xFF, 0x00, 0xFF, 0x00, 0xFF,
    0xFF, 0x00, 0xFF, 0x00, 0xFF, 0x00, 0xFF, 0x00, 0xFF, 0x00, 0xFF, 0x00,
    0xFF, 0x00, 0xFF, 0x00, 0x00, 0xFF, 0x00, 0xFF, 0x00, 0xFF, 0x00, 0xFF,
    0x00, 0xFF, 0x00, 0xFF, 0x00, 0xFF, 0x00, 0xFF, 0x00, 0xFF, 0x00, 0xFF,
    0x00, 0xFF, 0x00, 0xFF, 0x00, 0xFF, 0x00, 0xFF, 0x00, 0xFF, 0x00, 0xFF,
    0x00, 0xFF, 0x00, 0xFF, 0x00, 0xFF, 0x00, 0xFF, 0x00, 0xFF, 0x00, 0xFF,
    0x00, 0xFF, 0x00, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
    0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x00, 0xFF, 0x00, 0xFF,
    0x00, 0xFF, 0x00, 0xFF, 0x00, 0xFF, 0x00, 0xFF, 0x00, 0xFF, 0x00, 0xFF,
    0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
    0x00, 0x00, 0x00, 0x00, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
    0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x00, 0xFF, 0x00,
    0xFF, 0x00, 0xFF, 0x00, 0xFF, 0x00, 0xFF, 0x00, 0xFF, 0x00, 0xFF, 0x00,
    0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
    0xFF, 0xFF, 0xFF, 0xFF, 0x00, 0xFF, 0x00, 0xFF, 0x00, 0xFF, 0x00, 0xFF,
    0x00, 0xFF, 0x00, 0xFF, 0x00, 0xFF, 0x00, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
    0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
    0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
    0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
    0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF
        };

        public static class MegaManX
        {
            public const int LevelsCount = 0x25;
            public const int PlayableLevelsCount = 0xD;

            public const int CompressedTilesAmount = 0xA9;

            public const int PaletteBank = 0x86;
            public const int PaletteColorBank = 0x85;
            public const int PaletteInfoOffset = 0x30133;
            public const int PaletteStageBase = 0x60;

            public const int LoadTileSetBank = 0x86;
            public const int LoadTileSetStageBase = 4;

            public const int EnemyPointersOffset = 0x0282C2;
            public const int EnemyDataBank = 0x85;
            public const int TotalEnemyDataLength = 0x22B2; //does NOT include unused space
            public const int ExtraTotalEnemyDataLength = 0x37E;
            public const int ExtraTotalEnemyDataOffset = 0x2B383;

            public const int TotalLayoutDataLength = 0x1000; //uses unused space
            public const int LayoutDataOffset = 0x090000;

            public const int MaxTotalCheckpoints = 44;

            public const int MaxTotalCameraSettings = 0x46; //ID Used in Storm Eagle Stage
            public const int CameraSettingsBank = 0x86;
            public const int CameraTriggersLength = 0x7EE; //size includes all the pointers

            public const ushort CameraBorderLeftWRAM = 0x1E5E;
            public const ushort CameraBorderRightWRAM = 0x1E60;
            public const ushort CameraBorderTopWRAM = 0x1E68;
            public const ushort CameraBorderBottomWRAM = 0x1E6E;

            public const int ObjectTileInfoLength = 0xCC3; //size includes all the pointers & the un-playable stages
            public const int BackgroundTileInfoLength = 0x8B;

            public const int SpriteArrangmentPointersOffset = 0x68000; //Pointers to sprite arrangment data for objects/enemies (another 24-bit pointer after)

            public const int BackgroundPaletteInfoLength = 0x2F2; //size includes all pointers

            public const int MegaManTilesOffset = 0x170000; //Offset to MegaMan X sprite tiles
            public static readonly int[] MegaManGreenChargeShotTilesOffset = { 0x178400, 0x178500 }; //Offset to MegaMan X Pink Green Shot sprite tiles

            public static readonly int[] LayoutPointersOffset = { 0x30D24, 0x30F4F }; //Layout Pointers Offset to compressed data
            public static readonly int[] ScreenDataPointersOffset = { 0x30D93, 0x30FBE };
            public static readonly int[] Tile32DataPointersOffset = { 0x30E02, 0x3102D };
            public static readonly int[] Tile16DataPointersOffset = { 0x30E71, 0x3109C };
            public static int TileCollisionDataPointersOffset = 0x30EE0;

            public const int BankCount = 48; //$80-AF

            public static readonly int[] HadoukenAsmOffsets = { 0x03CA8A, 0x0427AA, 0x03D0D6 }; //Hadouken Stage ID
            public static readonly int[] RevistsAsmOffset = { 0x03CA97, 0x03CA9B }; //Revists Count
            public static readonly int[] CartSizeCheckAsmOffset = { 0x824A, 0x21FC3, 0x2241B }; //both JP & NA 1.0
            public static readonly int[] RevACartSizeCheckAsmOffset = { 0x824F, 0x21FC8, 0x22420 };
            internal static class NA // North America Version
            {
                public const int LoadTileSetInfoOffset = 0x3756F;
                public const int CompressedTileInfoOffset = 0x376F7;
                public const int CheckpointOffset = 0x32780;
                public const int CameraTriggersOffset = 0x364E2;
                public const int CameraSettingsOffset = 0x36CD0;
                public const int BackgroundPaletteOffset = 0x32260;
                public const int BackgroundTileInfoOffset = 0x321D5;
                public const int ObjectTileInfoOffset = 0x32CEE;
                public const int CompressedTilesSwapInfoOffset = 0x371B7;

                public const int CapsulePositionOffset = 0x03533E;
                public const int CapsuleArmorIndexesOffset = 0x035362;
                public const int UpgradeMovementOffset = 0x03536B;
                public const int CapsuleCameraPositionOffset = 0x0353A1;
                public const int CapsuleTextOffset = 0x0354E3;

                public const int ObjectSpriteInfoOffset = 0x325E4;
            }
            internal static class JP // Japanese Version
            {
                public const int LoadTileSetInfoOffset = 0x37572;
                public const int CompressedTileInfoOffset = 0x376FA;
                public const int CheckpointOffset = 0x32783;
                public const int CameraTriggersOffset = 0x364E5;
                public const int CameraSettingsOffset = 0x36CD3;
                public const int BackgroundPaletteOffset = 0x32263;
                public const int BackgroundTileInfoOffset = 0x321D8;
                public const int ObjectTileInfoOffset = 0x32CF1;
                public const int CompressedTilesSwapInfoOffset = 0x371BA;

                public const int CapsulePositionOffset = 0x035341;
                public const int CapsuleArmorIndexesOffset = 0x035365;
                public const int UpgradeMovementOffset = 0x03536E;
                public const int CapsuleCameraPositionOffset = 0x0353A4;
                public const int CapsuleTextOffset = 0x0354E5;

                public const int ObjectSpriteInfoOffset = 0x325E7;
            }

            public static readonly FrozenDictionary<int, ObjectIcon> ItemIcons =
                new Dictionary<int, ObjectIcon>
                {
                    {0x01, new ObjectIcon(0x1C, 0, 0xA, 0x07, 1)},
                    {0x02, new ObjectIcon(0x1C, 0, 0xA, 0x12, 2)},
                    {0x04, new ObjectIcon(0,    0, 0xA, 0x11, 0)},
                    {0x05, new ObjectIcon(0x1C, 0, 0x8C, 0x96, 0)},
                    {0x07, new ObjectIcon(0x34, 0, 0x19, 0x1B, 0)},
                    {0x0B, new ObjectIcon(0x98, 0, 0x36, 0x38, 0)},
                }
                .ToFrozenDictionary();
            public static readonly FrozenDictionary<int, ObjectIcon> EnemyIcons =
                new Dictionary<int, ObjectIcon>
                {
                    {0x01, new ObjectIcon(4) },
                    {0x02, new ObjectIcon(0x128) },
                    {0x04, new ObjectIcon(0x1E, 3) },
                    {0x05, new ObjectIcon(0x13C) },
                    {0x06, new ObjectIcon(0x22) },
                    {0x07, new ObjectIcon(0x13E) },
                    {0x0A, new ObjectIcon(0x54) },
                    {0xB, new ObjectIcon(0x36) },
                    {0xC, new ObjectIcon(0x15A) },
                    {0xF, new ObjectIcon(0x3C) },
                    {0x10, new ObjectIcon(0x8) },
                    {0x11, new ObjectIcon(0x3A) },
                    {0x13, new ObjectIcon(0x6) },
                    {0x14, new ObjectIcon(0x120) },
                    {0x15, new ObjectIcon(0x3E) },
                    {0x16, new ObjectIcon(0x182) },
                    {0x17, new ObjectIcon(0x182) },
                    {0x19, new ObjectIcon(0x58) },
                    {0x1D, new ObjectIcon(0x82) },
                    {0x1E, new ObjectIcon(0x84) },
                    {0x20, new ObjectIcon(0x88) },
                    {0x22, new ObjectIcon(0x8A) },
                    {0x27, new ObjectIcon(0x8E) },
                    {0x29, new ObjectIcon(0x90) },
                    {0x2B, new ObjectIcon(0xE2) },
                    {0x2C, new ObjectIcon(0xE4) },
                    {0x2E, new ObjectIcon(0xEC) },
                    {0x2D, new ObjectIcon(0xE8) },
                    {0x2F, new ObjectIcon(0xE6) },
                    {0x30, new ObjectIcon(0xF0) },
                    {0x31, new ObjectIcon(0x1D2) },
                    {0x34, new ObjectIcon(0xEE) },
                    {0x35, new ObjectIcon(0xFC) },
                    {0x36, new ObjectIcon(0x114) },
                    {0x37, new ObjectIcon(0xFA) },
                    {0x39, new ObjectIcon(0xFE) },
                    {0x3A, new ObjectIcon(0x11E) },
                    {0x3B, new ObjectIcon(0x148) },
                    {0x3D, new ObjectIcon(0x14C) },
                    {0x40, new ObjectIcon(0x134) },
                    {0x42, new ObjectIcon(0x13A) },
                    {0x44, new ObjectIcon(0x13A) },
                    {0x47, new ObjectIcon(0x150) },
                    {0x49, new ObjectIcon(0x156) },
                    {0x4C, new ObjectIcon(0x26) },
                    {0x4D, new ObjectIcon(0x38, -0x40, false) },
                    {0x4F, new ObjectIcon(0x26) },
                    {0x50, new ObjectIcon(0x96) },
                    {0x51, new ObjectIcon(0x168) },
                    {0x52, new ObjectIcon(0x16E) },
                    {0x53, new ObjectIcon(0x16C) },
                    {0x54, new ObjectIcon(0x180) },
                    {0x5B, new ObjectIcon(0x196) },
                    {0x5D, new ObjectIcon(0x1FC) },
                    {0x62, new ObjectIcon(0x1DA) },
                    {0x63, new ObjectIcon(0x1D8) },
                    {0x65, new ObjectIcon(0x1DC) }
                }.ToFrozenDictionary();

            public static readonly FrozenDictionary<int, string> EffectNames =
                new Dictionary<int, string>
                {
                    {0x00, "Camera Trigger"},
                    {0x0B, "Checkpoint" },
                    {0x15, "OBJ Tile Swap-H" },
                    {0x16, "BG Tile Swap-H" },
                    {0x17, "BG Palette Swap-H" },
                    {0x18, "OBJ Tile Swap-V" },
                    {0x19, "BG Tile Swap-V" },
                    {0x1A, "BG Palette Swap-V" }
                }
                .ToFrozenDictionary();

            public static readonly int[,] LayoutLength = new int[0x25, 2]
{
                    {0x12, 0x28 },
                    {0x32, 0x1C },
                    {0x38, 0x22 },
                    {0x64, 0x4C },
                    {0x22, 0x14 },
                    {0x3A, 0x08 },
                    {0x1E, 0x12 },
                    {0x6A, 0x6E },
                    {0x2A, 0x20 },
                    {0x3C, 0x32 },
                    {0x22, 0x20 },
                    {0x1A, 0x14 },
                    {0x0A, 0x08 },
                    {0x22, 0x22 },
                    {0x08, 0x08 },
                    {0x08, 0x08 },
                    {0x08, 0x08 },
                    {0x08, 0x08 },
                    {0x08, 0x08 },
                    {0x08, 0x08 },
                    {0x08, 0x08 },
                    {0x08, 0x08 },
                    {0x08, 0x08 },
                    {0x08, 0x08 },
                    {0x08, 0x08 },
                    {0x08, 0x08 },
                    {0x08, 0x08 },
                    {0x08, 0x08 },
                    {0x08, 0x08 },
                    {0x08, 0x08 },
                    {0x08, 0x08 },
                    {0x08, 0x08 },
                    {0x08, 0x08 },
                    {0x08, 0x08 },
                    {0x08, 0x08 },
                    {0x06, 0x08 },
                    {0x06, 0x08 }
};
            public static readonly int[,] Tile32Count = new int[0x25, 2]
            {
                    {0x16A, 0x8A },
                    {0x180, 0x82 },
                    {0x283, 0x5C },
                    {0x225, 0xF2 },
                    {0x215, 0x7A },
                    {0x1E2, 0x42 },
                    {0x1B0, 0x6D },
                    {0x1FB, 0x59 },
                    {0x234, 0x6A },
                    {0x267, 0x44 },
                    {0x271, 0x17 },
                    {0x23C, 0x34 },
                    {0x4E , 0x44 },
                    {0x215, 0x5C},
                    {0xE  , 0xE },
                    {0xE  , 0xE },
                    {0xE  , 0xE },
                    {0xE  , 0xE },
                    {0xE  , 0xE },
                    {0xE  , 0xE },
                    {0xE  , 0xE },
                    {0xE  , 0xE },
                    {0xE  , 0xE },
                    {0xE  , 0xE },
                    {0xE  , 0xE },
                    {0xE  , 0xE },
                    {0xE  , 0xE },
                    {0x3B , 0x12},
                    {0x3B , 0x12},
                    {0x3B , 0x12},
                    {0x3B , 0x12},
                    {0x3B , 0x12},
                    {0x3B , 0x12},
                    {0x3B , 0x12},
                    {0x3B , 0x12},
                    {0x19 , 0x12},
                    {0x62 , 0x12}
            };
            public static readonly int[,] Tile16Count = new int[0x25, 2]
            {
                    {0x388, 0x388},
                    {0x2D7, 0x2D7},
                    {0x357, 0x357},
                    {0x365, 0x365},
                    {0x325, 0x325},
                    {0x2D5, 0x2D5},
                    {0x2AE, 0x2AE},
                    {0x376, 0x376},
                    {0x2E2, 0x2E2},
                    {0x1EF, 0x1EF},
                    {0x26A, 0x26A},
                    {0x1DC, 0x1DC},
                    {0x107, 0x107},
                    {0x350, 0x350},
                    {0x38,  0x38 },
                    {0x38,  0x38 },
                    {0x38,  0x38 },
                    {0x38,  0x38 },
                    {0x38,  0x38 },
                    {0x38,  0x38 },
                    {0x38,  0x38 },
                    {0x38,  0x38 },
                    {0x38,  0x38 },
                    {0x38,  0x38 },
                    {0x38,  0x38 },
                    {0x38,  0x38 },
                    {0x38,  0x38 },
                    {0x6F,  0x6F },
                    {0x6F,  0x6F },
                    {0x6F,  0x6F },
                    {0x6F,  0x6F },
                    {0x6F,  0x6F },
                    {0x6F,  0x6F },
                    {0x6F,  0x6F },
                    {0x6F,  0x6F },
                    {0x6F,  0x6F },
                    {0xA8,  0xA8 }
            };
        }
        public static class MegaManX2
        {
            public const int LevelsCount = 0xD;
            public const int PlayableLevelsCount = 0xD;

            public const int CompressedTilesAmount = 0xA5;

            public const int PaletteBank = 0x06;
            public const int PaletteColorBank = 0x05;
            public const int PaletteInfoOffset = 0x3017A;
            public const int PaletteStageBase = 0x60;

            public const int LoadTileSetBank = 0x06;
            public const int LoadTileSetStageBase = 4;

            public const int EnemyDataBank = 0x29;
            public const int TotalEnemyDataLength = 0x2C07; //uses unused space

            public const int TotalLayoutDataLength = 0x800; //uses unused space
            public const int LayoutDataOffset = 0x90000;

            public const int MaxTotalCheckpoints = 64;

            public const int MaxTotalCameraSettings = 0x7E;
            public const int CameraTriggersLength = 0xEFB; //size includes all the pointers
            public const int CameraSettingsBank = 0x2;
            public const int CameraTriggersOffset = 0x16BE9;
            public const int CameraSettingsOffset = 0x17AE4;

            public const int ObjectTileInfoLength = 0xCF7; //size includes all the pointers & the un-playable stages
            public const int BackgroundTileInfoLength = 0x67;

            public const int SpriteArrangmentPointersOffset = 0x68000;

            public const ushort CameraBorderLeftWRAM = 0x1E6E;
            public const ushort CameraBorderRightWRAM = 0x1E70;
            public const ushort CameraBorderTopWRAM = 0x1E78;
            public const ushort CameraBorderBottomWRAM = 0x1E7E;

            public const int ObjectTileInfoOffset = 0x1532D4;
            public const int CompressedTilesSwapInfoOffset = 0x157B02;

            public const int BackgroundPaletteInfoLength = 0x382; //size includes all pointers

            public const int MegaManTilesOffset = 0x168000; //Offset to MegaMan X sprite tiles

            public static readonly int[] LayoutPointersOffset = { 0x030888, 0x30AB3 };
            public static readonly int[] ScreenDataPointersOffset = { 0x0308F7, 0x30B22 };
            public static readonly int[] Tile32DataPointersOffset = { 0x030966, 0x30B91 };
            public static readonly int[] Tile16DataPointersOffset = { 0x0309D5, 0x30C00 };
            public static int TileCollisionDataPointersOffset = 0x030A44;

            public const int BankCount = 48;//$00-2F
            internal static class NA // North America Version
            {
                public const int LoadTileSetInfoOffset = 0x37831;
                public const int CompressedTileInfoOffset = 0x37A01;
                public const int EnemyPointersOffset = 0x14D3D1;
                public const int CheckpointOffset = 0x324C5;
                public const int BackgroundTileInfoOffset = 0x31D6A;
                public const int BackgroundPaletteOffset = 0x31DD1;

                public static readonly int[] ShoryukenAsmOffsets = { 0x14930F, 0x00CE0E }; //Shoryuken Stage ID
                public const int CapsulePositionOffset = 0x0356F1;
                public const int CapsuleArmorIndexesOffset = 0x035721;
                public const int UpgradeMovementOffset = 0x03572D;
                public const int CapsuleCameraPositionOffset = 0x03579F;
                public const int CapsuleTextOffset = 0x035A94;

                public const int OstrichIdOffset = 0x14937F;

                public const int ObjectSpriteInfoOffset = 0x3234D;
            }
            internal static class JP // Japanese Version
            {
                public const int LoadTileSetInfoOffset = 0x37832;
                public const int CompressedTileInfoOffset = 0x37A02;
                public const int EnemyPointersOffset = 0x14D3D9;
                public const int CheckpointOffset = 0x324C6;
                public const int BackgroundTileInfoOffset = 0x31D6B;
                public const int BackgroundPaletteOffset = 0x31DD2;

                public static readonly int[] ShoryukenAsmOffsets = { 0x149317, 0x00CE0E }; //Shoryuken Stage ID
                public const int CapsulePositionOffset = 0x0356F2;
                public const int CapsuleArmorIndexesOffset = 0x035722;
                public const int UpgradeMovementOffset = 0x03572E;
                public const int CapsuleCameraPositionOffset = 0x0357A0;
                public const int CapsuleTextOffset = 0x035A95;

                public const int OstrichIdOffset = 0x149387;

                public const int ObjectSpriteInfoOffset = 0x3234E;
            }

            public static readonly FrozenDictionary<int, ObjectIcon> ItemIcons =
                new Dictionary<int, ObjectIcon>
                {
                    {0x01, new ObjectIcon(0x1C, 0, 0xA, 0x07, 1)},
                    {0x02, new ObjectIcon(0x1C, 0, 0xA, 0x12, 2)},
                    {0x04, new ObjectIcon(0,    0, 0xA, 0x11, 0)},
                    {0x05, new ObjectIcon(0x1C, 0, 0x8C, 0x96, 0)},
                    {0x06, new ObjectIcon(0x34, 0, 0x38, 0x1B, 9)},
                    {0x07, new ObjectIcon(0x34, 0, 0x19, 0x1B, 0)},
                    {0x0B, new ObjectIcon(0x98, 0, 0x36, 0x38, 0)},
                }
                .ToFrozenDictionary();
            public static readonly FrozenDictionary<int, ObjectIcon> EnemyIcons =
                new Dictionary<int, ObjectIcon>
                {
                    {0x04, new ObjectIcon(0x4,6) },
                    {0x05, new ObjectIcon(0x15A) },
                    {0x09, new ObjectIcon(0xA) },
                    {0x0A, new ObjectIcon(0x1E) },
                    {0x0B, new ObjectIcon(0x26) },
                    {0x0C, new ObjectIcon(0x22) },
                    {0x0E, new ObjectIcon(0x20) },
                    {0x10, new ObjectIcon(0x26,0x28) },
                    {0x11, new ObjectIcon(0x2C) },
                    {0x12, new ObjectIcon(0x26,2) },
                    {0x16, new ObjectIcon(0x3E) },
                    {0x17, new ObjectIcon(0x3C,3) },
                    {0x19, new ObjectIcon(0x84) },
                    {0x1A, new ObjectIcon(0x36) },
                    {0x1B, new ObjectIcon(0x86) },
                    {0x1D, new ObjectIcon(0x8E) },
                    {0x1E, new ObjectIcon(0X92) },
                    {0x1F, new ObjectIcon(0x94) },
                    {0x20, new ObjectIcon(0xA6)},
                    {0x21, new ObjectIcon(0x8C) },
                    {0x22, new ObjectIcon(0xA8) },
                    {0x23, new ObjectIcon(0x9E) },
                    {0x2C, new ObjectIcon(0x96) },
                    {0x2D, new ObjectIcon(0x82) },
                    {0x2F, new ObjectIcon(0xE6) },
                    {0x30, new ObjectIcon(0x9C) },
                    {0x32, new ObjectIcon(0x88) },
                    {0x33, new ObjectIcon(0xF0) },
                    {0x34, new ObjectIcon(0xEA) },
                    {0x35, new ObjectIcon(0xEE) },
                    {0x37, new ObjectIcon(0xE2) },
                    {0x38, new ObjectIcon(0xFA) },
                    {0x39, new ObjectIcon(0xFC) },
                    {0x3A, new ObjectIcon(0xF4) },
                    {0x3C, new ObjectIcon(0x116) },
                    {0x3D, new ObjectIcon(0x124) },
                    {0x3F, new ObjectIcon(0x126) },
                    {0x44, new ObjectIcon(0x122) },
                    {0x49, new ObjectIcon(0x134) },
                    {0x4D, new ObjectIcon(0xE4, -0x40, false) },
                    {0x50, new ObjectIcon(0x152) },
                    {0x51, new ObjectIcon(0x14C) },
                    {0x52, new ObjectIcon(0x128) },
                    {0x53, new ObjectIcon(0x11A) },
                    {0x54, new ObjectIcon(0xFC) },
                    {0x55, new ObjectIcon(0x13E) },
                    {0x57, new ObjectIcon(0x14A) },
                    {0x58, new ObjectIcon(0x14A, 4) },
                    {0x5A, new ObjectIcon(0x120) },
                    {0x5B, new ObjectIcon(0x15E) },
                    {0x61, new ObjectIcon(0x14E) },
                    {0x62, new ObjectIcon(0x16A,0xB) },
                    {0x65, new ObjectIcon(0x16E) },
                }.ToFrozenDictionary();

            public static readonly FrozenDictionary<int, string> EffectNames =
                new Dictionary<int, string>
                {
                    {0x00, "Camera Trigger"},
                    {0x0B, "Checkpoint" },
                    {0x15, "OBJ Tile Swap-H" },
                    {0x16, "BG Tile Swap-H" },
                    {0x17, "BG Palette Swap-H" },
                    {0x18, "OBJ Tile Swap-V" },
                    {0x19, "BG Tile Swap-V" },
                    {0x1A, "BG Palette Swap-V" }
                }
                .ToFrozenDictionary();

            public static readonly int[,] LayoutLength = new int[0xD, 2]
            {
                    {0x8C, 0x20 },
                    {0x3E, 0x24 },
                    {0x38, 0x24 },
                    {0x40, 0x28 },
                    {0x42, 0x2A },
                    {0x5C, 0x54 },
                    {0x2A, 0x12 },
                    {0x52, 0x34 },
                    {0x5E, 0x92 },
                    {0x5A, 0x14 },
                    {0x16, 0x10 },
                    {0x5A, 0x1E },
                    {0x28, 0x2A }
            };
            public static readonly int[,] Tile32Count = new int[0xD, 2]
            {
                    {0x1C8, 0xF6},
                    {0x36E, 0x51},
                    {0x257, 0x61},
                    {0x1B3, 0xD2},
                    {0x286, 0xA9},
                    {0x1E6, 0x84},
                    {0x2C2, 0x2B},
                    {0x24E, 0x89},
                    {0x27B, 0x6B},
                    {0x11A, 0x26},
                    {0x124, 0x31},
                    {0x172, 0x17},
                    {0xA5 , 0x4F}
            };
            public static readonly int[,] Tile16Count = new int[0xD, 2]
            {
                    {0x3A4, 0x3A4},
                    {0x3B3, 0x3B3},
                    {0x2F0, 0x2F0},
                    {0x2BA, 0x2BA},
                    {0x2F4, 0x2F4},
                    {0x28C, 0x28C},
                    {0x2B6, 0x2B6},
                    {0x31A, 0x31A},
                    {0x2BE, 0x2BE},
                    {0x113, 0x113},
                    {0xCE,  0xCE },
                    {0xCE,  0xCE },
                    {0x13C, 0x13C}
            };
        }
        public static class MegaManX3
        {
            public const int LevelsCount = 0x11;
            public const int PlayableLevelsCount = 0x11;

            public const int CompressedTilesAmount = 0xDD;

            public const int PaletteBank = 0x06;
            public const int PaletteColorBank = 0x0C;
            public const int PaletteInfoOffset = 0x30180;
            public const int PaletteStageBase = 0x60;

            public const int LoadTileSetBank = 0x06;
            public const int LoadTileSetStageBase = 4;

            public const int EnemyPointersOffset = 0x1E4E4B;
            public const int EnemyDataBank = 0x3C;
            public const int TotalEnemyDataLength = 0x3195; //uses unused space (used space is 0x3013)

            public const int TotalLayoutDataLength = 0x800; //uses unused space
            public const int LayoutDataOffset = 0x1AF800;

            public const int MaxTotalCheckpoints = 53;

            public const int MaxTotalCameraSettings = 0x90;
            public const int CameraTriggersLength = 0x1489; //size includes all the pointers
            public const int CameraSettingsBank = 0x3;
            public const int CameraTriggersOffset = 0x1DE43;
            public const int CameraSettingsOffset = 0x1F2CC;

            public const int ObjectTileInfoOffset = 0x40623;
            public const int CompressedTilesSwapInfoOffset = 0x457C9;

            public const int ObjectTileInfoLength = 0xBCF; //size includes all the pointers & the un-playable stages
            public const int BackgroundTileInfoLength = 0xED; //size includes all pointers

            public const int SpriteArrangmentPointersOffset = 0x68000;

            public const int BackgroundPaletteInfoLength = 0x334; //size includes all pointers

            public const int MegaManTilesOffset = 0x168000; //Offset to MegaMan X sprite tiles
            public static readonly int[] MegaManGreenChargeShotTilesOffset = { 0x1E0400, 0x1E0500 }; //Offset to MegaMan X Pink Green Shot sprite tiles

            public static readonly int[] LayoutPointersOffset = { 0x309B3, 0x30BDE };
            public static readonly int[] ScreenDataPointersOffset = { 0x30A22, 0x30C4D };
            public static readonly int[] Tile32DataPointersOffset = { 0x30A91, 0x30CBC };
            public static readonly int[] Tile16DataPointersOffset = { 0x30B00, 0x30D2B };
            public static int TileCollisionDataPointersOffset = 0x30B6F;

            public const int BankCount = 64; //0-3F

            public static readonly int[] FreeBanks = { 0x14, 0x16, 0x17, 0x18, 0x19, 0x34, 0x38, 0x3D }; //Needed for 16x16 Tile Data

            public const int GoldenArmorIdOffset = 0x09C015;

            public static readonly FrozenDictionary<int, ObjectIcon> ItemIcons =
                new Dictionary<int, ObjectIcon>
                {
                    {0x01, new ObjectIcon(0x1C, 0, 0xA, 0x07, 1)},
                    {0x02, new ObjectIcon(0x1C, 0, 0xA, 0x12, 2)},
                    {0x04, new ObjectIcon(0,    0, 0xA, 0x11, 0)},
                    {0x05, new ObjectIcon(0x1C, 0, 0x8C, 0x96, 0)},
                    {0x06, new ObjectIcon(0x34, 0, 0x38, 0x1B, 9)},
                    {0x07, new ObjectIcon(0x34, 0, 0x19, 0x1B, 0)},
                    {0x0B, new ObjectIcon(0x98, 0, 0x36, 0x38, 0)},
                    {0x11, new ObjectIcon(0x34, 0, 0x56, 0x4C, 0)},
                }
                .ToFrozenDictionary();

            public static readonly FrozenDictionary<int, ObjectIcon> EnemyIcons =
                new Dictionary<int, ObjectIcon>
                {
                    {0x03, new ObjectIcon(0x3C) },
                    {0x05, new ObjectIcon(0x4) },
                    {0x06, new ObjectIcon(0x20) },
                    {0x08, new ObjectIcon(8) },
                    {0x09, new ObjectIcon(0x1EE) },
                    {0x0B, new ObjectIcon(0xA, 6) },
                    {0x0C, new ObjectIcon(0x1E) },
                    {0x0D, new ObjectIcon(0xC4) },
                    {0x0E, new ObjectIcon(0x80) },
                    {0x11, new ObjectIcon(0x58) },
                    {0x16, new ObjectIcon(0x88) },
                    {0x18, new ObjectIcon(0x86) },
                    {0x19, new ObjectIcon(0x1C8) },
                    {0x1B, new ObjectIcon(0x92) },
                    {0x1C, new ObjectIcon(0x90) },
                    {0x1D, new ObjectIcon(0x84) },
                    {0x1E, new ObjectIcon(0x82) },
                    {0x1F, new ObjectIcon(0xC6) },
                    {0x20, new ObjectIcon(0x9E) },
                    {0x21, new ObjectIcon(0x92) },
                    {0x22, new ObjectIcon(0xE8) },
                    {0x23, new ObjectIcon(0x3A) },
                    {0x26, new ObjectIcon(0x9C) },
                    {0x27, new ObjectIcon(0xEA) },
                    {0x28, new ObjectIcon(0xF0) },
                    {0x29, new ObjectIcon(0xF4) },
                    {0x2B, new ObjectIcon(0xEE) },
                    {0x2C, new ObjectIcon(0x118) },
                    {0x2D, new ObjectIcon(0x116) },
                    {0x2F, new ObjectIcon(0x96) },
                    {0x30, new ObjectIcon(0x1D8) },
                    {0x32, new ObjectIcon(0x13E) },
                    {0x33, new ObjectIcon(0x14C) },
                    {0x34, new ObjectIcon(0x14C) },
                    {0x35, new ObjectIcon(0x16E) },
                    {0x36, new ObjectIcon(0x1EA) },
                    {0x38, new ObjectIcon(0x1E6) },
                    {0x3D, new ObjectIcon(0x1EC) },
                    {0x3F, new ObjectIcon(0x2) },
                    {0x42, new ObjectIcon(0x22) },
                    {0x46, new ObjectIcon(0x160) },
                    {0x47, new ObjectIcon(0x1FC) },
                    {0x49, new ObjectIcon(0x120) },
                    {0x4A, new ObjectIcon(0x1F2) },
                    {0x4B, new ObjectIcon(0x26, 2) },
                    {0x4C, new ObjectIcon(0xC2) },
                    {0x4D, new ObjectIcon(0xE4, -0x40, true) },
                    {0x50, new ObjectIcon(0xC0) },
                    {0x51, new ObjectIcon(0x158) },
                    {0x52, new ObjectIcon(0xFC) },
                    {0x53, new ObjectIcon(0xEC) },
                    {0x54, new ObjectIcon(0xFE) },
                    {0x55, new ObjectIcon(0x3E) },
                    {0x56, new ObjectIcon(0x122) },
                    {0x57, new ObjectIcon(0x182) },
                    {0x58, new ObjectIcon(0x1FE) },
                    {0x59, new ObjectIcon(0xFA) },
                    {0x5B, new ObjectIcon(0x148) },
                    {0x62, new ObjectIcon(0x16C) },
                    {0x63, new ObjectIcon(0x1F0) }
                }.ToFrozenDictionary();

            public static readonly FrozenDictionary<int, string> EffectNames =
                new Dictionary<int, string>
                {
                    {0x00, "Camera Trigger"},
                    {0x0B, "Checkpoint" },
                    {0x15, "OBJ Tile Swap-H" },
                    {0x16, "BG Tile Swap-H" },
                    {0x17, "BG Palette Swap-H" },
                    {0x18, "OBJ Tile Swap-V" },
                    {0x19, "BG Tile Swap-V" },
                    {0x1A, "BG Palette Swap-V" }
                }
                .ToFrozenDictionary();

            public static readonly int[,] LayoutLength = new int[0x11, 2]
            {
                    {0x4C, 0x3A },
                    {0x4C, 0x26 },
                    {0x38, 0x36 },
                    {0x42, 0x0E },
                    {0x60, 0x42 },
                    {0x54, 0x42 },
                    {0x4E, 0x32 },
                    {0x52, 0x1C },
                    {0x30, 0x1C },
                    {0x2E, 0x3A },
                    {0x4E, 0x40 },
                    {0x46, 0x20 },
                    {0x22, 0x10 },
                    {0x2A, 0x0A },
                    {0x46, 0x20 },
                    {0x38, 0x36 },
                    {0x42, 0x0E }
            };
            public static readonly int[,] Tile32Count = new int[0x11, 2]
            {
                    { 0x2FF, 0x11C  },
                    { 0x54C, 0x79   },
                    { 0x4F1, 0x184  },
                    { 0x407, 0x78   },
                    { 0x4C4, 0x156  },
                    { 0x517, 0x118  },
                    { 0x444, 0xE2   },
                    { 0x537, 0xC2   },
                    { 0x567, 0xB8   },
                    { 0x268, 0x71   },
                    { 0x36F, 0x58   },
                    { 0x335, 0x8C   },
                    { 0x205, 0x3B   },
                    { 0x1E1, 0x44   },
                    { 0x528, 0x8C   },
                    { 0x4E8, 0x184  },
                    { 0x3F7, 0x78   }
            };
            public static readonly int[,] Tile16Count = new int[0x11, 2]
            {
                    {0x400, 0x400},
                    {0x400, 0x400},
                    {0x2F9, 0x2F9},
                    {0x34B, 0x34B},
                    {0x35A, 0x35A},
                    {0x24E, 0x24E},
                    {0x3FC, 0x3FC},
                    {0x3C0, 0x3C0},
                    {0x333, 0x333},
                    {0x283, 0x283},
                    {0x2E4, 0x2E4},
                    {0x293, 0x293},
                    {0x1FA, 0x1FA},
                    {0x1B7, 0x1B7},
                    {0x293, 0x293},
                    {0x2F9, 0x2F9},
                    {0x34B, 0x34B}
            };
            internal class NA
            {
                public const int LoadTileSetInfoOffset = 0x373C3;
                public const int CompressedTileInfoOffset = 0x37732;
                public const int CheckpointOffset = 0x328E4;
                public const int BackgroundTileInfoOffset = 0x32085;
                public const int BackgroundPaletteOffset = 0x32172;

                public const int CapsulePositionOffset = 0x034CBE;
                public const int CapsuleArmorIndexesOffset = 0x034CFA;
                public const int UpgradeMovementOffset = 0x034D09;
                public const int CapsuleCameraPositionOffset = 0x034D9F;
                public const int CapsuleTextOffset = 0x034EED;

                public const int ObjectSpriteInfoOffset = 0x3628E;
            }
            internal class JP
            {
                public const int LoadTileSetInfoOffset = 0x373C4;
                public const int CompressedTileInfoOffset = 0x37733;
                public const int CheckpointOffset = 0x328E5;
                public const int BackgroundTileInfoOffset = 0x32086;
                public const int BackgroundPaletteOffset = 0x32173;

                public const int CapsulePositionOffset = 0x034CBF;
                public const int CapsuleArmorIndexesOffset = 0x034CFB;
                public const int UpgradeMovementOffset = 0x034D0A;
                public const int CapsuleCameraPositionOffset = 0x034DA0;
                public const int CapsuleTextOffset = 0x034EEE;

                public const int ObjectSpriteInfoOffset = 0x3628F;
            }
        }
        public static void AssignProperties(GameId gameId, GameVersion gameVersion, bool expanded)
        {
            Version = gameVersion;

            if (gameId == GameId.MegaManX)
            {
                Id = GameId.MegaManX;
                LevelsCount = MegaManX.LevelsCount;
                PlayableLevelsCount = MegaManX.PlayableLevelsCount;

                PaletteBank = MegaManX.PaletteBank;
                PaletteColorBank = MegaManX.PaletteColorBank;
                PaletteInfoOffset = MegaManX.PaletteInfoOffset;
                PaletteStageBase = MegaManX.PaletteStageBase;

                LoadTileSetBank = MegaManX.LoadTileSetBank;
                LoadTileSetStageBase = MegaManX.LoadTileSetStageBase;

                EnemyPointersOffset = MegaManX.EnemyPointersOffset;
                EnemyDataBank = MegaManX.EnemyDataBank;
                TotalEnemyDataLength = MegaManX.TotalEnemyDataLength;
                TotalLayoutDataLength = MegaManX.TotalLayoutDataLength;
                LayoutDataOffset = MegaManX.LayoutDataOffset;

                MaxTotalCheckpoints = MegaManX.MaxTotalCheckpoints;
                CameraSettingsBank = MegaManX.CameraSettingsBank;
                CameraTriggersLength = MegaManX.CameraTriggersLength;
                MaxTotalCameraSettings = MegaManX.MaxTotalCameraSettings;

                CameraBorderLeftWRAM = MegaManX.CameraBorderLeftWRAM;
                CameraBorderRightWRAM = MegaManX.CameraBorderRightWRAM;
                CameraBorderTopWRAM = MegaManX.CameraBorderTopWRAM;
                CameraBorderBottomWRAM = MegaManX.CameraBorderBottomWRAM;

                BackgroundTileInfoLength = MegaManX.BackgroundTileInfoLength;
                ObjectTileInfoLength = MegaManX.ObjectTileInfoLength;

                BackgroundPaletteInfoLength = MegaManX.BackgroundPaletteInfoLength;

                LayoutPointersOffset = MegaManX.LayoutPointersOffset;
                ScreenDataPointersOffset = MegaManX.ScreenDataPointersOffset;
                Tile32DataPointersOffset = MegaManX.Tile32DataPointersOffset;
                Tile16DataPointersOffset = MegaManX.Tile16DataPointersOffset;
                TileCollisionDataPointersOffset = MegaManX.TileCollisionDataPointersOffset;

                MegaManTilesOffset = MegaManX.MegaManTilesOffset;
                MegaManGreenChargeShotTilesOffset = MegaManX.MegaManGreenChargeShotTilesOffset;

                SpriteArrangmentPointersOffset = MegaManX.SpriteArrangmentPointersOffset;

                ItemIcons = MegaManX.ItemIcons;
                EnemyIcons = MegaManX.EnemyIcons;

                EffectNames = MegaManX.EffectNames;

                LayoutLength = MegaManX.LayoutLength;
                Tile32Count = MegaManX.Tile32Count;
                Tile16Count = MegaManX.Tile16Count;

                if (gameVersion == GameVersion.NA)
                {
                    LoadTileSetInfoOffset = MegaManX.NA.LoadTileSetInfoOffset;
                    CompressedTileInfoOffset = MegaManX.NA.CompressedTileInfoOffset;
                    CheckpointOffset = MegaManX.NA.CheckpointOffset;
                    BackgroundPaletteOffset = MegaManX.NA.BackgroundPaletteOffset;
                    BackgroundTileInfoOffset = MegaManX.NA.BackgroundTileInfoOffset;
                    CameraTriggersOffset = MegaManX.NA.CameraTriggersOffset;
                    CameraSettingsOffset = MegaManX.NA.CameraSettingsOffset;
                    ObjectTileInfoOffset = MegaManX.NA.ObjectTileInfoOffset;
                    CompressedTilesSwapInfoOffset = MegaManX.NA.CompressedTilesSwapInfoOffset;
                    ObjectSpriteInfoOffset = MegaManX.NA.ObjectSpriteInfoOffset;
                }
                else
                {
                    LoadTileSetInfoOffset = MegaManX.JP.LoadTileSetInfoOffset;
                    CompressedTileInfoOffset = MegaManX.JP.CompressedTileInfoOffset;
                    CheckpointOffset = MegaManX.JP.CheckpointOffset;
                    BackgroundPaletteOffset = MegaManX.JP.BackgroundPaletteOffset;
                    BackgroundTileInfoOffset = MegaManX.JP.BackgroundTileInfoOffset;
                    CameraTriggersOffset = MegaManX.JP.CameraTriggersOffset;
                    CameraSettingsOffset = MegaManX.JP.CameraSettingsOffset;
                    ObjectTileInfoOffset = MegaManX.JP.ObjectTileInfoOffset;
                    CompressedTilesSwapInfoOffset = MegaManX.JP.CompressedTilesSwapInfoOffset;
                    ObjectSpriteInfoOffset = MegaManX.JP.ObjectSpriteInfoOffset;
                }
            }
            else if (gameId == GameId.MegaManX2)
            {
                Id = GameId.MegaManX2;
                LevelsCount = MegaManX2.LevelsCount;
                PlayableLevelsCount = MegaManX2.PlayableLevelsCount;

                PaletteBank = MegaManX2.PaletteBank;
                PaletteColorBank = MegaManX2.PaletteColorBank;
                PaletteInfoOffset = MegaManX2.PaletteInfoOffset;
                PaletteStageBase = MegaManX2.PaletteStageBase;

                LoadTileSetBank = MegaManX2.LoadTileSetBank;
                LoadTileSetStageBase = MegaManX2.LoadTileSetStageBase;

                EnemyDataBank = MegaManX2.EnemyDataBank;
                TotalEnemyDataLength = MegaManX2.TotalEnemyDataLength;
                TotalLayoutDataLength = MegaManX2.TotalLayoutDataLength;
                LayoutDataOffset = MegaManX2.LayoutDataOffset;

                MaxTotalCheckpoints = MegaManX2.MaxTotalCheckpoints;
                CameraSettingsBank = MegaManX2.CameraSettingsBank;
                CameraTriggersLength = MegaManX2.CameraTriggersLength;
                MaxTotalCameraSettings = MegaManX2.MaxTotalCameraSettings;

                CameraBorderLeftWRAM = MegaManX2.CameraBorderLeftWRAM;
                CameraBorderRightWRAM = MegaManX2.CameraBorderRightWRAM;
                CameraBorderTopWRAM = MegaManX2.CameraBorderTopWRAM;
                CameraBorderBottomWRAM = MegaManX2.CameraBorderBottomWRAM;

                BackgroundTileInfoLength = MegaManX2.BackgroundTileInfoLength;
                ObjectTileInfoLength = MegaManX2.ObjectTileInfoLength;

                BackgroundPaletteInfoLength = MegaManX2.BackgroundPaletteInfoLength;

                CameraTriggersOffset = MegaManX2.CameraTriggersOffset;
                CameraSettingsOffset = MegaManX2.CameraSettingsOffset;

                LayoutPointersOffset = MegaManX2.LayoutPointersOffset;
                ScreenDataPointersOffset = MegaManX2.ScreenDataPointersOffset;
                Tile32DataPointersOffset = MegaManX2.Tile32DataPointersOffset;
                Tile16DataPointersOffset = MegaManX2.Tile16DataPointersOffset;
                TileCollisionDataPointersOffset = MegaManX2.TileCollisionDataPointersOffset;

                ObjectTileInfoOffset = MegaManX2.ObjectTileInfoOffset;
                CompressedTilesSwapInfoOffset = MegaManX2.CompressedTilesSwapInfoOffset;

                MegaManTilesOffset = MegaManX2.MegaManTilesOffset;
                MegaManGreenChargeShotTilesOffset = MegaManX.MegaManGreenChargeShotTilesOffset;

                SpriteArrangmentPointersOffset = MegaManX2.SpriteArrangmentPointersOffset;

                ItemIcons = MegaManX2.ItemIcons;
                EnemyIcons = MegaManX2.EnemyIcons;

                EffectNames = MegaManX2.EffectNames;

                LayoutLength = MegaManX2.LayoutLength;
                Tile32Count = MegaManX2.Tile32Count;
                Tile16Count = MegaManX2.Tile16Count;

                if (gameVersion == GameVersion.NA)
                {
                    LoadTileSetInfoOffset = MegaManX2.NA.LoadTileSetInfoOffset;
                    CompressedTileInfoOffset = MegaManX2.NA.CompressedTileInfoOffset;
                    EnemyPointersOffset = MegaManX2.NA.EnemyPointersOffset;
                    CheckpointOffset = MegaManX2.NA.CheckpointOffset;
                    BackgroundTileInfoOffset = MegaManX2.NA.BackgroundTileInfoOffset;
                    BackgroundPaletteOffset = MegaManX2.NA.BackgroundPaletteOffset;
                    ObjectSpriteInfoOffset = MegaManX2.NA.ObjectSpriteInfoOffset;
                }
                else
                {
                    LoadTileSetInfoOffset = MegaManX2.JP.LoadTileSetInfoOffset;
                    CompressedTileInfoOffset = MegaManX2.JP.CompressedTileInfoOffset;
                    EnemyPointersOffset = MegaManX2.JP.EnemyPointersOffset;
                    CheckpointOffset = MegaManX2.JP.CheckpointOffset;
                    BackgroundTileInfoOffset = MegaManX2.JP.BackgroundTileInfoOffset;
                    BackgroundPaletteOffset = MegaManX2.JP.BackgroundPaletteOffset;
                    ObjectSpriteInfoOffset = MegaManX2.JP.ObjectSpriteInfoOffset;
                }
            }
            else if (gameId == GameId.MegaManX3)
            {
                Id = GameId.MegaManX3;
                LevelsCount = MegaManX3.LevelsCount;
                PlayableLevelsCount = MegaManX3.PlayableLevelsCount;

                PaletteBank = MegaManX3.PaletteBank;
                PaletteColorBank = MegaManX3.PaletteColorBank;
                PaletteInfoOffset = MegaManX3.PaletteInfoOffset;
                PaletteStageBase = MegaManX3.PaletteStageBase;

                LoadTileSetBank = MegaManX3.LoadTileSetBank;
                LoadTileSetStageBase = MegaManX3.LoadTileSetStageBase;


                EnemyPointersOffset = MegaManX3.EnemyPointersOffset;
                EnemyDataBank = MegaManX3.EnemyDataBank;
                TotalEnemyDataLength = MegaManX3.TotalEnemyDataLength;
                TotalLayoutDataLength = MegaManX3.TotalLayoutDataLength;
                LayoutDataOffset = MegaManX3.LayoutDataOffset;

                MaxTotalCheckpoints = MegaManX3.MaxTotalCheckpoints;
                CameraSettingsBank = MegaManX3.CameraSettingsBank;
                CameraTriggersLength = MegaManX3.CameraTriggersLength;

                CameraTriggersOffset = MegaManX3.CameraTriggersOffset;
                CameraSettingsOffset = MegaManX3.CameraSettingsOffset;
                MaxTotalCameraSettings = MegaManX3.MaxTotalCameraSettings;

                CameraBorderLeftWRAM = MegaManX2.CameraBorderLeftWRAM;
                CameraBorderRightWRAM = MegaManX2.CameraBorderRightWRAM;
                CameraBorderTopWRAM = MegaManX2.CameraBorderTopWRAM;
                CameraBorderBottomWRAM = MegaManX2.CameraBorderBottomWRAM;

                BackgroundTileInfoLength = MegaManX3.BackgroundTileInfoLength;
                ObjectTileInfoLength = MegaManX3.ObjectTileInfoLength;

                BackgroundPaletteInfoLength = MegaManX3.BackgroundPaletteInfoLength;

                LayoutPointersOffset = MegaManX3.LayoutPointersOffset;
                ScreenDataPointersOffset = MegaManX3.ScreenDataPointersOffset;
                Tile32DataPointersOffset = MegaManX3.Tile32DataPointersOffset;
                Tile16DataPointersOffset = MegaManX3.Tile16DataPointersOffset;
                TileCollisionDataPointersOffset = MegaManX3.TileCollisionDataPointersOffset;

                ObjectTileInfoOffset = MegaManX3.ObjectTileInfoOffset;
                CompressedTilesSwapInfoOffset = MegaManX3.CompressedTilesSwapInfoOffset;

                MegaManTilesOffset = MegaManX3.MegaManTilesOffset;
                MegaManGreenChargeShotTilesOffset = MegaManX3.MegaManGreenChargeShotTilesOffset;

                SpriteArrangmentPointersOffset = MegaManX3.SpriteArrangmentPointersOffset;

                ItemIcons = MegaManX3.ItemIcons;
                EnemyIcons = MegaManX3.EnemyIcons;

                EffectNames = MegaManX3.EffectNames;

                LayoutLength = MegaManX3.LayoutLength;
                Tile32Count = MegaManX3.Tile32Count;
                Tile16Count = MegaManX3.Tile16Count;

                if (gameVersion == GameVersion.NA)
                {
                    LoadTileSetInfoOffset = MegaManX3.NA.LoadTileSetInfoOffset;
                    CompressedTileInfoOffset = MegaManX3.NA.CompressedTileInfoOffset;
                    CheckpointOffset = MegaManX3.NA.CheckpointOffset;
                    BackgroundTileInfoOffset = MegaManX3.NA.BackgroundTileInfoOffset;
                    BackgroundPaletteOffset = MegaManX3.NA.BackgroundPaletteOffset;
                    ObjectSpriteInfoOffset = MegaManX3.NA.ObjectSpriteInfoOffset;
                }
                else
                {
                    LoadTileSetInfoOffset = MegaManX3.JP.LoadTileSetInfoOffset;
                    CompressedTileInfoOffset = MegaManX3.JP.CompressedTileInfoOffset;
                    CheckpointOffset = MegaManX3.JP.CheckpointOffset;
                    BackgroundTileInfoOffset = MegaManX3.JP.BackgroundTileInfoOffset;
                    BackgroundPaletteOffset = MegaManX3.JP.BackgroundPaletteOffset;
                    ObjectSpriteInfoOffset = MegaManX3.JP.ObjectSpriteInfoOffset;
                }
            }

            if (expanded)
                AssignExpand();
        }
        public static void AssignExpand()
        {
            ScreenCount = (int[,])ScreenCount.Clone();
            Tile32Count = (int[,])Tile32Count.Clone();
            Tile16Count = (int[,])Tile32Count.Clone();

            for (int i = 0; i < PlayableLevelsCount; i++)
            {
                if (Id == GameId.MegaManX)
                    ScreenCount[i, 0] = ExpandMaxScreens[0];
                else
                    ScreenCount[i, 0] = ExpandMaxScreens2[0];
                ScreenCount[i, 1] = ExpandMaxScreens[1];

                Tile32Count[i, 0] = ExpandMaxTiles32[0];
                Tile32Count[i, 1] = ExpandMaxTiles32[1];

                Tile16Count[i, 0] = ExpandMaxTiles16;
                Tile16Count[i, 0] = ExpandMaxTiles16;
            }
        }
    }
}
