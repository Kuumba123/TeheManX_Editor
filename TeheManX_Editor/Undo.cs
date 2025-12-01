using System;
using System.Buffers.Binary;
using TeheManX_Editor.Forms;

namespace TeheManX_Editor
{
    public class Undo
    {

        #region Constants
        public enum UndoType
        {
            Layout,
            Screen,
            X32,
            X16,
            Collision
        }
        #endregion Constants

        #region Properties
        public UndoType type;
        internal byte[] data;
        #endregion Properties

        #region Methods
        public static void ApplyUndo(Undo undo)
        {
            switch (undo.type)
            {
                case UndoType.Layout:
                    undo.ApplyLayoutUndo();
                    break;
                case UndoType.Screen:
                    undo.ApplyScreenUndo();
                    break;
                case UndoType.X32:
                    undo.ApplyTile32Undo();
                    break;
                case UndoType.X16:
                    undo.ApplyTile16Undo();
                    break;
                case UndoType.Collision:
                    undo.ApplyTileCollisionUndo();
                    break;
            }
        }
        internal static Undo CreateLayoutUndo(int offset)
        {
            Undo undo = new Undo();
            byte[] undoData = new byte[0x6];
            BinaryPrimitives.WriteInt32LittleEndian(undoData.AsSpan(0), offset);
            undoData[4] = (byte)Level.BG;
            undoData[5] = Level.Layout[Level.Id, Level.BG, offset];
            undo.data = undoData;
            undo.type = UndoType.Layout;
            return undo;
        }
        internal void ApplyLayoutUndo()
        {
            int offset = BinaryPrimitives.ReadInt32LittleEndian(this.data.AsSpan(0));
            int layoutId = this.data[4];
            byte previousScreen = this.data[5];
            Level.Layout[Level.Id, layoutId, offset] = previousScreen;
            SNES.edit = true;

            if (Level.BG == layoutId)
            {
                MainWindow.window.layoutE.DrawLayout();
                MainWindow.window.enemyE.DrawLayout();
                MainWindow.layoutWindow.UpdateLayoutGrid();
            }
        }
        internal static Undo CreateScreenUndo(byte screen, byte x, byte y)
        {
            byte[] undoData = new byte[7];
            undoData[0] = screen;
            undoData[1] = x;
            undoData[2] = y;
            undoData[3] = 0; // Screen Undo Type
            undoData[4] = (byte)Level.BG;

            int offset = SNES.CpuToOffset(BitConverter.ToInt32(SNES.rom, Const.ScreenDataPointersOffset[Level.BG] + Level.Id * 3));

            ushort tileId = BitConverter.ToUInt16(SNES.rom, offset + screen * 0x80 + x * 2 + y * 16);
            BinaryPrimitives.WriteUInt16LittleEndian(undoData.AsSpan(5), tileId);

            return new Undo() { data = undoData, type = UndoType.Screen};
        }
        internal static Undo CreateScreenUndo16(byte screen, byte x, byte y)
        {
            byte[] undoData = new byte[6];
            undoData[0] = screen;
            undoData[1] = x;
            undoData[2] = y;
            undoData[3] = 0; // Screen Undo Type


            ushort tileId = BitConverter.ToUInt16(MainWindow.window.screenE.screenData16,screen * 0x200 + x * 2 + y * 32);
            BinaryPrimitives.WriteUInt16LittleEndian(undoData.AsSpan(4), tileId);

            return new Undo() { data = undoData, type = UndoType.Screen };
        }
        internal static Undo CreateGroupScreenUndo16(byte screen, byte x, byte y, byte spanC,byte spanR)
        {
            byte[] undoData = new byte[6 + spanC * 2 + spanR * 32];
            undoData[0] = screen;
            undoData[1] = x;
            undoData[2] = y;
            undoData[3] = 1; // Screen Undo Type
            undoData[4] = spanC;
            undoData[5] = spanR;

            int writeOffset = 6;

            for (int r = 0; r < spanR; r++)
            {
                for (int c = 0; c < spanC; c++)
                {
                    if (x + c > 15)
                        continue;
                    if (y + r > 15)
                        continue;
                    ushort tileId = BitConverter.ToUInt16(MainWindow.window.screenE.screenData16, screen * 0x200 + (x + c) * 2 + (y + r) * 32);
                    BinaryPrimitives.WriteUInt16LittleEndian(undoData.AsSpan(writeOffset), tileId);
                    writeOffset += 2;
                }
            }
            return new Undo() { data = undoData, type = UndoType.Screen };
        }
        internal void ApplyScreenUndo()
        {
            SNES.edit = true;
            if (!MainWindow.window.screenE.mode16)
            {
                byte screen = this.data[0];
                byte x = this.data[1];
                byte y = this.data[2];
                byte layer = this.data[4];
                ushort previousTileId = BinaryPrimitives.ReadUInt16LittleEndian(this.data.AsSpan(5));
                int offset = x * 2 + y * 16 + screen * 0x80;
                offset += SNES.CpuToOffset(BitConverter.ToInt32(SNES.rom, Const.ScreenDataPointersOffset[layer] + Level.Id * 3));
                BinaryPrimitives.WriteUInt16LittleEndian(SNES.rom.AsSpan(offset), previousTileId);
                MainWindow.window.layoutE.DrawLayout();
                MainWindow.window.layoutE.DrawScreen();
                MainWindow.window.enemyE.DrawLayout();
                MainWindow.window.screenE.DrawScreen();
            }
            else
            {
                byte screen = this.data[0];
                byte x = this.data[1];
                byte y = this.data[2];

                byte type = this.data[3];

                if (type == 0)
                {
                    ushort previousTileId = BinaryPrimitives.ReadUInt16LittleEndian(this.data.AsSpan(4));
                    int offset = x * 2 + y * 32 + screen * 0x200;
                    BinaryPrimitives.WriteUInt16LittleEndian(MainWindow.window.screenE.screenData16.AsSpan(offset), previousTileId);
                }
                else if (type == 1)
                {
                    int offset = x * 2 + y * 32 + screen * 0x200;
                    int readOffset = 6;

                    for (int r = 0; r < data[5]; r++)
                    {
                        for (int c = 0; c < data[4]; c++)
                        {
                            if (x + c > 15)
                                continue;
                            if (y + r > 15)
                                continue;
                            ushort previousTileId = BinaryPrimitives.ReadUInt16LittleEndian(this.data.AsSpan(readOffset));
                            readOffset += 2;
                            int dumpOffset = (x + c) * 2 + (y + r) * 32 + screen * 0x200;
                            BinaryPrimitives.WriteUInt16LittleEndian(MainWindow.window.screenE.screenData16.AsSpan(dumpOffset), previousTileId);
                        }
                    }
                }
                MainWindow.window.screenE.Update32x32TileList();
                MainWindow.window.screenE.Update32x32TileCountText();
                MainWindow.window.screenE.DrawScreen16();
            }
        }
        internal static Undo CreateTile32Undo(ushort tileId32,ulong val)
        {
            byte[] undoData = new byte[11];
            BinaryPrimitives.WriteUInt16LittleEndian(undoData.AsSpan(), tileId32);
            BinaryPrimitives.WriteUInt64LittleEndian(undoData.AsSpan(2), val);
            undoData[10] = (byte)Level.BG;

            return new Undo() { data = undoData, type = UndoType.X32 };
        }
        internal void ApplyTile32Undo()
        {
            ushort tileId32 = BinaryPrimitives.ReadUInt16LittleEndian(data.AsSpan());
            ulong previousVal = BinaryPrimitives.ReadUInt64LittleEndian(data.AsSpan(2));

            byte layerBG = data[10];

            int offset = SNES.CpuToOffset(BitConverter.ToInt32(SNES.rom, Const.Tile32DataPointersOffset[layerBG] + Level.Id * 3));
            BinaryPrimitives.WriteUInt64LittleEndian(SNES.rom.AsSpan(offset + tileId32 * 8), previousVal);
            SNES.edit = true;


            MainWindow.window.layoutE.DrawLayout();
            MainWindow.window.layoutE.DrawScreen();
            if (!MainWindow.window.screenE.mode16)
            {
                MainWindow.window.screenE.DrawScreen();
                MainWindow.window.screenE.DrawTiles();
                MainWindow.window.screenE.DrawTile();
            }
            MainWindow.window.tile32E.DrawTiles();
            if (MainWindow.window.tile32E.selectedTile == tileId32)
            {
                MainWindow.window.tile32E.UpdateTile32Ints(offset);
                MainWindow.window.tile32E.UpdateTile16SelectionUI();
                MainWindow.window.tile32E.DrawTile();
            }
            MainWindow.window.enemyE.DrawLayout();
        }
        internal static Undo CreateTile16Undo(ushort tileId16, ulong val)
        {
            byte[] undoData = new byte[11];
            BinaryPrimitives.WriteUInt16LittleEndian(undoData.AsSpan(), tileId16);
            BinaryPrimitives.WriteUInt64LittleEndian(undoData.AsSpan(2), val);
            undoData[10] = (byte)Level.BG;

            return new Undo() { data = undoData, type = UndoType.X16 };
        }
        internal void ApplyTile16Undo()
        {
            ushort tileId16 = BinaryPrimitives.ReadUInt16LittleEndian(data.AsSpan());
            ulong previousVal = BinaryPrimitives.ReadUInt64LittleEndian(data.AsSpan(2));

            byte layerBG = data[10];

            int offset = SNES.CpuToOffset(BitConverter.ToInt32(SNES.rom, Const.Tile16DataPointersOffset[layerBG] + Level.Id * 3));
            BinaryPrimitives.WriteUInt64LittleEndian(SNES.rom.AsSpan(offset + tileId16 * 8), previousVal);
            SNES.edit = true;


            MainWindow.window.layoutE.DrawLayout();
            MainWindow.window.layoutE.DrawScreen();
            if (!MainWindow.window.screenE.mode16)
            {
                MainWindow.window.screenE.DrawScreen();
                MainWindow.window.screenE.DrawTiles();
                MainWindow.window.screenE.DrawTile();
            }
            MainWindow.window.tile32E.DrawTiles();
            MainWindow.window.tile32E.DrawTile();
            MainWindow.window.tile16E.Draw16xTiles();
            if (MainWindow.window.tile16E.selectedTile == tileId16)
            {
                MainWindow.window.tile16E.UpdateTileAttributeUI();
                MainWindow.window.tile16E.UpdateTile8SelectionUI();
                MainWindow.window.tile16E.DrawTile();
            }
            MainWindow.window.enemyE.DrawLayout();
        }
        internal static Undo CreateCollisionUndo(ushort tileId16, byte val)
        {
            byte[] undoData = new byte[3];
            undoData[0] = val;
            BinaryPrimitives.WriteUInt16LittleEndian(undoData.AsSpan(1), tileId16);

            return new Undo() { data = undoData , type = UndoType.Collision};
        }
        internal void ApplyTileCollisionUndo()
        {
            int offset = SNES.CpuToOffset(BitConverter.ToInt32(SNES.rom, Const.TileCollisionDataPointersOffset + Level.Id * 3));
            ushort tileId16 = BinaryPrimitives.ReadUInt16LittleEndian(data.AsSpan(1));
            offset += tileId16;
            SNES.rom[offset] = data[0];
            SNES.edit = true;

            if (MainWindow.window.tile16E.selectedTile == tileId16)
                MainWindow.window.tile16E.collisionInt.Value = data[0];
        }
        #endregion Methods
    }
}
