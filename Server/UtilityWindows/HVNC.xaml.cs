using Server.UtilityWindows.Interface;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media.Animation;

namespace Server.UtilityWindows
{
    public partial class HVNC :Window, IUtilityWindow
    {
        private bool isDragging = false;
        private System.Windows.Point clickPosition;
        Storyboard triggerd;
        Storyboard default_Down;
        Storyboard default_Up;

        public HVNC()
        {
            InitializeComponent();

            triggerd = (FindResource("ControlPanel_Triggered") as Storyboard);
            default_Down = (FindResource("ControlPanel_Default_Down") as Storyboard);
            default_Up = (FindResource("ControlPanel_Default_Up") as Storyboard);
        }

        private void controlPanel_TBTN_RightMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.RightButton == MouseButtonState.Pressed)
            {
                isDragging = true;
                clickPosition = e.GetPosition(this);
                (sender as ToggleButton).CaptureMouse();
            }
        }

        private void controlPanel_TBTN_MouseMove(object sender, MouseEventArgs e)
        {
            if (isDragging)
            {
                System.Windows.Point currentPosition = e.GetPosition(this);
                double deltaY = currentPosition.Y - clickPosition.Y;

                if (currentPosition.Y < this.ActualHeight / 2)
                {
                    if (controlPanel_Grid.VerticalAlignment != VerticalAlignment.Top)
                    {
                        controlPanel_TBTN.IsChecked = false;
                        controlPanel_Grid.VerticalAlignment = VerticalAlignment.Top;
                        BeginStoryboard((Storyboard)this.Resources["up"]);
                    }
                }
                else
                {
                    if (controlPanel_Grid.VerticalAlignment != VerticalAlignment.Bottom)
                    {
                        controlPanel_TBTN.IsChecked = false;
                        controlPanel_Grid.VerticalAlignment = VerticalAlignment.Bottom;
                        BeginStoryboard((Storyboard)this.Resources["down"]);
                    }
                }
            }
        }

        private void controlPanel_TBTN_RightMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (isDragging)
            {
                isDragging = false;
                (sender as ToggleButton).ReleaseMouseCapture();
            }
        }

        private void controlPanel_TBTN_Checked(object sender, RoutedEventArgs e)
        {
            triggerd.Begin();
        }

        private void controlPanel_TBTN_Unchecked(object sender, RoutedEventArgs e)
        {
            if (controlPanel_Grid.VerticalAlignment == VerticalAlignment.Top)
            {
                default_Up.Begin();
            }

            if (controlPanel_Grid.VerticalAlignment == VerticalAlignment.Bottom)
            {
                default_Down.Begin();
            }
        }

        private void Window_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            // Kolla om klicket var utanför ToggleButton och Border
            if (!IsClickInsideElement(screenControl_TBTN, e) && !IsClickInsideElement(screenControl, e))
            {
                screenControl_TBTN.IsChecked = false;
            }
            if (!IsClickInsideElement(randomControl_TBTN, e) && !IsClickInsideElement(randomControl, e))
            {
                randomControl_TBTN.IsChecked = false;
            }
        }

        private bool IsClickInsideElement(FrameworkElement element, MouseButtonEventArgs e)
        {
            if (element == null)
            {
                return false;
            }

            System.Windows.Point clickPosition = e.GetPosition(element);
            return clickPosition.X >= 0 && clickPosition.X <= element.ActualWidth &&
                   clickPosition.Y >= 0 && clickPosition.Y <= element.ActualHeight;
        }

        #region TitleBar
        private bool isDraggingFromMaximized = false;
        private System.Windows.Point startDragPoint;

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var window = Window.GetWindow(this) as Window;
            if (e.ClickCount == 2)
            {
                MaximizeButton_Click(sender, e);
            }
            else
            {
                if (window != null && window.WindowState == WindowState.Maximized)
                {
                    isDraggingFromMaximized = true;
                    startDragPoint = e.GetPosition(this);
                }
                else
                {
                    window?.DragMove();
                }
            }
        }

        private void TitleBar_MouseMove(object sender, MouseEventArgs e)
        {
            if (isDraggingFromMaximized)
            {
                var window = Window.GetWindow(this) as Window;
                if (window == null) return;

                System.Windows.Point currentPoint = e.GetPosition(this);

                if (e.LeftButton == MouseButtonState.Pressed &&
                    (Math.Abs(currentPoint.X - startDragPoint.X) > 10 || Math.Abs(currentPoint.Y - startDragPoint.Y) > 10))
                {
                    isDraggingFromMaximized = false;
                    window.WindowState = WindowState.Normal;

                    var mousePosition = e.GetPosition(window);
                    window.Left = mousePosition.X - (startDragPoint.X * window.RestoreBounds.Width / ActualWidth);
                    window.Top = mousePosition.Y - (startDragPoint.Y * window.RestoreBounds.Height / ActualHeight);
                    window.DragMove();
                }
            }
        }

        private void TitleBar_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            isDraggingFromMaximized = false;
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            var window = Window.GetWindow(this);
            if (window != null)
            {
                window.WindowState = WindowState.Minimized;
            }
        }

        private void MaximizeButton_Click(object sender, RoutedEventArgs e)
        {
            var window = Window.GetWindow(this);
            if (window != null)
            {
                window.WindowState = window.WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            var window = Window.GetWindow(this);
            window?.Close();
        }
        #endregion

    }
}
