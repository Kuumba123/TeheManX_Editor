namespace TeheManX_Editor
{
    public class Enemy
    {
        #region Properties
        public short X {  get; set; }
        public short Y { get; set; }
        public byte Id { get; set; }
        public byte SubId { get; set; }
        public byte Type { get; set; }
        public byte Column { get; set; } //just to make exporting easier
        #endregion Properties

        #region Constructors
        public Enemy()
        {
        }
        #endregion Constructors
    }
}
