using System;
using System.Text;

namespace TeheManX_Editor
{
    static class SNES
    {
        #region Fields
        public static byte[] rom;
        public static bool edit = false;
        public static bool expanded = false;
        public static string savePath;
        public static DateTime date; //Save Date
        #endregion Fields

        #region Methods
        public static bool IsValidRom(byte [] rom)
        {
            bool tempExpand = rom.Length >= 0x400000 && Encoding.ASCII.GetString(rom, 0x3FFFF0, 6) == "POGYOU";

            if (Encoding.ASCII.GetString(rom, 0x7FC0, 10) == "MEGAMAN X ")
            {
                Const.AssignProperties(Const.GameId.MegaManX, Const.GameVersion.NA, tempExpand);
            }
            else if (Encoding.ASCII.GetString(rom, 0x7FC0, 10) == "ROCKMAN X ")
            {
                Const.AssignProperties(Const.GameId.MegaManX, Const.GameVersion.JP, tempExpand);
            }
            else if (Encoding.ASCII.GetString(rom, 0x7FC0, 10) == "MEGAMAN X2")
            {
                Const.AssignProperties(Const.GameId.MegaManX2, Const.GameVersion.NA, tempExpand);
            }
            else if (Encoding.ASCII.GetString(rom, 0x7FC0, 10) == "ROCKMAN X2")
            {
                Const.AssignProperties(Const.GameId.MegaManX2, Const.GameVersion.JP, tempExpand);
            }
            else if (Encoding.ASCII.GetString(rom, 0x7FC0, 10) == "MEGAMAN X3")
            {
                Const.AssignProperties(Const.GameId.MegaManX3, Const.GameVersion.NA, tempExpand);
            }
            else if (Encoding.ASCII.GetString(rom, 0x7FC0, 10) == "ROCKMAN X3")
            {
                Const.AssignProperties(Const.GameId.MegaManX3, Const.GameVersion.JP, tempExpand);
            }
            else
                return false;
            expanded = tempExpand;
            return true;
        }
        public static int GetSelectedTile(int c, double width, int d)
        {
            int i = (int)width;
            int e = i / d;
            return c / e;
        }
        public static int CpuToOffset(int cpu)
        {
            cpu &= 0xFFFFFF;
            return cpu % 0x8000 + (cpu >> 16) % 0x80 * 0x8000;
        }
        public static int CpuToOffset(int cpu , int bank)
        {
            cpu &= 0x7FFF;
            return cpu + ((bank & 0x7F) * 0x8000);
        }
        public static int OffsetToCpu(int offset)
        {
            offset &= 0xFFFFFF;
            int bank = (offset / 0x8000) & 0x7F;
            int addr = offset % 0x8000;
            return (bank << 16) | (addr + 0x8000);
        }
    #endregion Methods
    }
}
