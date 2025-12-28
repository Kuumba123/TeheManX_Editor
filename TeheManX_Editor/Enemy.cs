namespace TeheManX_Editor
{
    public class Enemy
    {
        #region Properties
        public short X;
        public short Y;
        public byte Id;
        public byte SubId;
        public byte Type;
        public byte Column; //just to make exporting easier
        #endregion Properties

        #region Constructors
        public Enemy()
        {
        }
        #endregion Constructors
    }
}
