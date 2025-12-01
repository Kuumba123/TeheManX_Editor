using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace TeheManX_Editor.Forms
{
    /// <summary>
    /// Interaction logic for LayoutWindow.xaml
    /// </summary>
    public partial class LayoutWindow : Window
    {
        #region Fields
        public static bool isOpen;
        #endregion Fields

        #region Constructors
        public LayoutWindow()
        {
            InitializeComponent();

            BuildLayoutGrid();
        }
        #endregion Constructors

        #region Methods
        private void BuildLayoutGrid()
        {
            // Total is 33x33 = 1089 cells
            int size = 33;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    UIElement element;

                    // Corner Cell (Top Left)
                    if (x == 0 && y == 0)
                    {
                        element = new TextBlock
                        {
                            Text = "",
                            Width = 25,
                            Height = 22,
                        };
                    }

                    // Top row: X-axis labels
                    else if (y == 0)
                    {
                        element = new TextBlock
                        {
                            Text = (x - 1).ToString("X2"),
                            Width = 25,
                            Height = 22,
                            FontSize = 18,
                            TextAlignment = TextAlignment.Center,
                            VerticalAlignment = VerticalAlignment.Center,
                            Background = Brushes.DarkSlateGray,
                            Foreground = Brushes.White
                        };
                    }

                    // Left column: Y-axis labels
                    else if (x == 0)
                    {
                        element = new TextBlock
                        {
                            Text = (y - 1).ToString("X2"),
                            Width = 25,
                            Height = 22,
                            FontSize = 18,
                            TextAlignment = TextAlignment.Center,
                            VerticalAlignment = VerticalAlignment.Center,
                            Background = Brushes.DarkSlateGray,
                            Foreground = Brushes.White
                        };
                    }

                    // Actual stage layout cell (32×32 area)
                    else
                    {
                        int index = (y - 1) * 32 + (x - 1);
                        byte value = Level.Layout[Level.Id,Level.BG, index];

                        element = new TextBlock
                        {
                            Text = value.ToString("X"),
                            Width = 25,
                            Height = 22,
                            FontSize = 18,
                            TextAlignment = TextAlignment.Center,
                            VerticalAlignment = VerticalAlignment.Center,
                            Background = new SolidColorBrush(Color.FromRgb(0x10, 0x10, 0x10)),
                            Foreground = Brushes.White
                        };
                        element.Uid = index.ToString();
                        element.MouseUp += (s, e) =>
                        {
                            TextBlock txtBlock = s as TextBlock;
                            int layoutIndex = int.Parse(txtBlock.Uid);
                            int x = layoutIndex & 0x1F;
                            int y = layoutIndex >> 5;

                            if (x > 0x1D) x = 0x1D;

                            if (e.ChangedButton == MouseButton.Left) // goto in Layout Editor
                            {
                                if (y > 0x1D) y = 0x1D;
                                MainWindow.window.layoutE.viewerX = x << 8;
                                MainWindow.window.layoutE.viewerY = y << 8;
                                MainWindow.window.layoutE.DrawLayout();
                                MainWindow.window.UpdateViewrCam();
                            }
                            else // goto in Enemy Editor
                            {
                                if (y > 0x1E) y = 0x1E;
                                MainWindow.window.enemyE.viewerX = x << 8;
                                MainWindow.window.enemyE.viewerY = y << 8;
                                MainWindow.window.enemyE.DrawLayout();
                                MainWindow.window.enemyE.DrawEnemies();
                                MainWindow.window.UpdateEnemyViewerCam();
                            }
                        };
                    }

                    layoutGrid.Children.Add(element);
                }
            }
        }
        internal void UpdateLayoutGrid()
        {
            for (int i = 0; i < layoutGrid.Children.Count; i++)
            {
                TextBlock textBlock = layoutGrid.Children[i] as TextBlock;
                if (textBlock.Uid == null || textBlock.Uid == "") continue;

                int layoutOffset = int.Parse(textBlock.Uid);
                textBlock.Text = Level.Layout[Level.Id, Level.BG, layoutOffset].ToString("X");
            }
        }
        #endregion Methods

        #region Events
        private void Window_ContentRendered(object sender, EventArgs e)
        {
            isOpen = true;
        }
        private void Window_Closed(object sender, EventArgs e)
        {
            isOpen = false;
        }
        #endregion Events
    }
}
