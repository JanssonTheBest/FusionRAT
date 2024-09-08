using System.Windows.Controls.Primitives;
using Server.UtilityWindows.Interface;
using Server.CoreServerFunctionality;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Common.DTOs.MessagePack;
using System.Windows.Input;
using System.Drawing;
using System.Windows;
using System.Timers;
using System.IO;
using Server.VideoProcessing;
using System.Windows.Media;
using System.IO.Pipelines;
using FFmpeg.AutoGen;
using System.Windows.Controls;
using System.Windows.Media.Media3D;
using System.Collections.Concurrent;
using System.Buffers;

namespace Server.UtilityWindows
{

    public partial class RemoteDesktop : Window, IUtilityWindow
    {
        ServerSession _session;
        VideoStreamPlayer _videoStreamPlayer;
        WriteableBitmap _bitmap;
        Pipe _pipe;
        CancellationTokenSource cts = new CancellationTokenSource();
        CancellationToken ct;

        public RemoteDesktop(ServerSession session)
        {
            InitializeComponent();

            ct = cts.Token;
            _session = session;

            session.OnRemoteDesktop += HandlePacket;
            _pipe = new();

            Closed += WindowIsClosing;

            triggerd = (FindResource("ControlPanel_Triggered") as Storyboard);
            default_Down = (FindResource("ControlPanel_Default_Down") as Storyboard);
            default_Up = (FindResource("ControlPanel_Default_Up") as Storyboard);

            _session.SendPlugin(typeof(RemoteDesktopPlugin.Plugin));
        }

        private async void WindowIsClosing(object? sender, EventArgs e)
        {
            await Stop();
            _videoStreamPlayer = null;
            _session.OnRemoteDesktop -= HandlePacket;
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        SemaphoreSlim semaphore = new SemaphoreSlim(1, 1);
        private async void HandlePacket(object? sender, EventArgs e)
        {
            var dto = sender as RemoteDesktopDTO;
            if (dto?.Options != null)
            {
                AssignScreens(dto.Options);
                return;
            }

            if (dto?.LibAVFiles != null)
            {
                CopyAndSendDependecies();
                return;
            }

            if (!_videoStreamPlayer.IsPlaying)
            {
                return;
            }

            try
            {
                await semaphore.WaitAsync();
                await _pipe.Writer.WriteAsync(dto.VideoChunk);
                semaphore.Release();
            }
            catch (Exception ex)
            {
                semaphore.Release();
            }
        }


        private async Task Stop()
        {
            await _session.SendPacketAsync(new RemoteDesktopDTO());
            cts.Cancel();
            _videoStreamPlayer?.Stop();
            _pipe.Writer.Complete();
            _pipe.Reset();
        }

        private void AssignScreens(string[] screens)
        {
            foreach (var screen in screens)
            {
                var screenInfo = screen.Split('|');
                Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    Button button = new Button();
                    button.Content = string.Join(",", screenInfo);
                    button.HorizontalAlignment = HorizontalAlignment.Stretch;
                    button.VerticalAlignment = VerticalAlignment.Stretch;
                    button.Foreground = System.Windows.SystemColors.ControlLightBrush;
                    button.Background = System.Windows.SystemColors.ControlDarkDarkBrush;
                    button.Click += ScreenSelected;
                    screenControlPanel.Children.Add(button);
                });
            }

        }

        private async void ScreenSelected(object sender, RoutedEventArgs e)
        {
            if (_videoStreamPlayer != null)
            {
                if (_videoStreamPlayer.IsPlaying)
                {
                    await ChangeScreen(sender);
                    return;
                }
            }

            Button button = (Button)sender;
            string[] options = ((string)button.Content).Split(",");
            _bitmap = new(Convert.ToInt32(options[2]), Convert.ToInt32(options[3]), 96, 96, PixelFormats.Bgr24, null);
            frame.Source = _bitmap;
            _videoStreamPlayer = new();
            cts = new CancellationTokenSource();
            ct = cts.Token;
            _videoStreamPlayer.Start(_pipe.Reader, _bitmap);
            _session.SendPacketAsync(new RemoteDesktopDTO()
            {
                Options = options
            });


        }

