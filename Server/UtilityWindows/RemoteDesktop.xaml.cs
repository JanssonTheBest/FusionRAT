using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipelines;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Server.CoreServerFunctionality;
using Server.UtilityWindows.Interface;
using Server.VideoProcessing;
using Common.DTOs.MessagePack;
using FFmpeg.AutoGen;
using System.Collections.Concurrent;

namespace Server.UtilityWindows
{
    public partial class RemoteDesktop : Window, IUtilityWindow
    {
        private ServerSession _session;
        private VideoStreamPlayer _videoStreamPlayer;
        private WriteableBitmap _bitmap;
        private Pipe _pipe;
        private CancellationTokenSource cts = new CancellationTokenSource();
        private CancellationToken ct;

        public RemoteDesktop(ServerSession session)
        {
            InitializeComponent();

            ct = cts.Token;
            _session = session;

            session.OnRemoteDesktop += HandlePacket;
            _pipe = new Pipe();

            Closed += WindowIsClosing;

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

        private SemaphoreSlim semaphore = new SemaphoreSlim(1, 1);
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
                CopyAndSendDependencies();
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
            }
            finally
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
                    ComboBoxItem comboBoxItem = new ComboBoxItem
                    {
                        Content = string.Join(",", screenInfo),
                        HorizontalAlignment = HorizontalAlignment.Stretch,
                        VerticalAlignment = VerticalAlignment.Stretch
                    };
                    screenControlComboBox.Items.Add(comboBoxItem);
                });
            }
        }



        private async void ToggleStream_Checked(object sender, RoutedEventArgs e)
        {

        }

        private async void ToggleStream_Unchecked(object sender, RoutedEventArgs e)
        {
        }

        private async Task ChangeScreen(object screen)
        {
            ComboBoxItem button = (ComboBoxItem)screen;
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
            _videoStreamPlayer.Start(_pipe.Reader, _bitmap);
        }



        private async Task CopyAndSendDependencies()
        {
            string[] files = Directory.GetFiles(ffmpeg.RootPath)
                .Select(a => Path.GetFileName(a))
                .ToArray();

            var libavFiles = new ConcurrentDictionary<string, byte[]>();

            await Task.WhenAll(files.Select(async file =>
            {
                string path = Path.Combine(ffmpeg.RootPath, file);
                byte[] content = await File.ReadAllBytesAsync(path);
                libavFiles.TryAdd(file, content);
            }));

            RemoteDesktopDTO dto = new RemoteDesktopDTO()
            {
                LibAVFiles = new Dictionary<string, byte[]>(libavFiles)
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

        private void TitleBar_MouseLeftButtonUp(object sender, MouseEventArgs e)
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

        private async void screenControlComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            object item = screenControlComboBox.SelectedItem;
            if (_videoStreamPlayer != null)
            {
                if (_videoStreamPlayer.IsPlaying)
                {
                    await ChangeScreen(item);
                    return;
                }
            }

            ComboBoxItem button = (ComboBoxItem)item;
            string[] options = ((string)button.Content).Split(",");
            _bitmap = new(Convert.ToInt32(options[2]), Convert.ToInt32(options[3]), 96, 96, PixelFormats.Bgr24, null);
            frame.Source = _bitmap;
            _videoStreamPlayer = new();
            _videoStreamPlayer.OnFPSCallback += UpdateFPSAndMS;
            cts = new CancellationTokenSource();
            ct = cts.Token;
            _videoStreamPlayer.Start(_pipe.Reader, _bitmap);
            _session.SendPacketAsync(new RemoteDesktopDTO()
            {
                Options = options
            });

        }

        private void UpdateFPSAndMS(object? sender, VideoStreamInfo e)
        {
            Application.Current.Dispatcher.InvokeAsync(() =>
            {
                fpsTblock.Text = $"{e.FPS} FPS";
                msTblock.Text = $"{e.DelayMS} MS";
            });

        }


        //Black screen
        private void ToggleButton_Checked(object sender, RoutedEventArgs e)
        {

        }

        private void ToggleButton_Unchecked(object sender, RoutedEventArgs e)
        {

        }


        //Mouse
        bool mouseInput = false;
        private void ToggleButton_Checked_1(object sender, RoutedEventArgs e)
        {
            mouseInput = true;
        }

        private void ToggleButton_Unchecked_1(object sender, RoutedEventArgs e)
        {
            mouseInput = false; 
        }

        //Keyboard
        bool keyboardInput = false;
        private void ToggleButton_Checked_2(object sender, RoutedEventArgs e)
        {
            keyboardInput = true;
        }

        private void ToggleButton_Unchecked_2(object sender, RoutedEventArgs e)
        {
            keyboardInput = false;
        }

        DateTime oldtimeDateTime;

        private async void Grid_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (!mouseInput)
            {
                return;
            }
            DateTime currentTime = DateTime.Now;
            if (oldtimeDateTime == null)
            {
                oldtimeDateTime = DateTime.Now;
            }
            else
            {
                TimeSpan span = (currentTime - oldtimeDateTime);
                if (span.TotalMilliseconds < 50)
                {
                    return;
                }
            }

            oldtimeDateTime = currentTime;

            var pos = e.GetPosition(actualFrame);
            double xFac = pos.X / actualFrame.ActualWidth;
            double yFac = pos.Y / actualFrame.ActualHeight;

            await _session.SendPacketAsync(new RemoteDesktopDTO()
            {
                xFactor = xFac,
                yFactor = yFac,
            });
        }

        private async void Grid_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (!mouseInput)
            {
                return;
            }
            var pos = e.GetPosition(actualFrame);
            double xFac = pos.X / actualFrame.ActualWidth;
            double yFac = pos.Y / actualFrame.ActualHeight;

            int mousebutton = 1;

            if (e.ChangedButton == System.Windows.Input.MouseButton.Left)
            {
                mousebutton = (int)MouseButton.Left;
            }
            else if (e.ChangedButton == System.Windows.Input.MouseButton.Right)
            {
                mousebutton = (int)MouseButton.Right;
            }

            await _session.SendPacketAsync(new RemoteDesktopDTO()
            {
                IsPressed = true,
                MouseButton = mousebutton,
                xFactor = xFac,
                yFactor = yFac,
            });
        }

        private async void Grid_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (!mouseInput)
            {
                return;
            }
            var pos = e.GetPosition(actualFrame);
            double xFac = pos.X / actualFrame.ActualWidth;
            double yFac = pos.Y / actualFrame.ActualHeight;

            int mousebutton = 1;

            if (e.ChangedButton == System.Windows.Input.MouseButton.Left)
            {
                mousebutton = (int)MouseButton.Left;
            }
            else if (e.ChangedButton == System.Windows.Input.MouseButton.Right)
            {
                mousebutton = (int)MouseButton.Right;
            }

            await _session.SendPacketAsync(new RemoteDesktopDTO()
            {
                IsPressed = false,
                MouseButton = mousebutton,
                xFactor = xFac,
                yFactor = yFac,
            });

        }
    }
}
