namespace TeheManX_Editor
{
    static class Const
    {
        public static readonly string ReproURL = "https://api.github.com/repos/Kuumba123/TeheManX_Editor/releases/latest";
        public const string EditorVersion = "1.0";
        public static readonly string[] PastVersions =
        {
            "1.0"
        };
        public const int EnemyOffset = 8; //For Enemy Labels
        public const int MaxUndo = 512;
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
        public static readonly int[] ExpandMaxTiles32 = { 0x540, 0x300 };
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

        public static int LoadTileSetBank;
        public static int LoadTileSetInfoOffset;
        public static int LoadTileSetStageBase; //Base Id for Stage Load CHR Info

        public static int CompressedTileInfoOffset;

        public static int EnemyPointersOffset;
        public static int EnemyDataBank;

        public static int MaxTotalCheckpoints;
        public static int CheckpointOffset;
        public static int[] MaxCheckpoints = new int[MaxLevels];

        public static int BackgroundTileInfoOffset;

        public static int[] LayoutPointersOffset = new int[2]; //Layout Pointers Offset to compressed data
        public static int[] ScreenDataPointersOffset = new int[2];
        public static int[] Tile32DataPointersOffset = new int[2];
        public static int[] Tile16DataPointersOffset = new int[2];
        public static int TileCollisionDataPointersOffset;

        public static byte[,] LayoutLength;
        public static int[,] ScreenCount = new int[MaxLevels, 2];
        public static int[,] Tile32Count;
        public static int[,] Tile16Count;
        public static int[] EnemiesLength;

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

            public const int MaxTotalCheckpoints = 44;

            public const int MaxTotalCameraSettings = 0x46; //ID Used in Storm Eagle Stage
            public const int CameraSettingsBank = 0x86;
            public const int CameraTriggersLength = 0x7EE; //size includes all the pointers

            public static readonly int[] LayoutPointersOffset = { 0x30D24, 0x30F4F }; //Layout Pointers Offset to compressed data
            public static readonly int[] ScreenDataPointersOffset = { 0x30D93, 0x30FBE };
            public static readonly int[] Tile32DataPointersOffset = { 0x30E02, 0x3102D };
            public static readonly int[] Tile16DataPointersOffset = { 0x30E71, 0x3109C };
            public static int TileCollisionDataPointersOffset = 0x30EE0;

