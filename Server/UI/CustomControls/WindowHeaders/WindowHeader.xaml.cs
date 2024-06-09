using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Server.UI.CustomControls.WindowHeaders
{
    public partial class WindowHeader : UserControl
    {
        public WindowHeader()
        {
            InitializeComponent();
        }

        private bool isDraggingFromMaximized = false;
        private Point startDragPoint;

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

                Point currentPoint = e.GetPosition(this);

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
            if (window != null)
            {
                window.Close();
            }
        }
    }
}
