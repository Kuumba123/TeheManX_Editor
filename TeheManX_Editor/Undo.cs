using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
            X16
        }
        #endregion Constants

        #region Properties
        public UndoType type;
        internal byte[] data;
        #endregion Properties

        #region Methods
        public static void ApplyUndo(Undo undo)
        {
            switch ((UndoType)undo.type)
            {
                case UndoType.Layout:
                    undo.ApplyLayoutUndo();
                    break;
                case UndoType.Screen:
                    undo.ApplyScreenUndo();
                    break;
                case UndoType.X32:
                    //undo.ApplyX32Undo();
                    break;
                case UndoType.X16:
                    //undo.ApplyX16Undo();
                    break;
            }
        }
        /*Layout Undo*/
        internal static Undo CreateLayoutUndo(int offset)
        {
            Undo undo = new Undo();
            undo.type = 0; // Layout Undo Type
            byte[] undoData = new byte[0x6];
            BinaryPrimitives.WriteInt32LittleEndian(undoData.AsSpan(0), offset);
            undoData[4] = (byte)Level.Id;
            undoData[5] = Level.Layout[Level.Id, Level.BG, offset];
            undo.data = undoData;
            undo.type = UndoType.Layout;
            return undo;
        }
        internal void ApplyLayoutUndo()
        {
            int offset = BinaryPrimitives.ReadInt32LittleEndian(this.data.AsSpan(0));
            int layoutId = this.data[4];
            byte previousTile = this.data[5];
            byte currentTile = Level.Layout[layoutId, Level.BG, offset];
            Level.Layout[layoutId, Level.BG, offset] = previousTile;
            SNES.edit = true;

            if (Level.BG == layoutId)
            {
                MainWindow.window.layoutE.DrawLayout();
                MainWindow.window.enemyE.DrawLayout();
            }
        }
        /*Screen Undo*/
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
                MainWindow.window.screenE.DrawScreen16();
            }


        }
        #endregion Methods
    }
}