            public const int BankCount = 48;
            internal class NA // North America Version
            {
                public const int LoadTileSetInfoOffset = 0x3756F;
                public const int CompressedTileInfoOffset = 0x376F7;
                public const int CheckpointOffset = 0x32780;
                public const int CameraTriggersOffset = 0x364E2;
                public const int CameraSettingsOffset = 0x36CD0;
                public const int BackgroundTileInfoOffset = 0x321D5;
            }
            internal class JP // Japanese Version
            {
                public const int LoadTileSetInfoOffset = 0x37572;
                public const int CompressedTileInfoOffset = 0x376FA;
                public const int CheckpointOffset = 0x32783;
                public const int CameraTriggersOffset = 0x364E5;
                public const int CameraSettingsOffset = 0x36CD3;
                public const int BackgroundTileInfoOffset = 0x321D8;
            }
            public static readonly byte[,] LayoutLength = new byte[0x25, 2]
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
                    {0x363 , 0x388 },
                    {0x2D7 , 0x290 },
                    {0x356 , 0x350 },
                    {0x365 , 0x35D },
                    {0x325 , 0x324 },
                    {0x2D4 , 0x2C5 },
                    {0x2AE , 0x2AD },
                    {0x36F , 0x33F },
                    {0x2E0 , 0x2E2 },
                    {0x1EC , 0x1DF },
                    {0x26A , 0x251 },
                    {0x1DC , 0x1B8 },
                    {0x106 , 0xFA  },
                    {0x325 , 0x350 },
                    {0x1C ,  0x38  },
                    {0x1C ,  0x38  },
                    {0x1C ,  0x38  },
                    {0x1C ,  0x38  },
                    {0x1C ,  0x38  },
                    {0x1C ,  0x38  },
                    {0x1C ,  0x38  },
                    {0x1C ,  0x38  },
                    {0x1C ,  0x38  },
                    {0x1C ,  0x38  },
                    {0x1C ,  0x38  },
                    {0x1C ,  0x38  },
                    {0x1C ,  0x38  },
                    {0x6F ,  0x68  },
                    {0x6F ,  0x68  },
                    {0x6F ,  0x68  },
                    {0x6F ,  0x68  },
                    {0x6F ,  0x68  },
                    {0x6F ,  0x68  },
                    {0x6F ,  0x68  },
                    {0x6F ,  0x68  },
                    {0x6F ,  0x68  },
                    {0xA8 ,  0x68  }
            };
            public static readonly int[] EnemiesLength = 
            {
                0x2C8,
                0x211,
                0x250,
                0x4B3,
                0x2EA,
                0x32C,
                0x2E2,
                0x260,
                0x2D2,
                0x37F,
                0x254,
                0x2B2,
                0x27
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

            public const int MaxTotalCheckpoints = 64;

            public const int MaxTotalCameraSettings = 0x7E;
            public const int CameraTriggersLength = 0xEFB; //size includes all the pointers
            public const int CameraSettingsBank = 0x2;
            public const int CameraTriggersOffset = 0x16BE9;
            public const int CameraSettingsOffset = 0x17AE4;

            public static readonly int[] LayoutPointersOffset = { 0x030888, 0x30AB3 };
            public static readonly int[] ScreenDataPointersOffset = { 0x0308F7, 0x30B22 };
            public static readonly int[] Tile32DataPointersOffset = { 0x030966, 0x30B91 };
            public static readonly int[] Tile16DataPointersOffset = { 0x0309D5, 0x30C00 };
            public static int TileCollisionDataPointersOffset = 0x030A44;

            public const int BankCount = 48;
            internal class NA // North America Version
            {
                public const int LoadTileSetInfoOffset = 0x37831;
                public const int CompressedTileInfoOffset = 0x37A01;
                public const int EnemyPointersOffset = 0x14D3D1;
                public const int CheckpointOffset = 0x324C5;
                public const int BackgroundTileInfoOffset = 0x31D6A;
            }
            internal class JP // Japanese Version
            {
                public const int LoadTileSetInfoOffset = 0x37832;
                public const int CompressedTileInfoOffset = 0x37A02;
                public const int EnemyPointersOffset = 0x14D3D9;
                public const int CheckpointOffset = 0x324C6;
                public const int BackgroundTileInfoOffset = 0x31D6B;
            }
            public static readonly byte[,] LayoutLength = new byte[0xD, 2]
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
                    {0x3A3,  0x394 },
                    {0x3B3,  0x3B1 },
                    {0x2F0,  0x2E8 },
                    {0x2BA,  0x2B5 },
                    {0x2F3,  0x2F2 },
                    {0x28C,  0x26B },
                    {0x261,  0x2B5 },
                    {0x31A,  0x312 },
                    {0x2BE,  0x2B7 },
                    {0x113,  0x113 },
                    {0xC7 ,  0xC3  },
                    {0xCE ,  0x2B  },
                    {0x13B,  0x12F }
            };
            public static readonly int[] EnemiesLength =
            {
                0x235,
                0x4A7,
                0x338,
                0x489,
                0x310,
                0x382,
                0x3B6,
                0x3DA,
                0x45C,
                0x303,
                0x212,
                0x30F,
                0xBD
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

            public const int MaxTotalCheckpoints = 53;

            public const int MaxTotalCameraSettings = 0x90;
            public const int CameraTriggersLength = 0x1489; //size includes all the pointers
            public const int CameraSettingsBank = 0x3;
            public const int CameraTriggersOffset = 0x1DE43;
            public const int CameraSettingsOffset = 0x1F2CC;

            public static readonly int[] LayoutPointersOffset = { 0x309B3, 0x30BDE };
            public static readonly int[] ScreenDataPointersOffset = { 0x30A22, 0x30C4D };
            public static readonly int[] Tile32DataPointersOffset = { 0x30A91, 0x30CBC };
            public static readonly int[] Tile16DataPointersOffset = { 0x30B00, 0x30D2B };
            public static int TileCollisionDataPointersOffset = 0x30B6F;

            public const int BankCount = 64;

            public static readonly byte[,] LayoutLength = new byte[0x11, 2]
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
                    { 0x37C, 0x3FF  },
                    { 0x35D, 0x400  },
                    { 0x2F9, 0x2E5  },
                    { 0x34B, 0x33F  },
                    { 0x35A, 0x356  },
                    { 0x1DC, 0x24E  },
                    { 0x3FC, 0x39B  },
                    { 0x251, 0x3B0  },
                    { 0x333, 0x331  },
                    { 0x250, 0x283  },
                    { 0x2D5, 0x2DF  },
                    { 0x13F, 0x293  },
                    { 0x1F4, 0x1F3  },
                    { 0x114, 0x1B7  },
                    { 0x181, 0x293  },
                    { 0x2F9, 0x2E5  },
                    { 0x34B, 0x33F  }
            };
            public static readonly int[] EnemiesLength =
            {
                0x2F1,
                0x3B4,
                0x3A7,
                0x3D9,
                0x3DA,
                0x455,
                0x3C9,
                0x405,
                0x33B,
                0x22B,
                0x3CB,
                0x2BA,
                0x274,
                0xE6,
                0x2AC
            };
            internal class NA
            {
                public const int LoadTileSetInfoOffset = 0x373C3;
                public const int CompressedTileInfoOffset = 0x37732;
                public const int CheckpointOffset = 0x328E4;
                public const int BackgroundTileInfoOffset = 0x32085;
            }
            internal class JP
            {
                public const int LoadTileSetInfoOffset = 0x373C4;
                public const int CompressedTileInfoOffset = 0x37733;
                public const int CheckpointOffset = 0x328E5;
                public const int BackgroundTileInfoOffset = 0x32086;
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

