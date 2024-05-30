using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Server
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Storyboard ImpactSidePanel;
        private Storyboard ExpandSidePanel;
        public MainWindow()
        {
            InitializeComponent();

            ImpactSidePanel = (Storyboard)FindResource("Impact_SidePanel");
            ExpandSidePanel = (Storyboard)FindResource("Expand_SidePanel");

            SizeChanged += MainWindow_SizeChanged;

            if (Width <= 1100)
            {
                ImpactSidePanel.Begin(this);
            }
            else
            {
                ExpandSidePanel.Begin(this);
            }
        }

        private void MainWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (Width <= 1100)
            {
                ImpactSidePanel.Begin(this);
            }
            else
            {
                ExpandSidePanel.Begin(this);
            }
        }

        #region Title Bar
        private bool isDraggingFromMaximized = false;
        private Point startDragPoint;

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void MaximizeButton_Click(object sender, RoutedEventArgs e)
        {
            if (WindowState == WindowState.Maximized)
            {
                WindowState = WindowState.Normal;
            }
            else
            {
                WindowState = WindowState.Maximized;
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                MaximizeButton_Click(sender, e);
            }
            else
            {
                if (WindowState == WindowState.Maximized)
                {
                    isDraggingFromMaximized = true;
                    startDragPoint = e.GetPosition(this);
                }
                else
                {
                    DragMove();
                }
            }
        }

        private void TitleBar_MouseMove(object sender, MouseEventArgs e)
        {
            if (isDraggingFromMaximized)
            {
                Point currentPoint = e.GetPosition(this);

                if (e.LeftButton == MouseButtonState.Pressed && (Math.Abs(currentPoint.X - startDragPoint.X) > 10 || Math.Abs(currentPoint.Y - startDragPoint.Y) > 10))
                {
                    isDraggingFromMaximized = false;
                    WindowState = WindowState.Normal;

                    Left = currentPoint.X - (startDragPoint.X * RestoreBounds.Width / ActualWidth);
                    Top = currentPoint.Y - (startDragPoint.Y * RestoreBounds.Height / ActualHeight);
                    DragMove();
                }
            }
        }

        private void TitleBar_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            isDraggingFromMaximized = false;
        }
        #endregion
    }
}