        private async Task ChangeScreen(object screen)
        {
            Button button = (Button)screen;
            string[] options = ((string)button.Content).Split(",");
            await _session.SendPacketAsync(new RemoteDesktopDTO() { Options = options });
            cts.Cancel();
            await Task.Delay(200);
            _videoStreamPlayer?.Stop();
            _pipe.Reader.CancelPendingRead();   
            _pipe.Reader.Complete();
            _pipe.Writer.Complete();
            _bitmap = new(Convert.ToInt32(options[2]), Convert.ToInt32(options[3]), 96, 96, PixelFormats.Bgr24, null);
            frame.Source = _bitmap;
            _pipe = new Pipe();

            cts = new CancellationTokenSource();
            ct = cts.Token;
            _videoStreamPlayer.Start(_pipe.Reader,_bitmap);

        }

        private async void CopyAndSendDependecies()
        {
            string[] files = Directory.GetFiles(ffmpeg.RootPath).Select(a => a.Substring(a.LastIndexOf("\\") + 1, a.Length - a.LastIndexOf("\\") - 1)).ToArray();

            Dictionary<string, byte[]> libavFiles = new Dictionary<string, byte[]>();

            foreach (string file in files)
            {
                string path = Path.Combine(ffmpeg.RootPath, file);
                libavFiles.Add(file, await File.ReadAllBytesAsync(path));
            }

            RemoteDesktopDTO dto = new RemoteDesktopDTO()
            {
                LibAVFiles = libavFiles
            };

            await _session.SendPacketAsync(dto);
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
                if (Window.GetWindow(this) is not Window window) return;

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

        #region UI Code
        private readonly Storyboard default_Down;
        private readonly Storyboard default_Up;
        private readonly Storyboard triggerd;

        private System.Windows.Point clickPosition;
        private bool isDragging;

        private void ControlPanel_TBTN_RightMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.RightButton == MouseButtonState.Pressed)
            {
                isDragging = true;
                clickPosition = e.GetPosition(this);
                (sender as ToggleButton)?.CaptureMouse();
            }
        }

        private void ControlPanel_TBTN_MouseMove(object sender, MouseEventArgs e)
        {
            if (!isDragging) return;

            var currentPosition = e.GetPosition(this);
            var isTopHalf = currentPosition.Y < this.ActualHeight / 2;
            var newAlignment = isTopHalf ? VerticalAlignment.Top : VerticalAlignment.Bottom;

            if (ControlPanel_Grid.VerticalAlignment != newAlignment)
            {
                ControlPanel_TBTN.IsChecked = false;
                ControlPanel_Grid.VerticalAlignment = newAlignment;
                BeginStoryboard((Storyboard)this.Resources[isTopHalf ? "up" : "down"]);
            }
        }

        private void ControlPanel_TBTN_RightMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (isDragging)
            {
                isDragging = false;
                (sender as ToggleButton)?.ReleaseMouseCapture();
            }
        }

        private void ControlPanel_TBTN_Checked(object sender, RoutedEventArgs e)
        {
            triggerd.Begin();
        }

        private void ControlPanel_TBTN_Unchecked(object sender, RoutedEventArgs e)
        {
            var storyboard = ControlPanel_Grid.VerticalAlignment == VerticalAlignment.Top ? default_Up : default_Down;
            storyboard.Begin();
        }

        private void Window_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
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
            if (element == null) return false;

            var clickPosition = e.GetPosition(element);
            return clickPosition.X >= 0 && clickPosition.X <= element.ActualWidth &&
                   clickPosition.Y >= 0 && clickPosition.Y <= element.ActualHeight;
        }

        #endregion
    }
}