                MaxTotalCheckpoints = MegaManX.MaxTotalCheckpoints;

                LayoutPointersOffset = MegaManX.LayoutPointersOffset;
                ScreenDataPointersOffset = MegaManX.ScreenDataPointersOffset;
                Tile32DataPointersOffset = MegaManX.Tile32DataPointersOffset;
                Tile16DataPointersOffset = MegaManX.Tile16DataPointersOffset;
                TileCollisionDataPointersOffset = MegaManX.TileCollisionDataPointersOffset;

                LayoutLength = MegaManX.LayoutLength;
                Tile32Count = MegaManX.Tile32Count;
                Tile16Count = MegaManX.Tile16Count;
                EnemiesLength = MegaManX.EnemiesLength;

                if (gameVersion == GameVersion.NA)
                {
                    LoadTileSetInfoOffset = MegaManX.NA.LoadTileSetInfoOffset;
                    CompressedTileInfoOffset = MegaManX.NA.CompressedTileInfoOffset;
                    CheckpointOffset = MegaManX.NA.CheckpointOffset;
                    BackgroundTileInfoOffset = MegaManX.NA.BackgroundTileInfoOffset;
                }
                else
                {
                    LoadTileSetInfoOffset = MegaManX.JP.LoadTileSetInfoOffset;
                    CompressedTileInfoOffset = MegaManX.JP.CompressedTileInfoOffset;
                    CheckpointOffset = MegaManX.JP.CheckpointOffset;
                    BackgroundTileInfoOffset = MegaManX.JP.BackgroundTileInfoOffset;
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

                MaxTotalCheckpoints = MegaManX2.MaxTotalCheckpoints;

                LayoutPointersOffset = MegaManX2.LayoutPointersOffset;
                ScreenDataPointersOffset = MegaManX2.ScreenDataPointersOffset;
                Tile32DataPointersOffset = MegaManX2.Tile32DataPointersOffset;
                Tile16DataPointersOffset = MegaManX2.Tile16DataPointersOffset;
                TileCollisionDataPointersOffset = MegaManX2.TileCollisionDataPointersOffset;

                LayoutLength = MegaManX2.LayoutLength;
                Tile32Count = MegaManX2.Tile32Count;
                Tile16Count = MegaManX2.Tile16Count;
                EnemiesLength = MegaManX2.EnemiesLength;

                if (gameVersion == GameVersion.NA)
                {
                    LoadTileSetInfoOffset = MegaManX2.NA.LoadTileSetInfoOffset;
                    CompressedTileInfoOffset = MegaManX2.NA.CompressedTileInfoOffset;
                    EnemyPointersOffset = MegaManX2.NA.EnemyPointersOffset;
                    CheckpointOffset = MegaManX2.NA.CheckpointOffset;
                    BackgroundTileInfoOffset = MegaManX2.NA.BackgroundTileInfoOffset;
                }
                else
                {
                    LoadTileSetInfoOffset = MegaManX2.JP.LoadTileSetInfoOffset;
                    CompressedTileInfoOffset = MegaManX2.JP.CompressedTileInfoOffset;
                    EnemyPointersOffset = MegaManX2.JP.EnemyPointersOffset;
                    CheckpointOffset = MegaManX2.JP.CheckpointOffset;
                    BackgroundTileInfoOffset = MegaManX2.JP.BackgroundTileInfoOffset;
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

                MaxTotalCheckpoints = MegaManX3.MaxTotalCheckpoints;

                LayoutPointersOffset = MegaManX3.LayoutPointersOffset;
                ScreenDataPointersOffset = MegaManX3.ScreenDataPointersOffset;
                Tile32DataPointersOffset = MegaManX3.Tile32DataPointersOffset;
                Tile16DataPointersOffset = MegaManX3.Tile16DataPointersOffset;
                TileCollisionDataPointersOffset = MegaManX3.TileCollisionDataPointersOffset;

                LayoutLength = MegaManX3.LayoutLength;
                Tile32Count = MegaManX3.Tile32Count;
                Tile16Count = MegaManX3.Tile16Count;
                EnemiesLength = MegaManX3.EnemiesLength;

                if (gameVersion == GameVersion.NA)
                {
                    LoadTileSetInfoOffset = MegaManX3.NA.LoadTileSetInfoOffset;
                    CompressedTileInfoOffset = MegaManX3.NA.CompressedTileInfoOffset;
                    CheckpointOffset = MegaManX3.NA.CheckpointOffset;
                    BackgroundTileInfoOffset = MegaManX3.NA.BackgroundTileInfoOffset;
                }
                else
                {
                    LoadTileSetInfoOffset = MegaManX3.JP.LoadTileSetInfoOffset;
                    CompressedTileInfoOffset = MegaManX3.JP.CompressedTileInfoOffset;
                    CheckpointOffset = MegaManX3.JP.CheckpointOffset;
                    BackgroundTileInfoOffset = MegaManX3.JP.BackgroundTileInfoOffset;
                }
            }
        }
    }
}
