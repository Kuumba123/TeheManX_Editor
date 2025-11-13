using System.Windows.Controls;
using System.Windows.Media;

namespace TeheManX_Editor.Forms
{
    /// <summary>
    /// Interaction logic for EnemyLabel.xaml
    /// </summary>
    public partial class EnemyLabel : UserControl
    {
        #region Constructors
        public EnemyLabel()
        {
            InitializeComponent();
        }
        #endregion Constructors

        #region Methods
        public void AssignTypeBorder(byte type)
        {
            switch (type)
            {
                case 0: //Item Objects
                    this.border.BorderBrush = Brushes.Blue;
                    break;
                case 1: //Misc Objects
                    this.border.BorderBrush = Brushes.Purple;
                    break;
                case 2: //Effect Objects
                    this.border.BorderBrush = Brushes.HotPink;
                    break;
                case 3: //Main Objects
                    this.border.BorderBrush = Brushes.Red;
                    break;
                case 4: //Weapon Objects
                    this.border.BorderBrush = Brushes.Green;
                    break;
                case 5: //Shot Objects
                    this.border.BorderBrush = Brushes.Orange;
                    break;
                default:
                    this.border.BorderBrush = Brushes.Black;
                    break;
            }
        }
        #endregion Methods
    }
}
