using Common.DTOs.MessagePack;
using Server.CoreServerFunctionality;
using Server.UtilityWindows.Interface;
using System.Drawing;
using System.IO;
using System.Timers;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Collections.Concurrent;
using Server.Helper;
using System.Windows.Input;

namespace Server.UtilityWindows
{
    public partial class RemoteDesktop : Window, IUtilityWindow
    {
        private readonly ServerSession _serverSession;
        private readonly System.Timers.Timer _fpsTimer;
        private int _frameCounter;
        private readonly SemaphoreSlim _semaphoreSlim = new SemaphoreSlim(1, 1);
        private readonly Bitmap _bmp;
        private readonly Graphics _graphics;
        private readonly int _bitrate = 6;
        private readonly int[] _screen = { 1920, 1080 };
        private readonly int _screenArea;
        private readonly int _bmpPartSize;
        private readonly int _horizontalAmount;
        private readonly int _verticalAmount;

        public RemoteDesktop(ServerSession serverSession)
        {
            InitializeComponent();
            _serverSession = serverSession;
            _serverSession.OnRemoteDesktop += HandlePacket;
            _serverSession.SendPlugin(typeof(RemoteDesktopPlugin.Plugin));

            _screenArea = _screen[0] * _screen[1];
            _bmpPartSize = (int)Math.Round(MathF.Sqrt(_screenArea / _bitrate));
            _horizontalAmount = _screen[0] / _bmpPartSize + 1;
            _verticalAmount = _screen[1] / _bmpPartSize + 1;

            _bmp = new Bitmap(_screen[0], _screen[1]);
            _graphics = Graphics.FromImage(_bmp);

            _fpsTimer = new System.Timers.Timer(1000);
            _fpsTimer.Elapsed += OnTimerCallBack;
            _fpsTimer.AutoReset = true;
            _fpsTimer.Start();
        }

        private async void HandlePacket(object? sender, EventArgs e)
        {
            var dto = sender as RemoteDesktopDTO;
            if (dto?.Screen != null)
            {
                await Application.Current.Dispatcher.InvokeAsync(() => screens.Items.Add(string.Join("|", dto.Screen)));
                return;
            }
            await DisplayFrame(dto?.Frame);
        }

        private async Task<Bitmap> ConcatenateBitmap(byte[][] bmpBytes)
        {
            Parallel.For(0, _verticalAmount, i =>
            {
                Parallel.For(0, _horizontalAmount, j =>
                {
                    int index = j + i * _horizontalAmount;
                    var point = new System.Drawing.Point(j * _bmpPartSize, i * _bmpPartSize);

                    if (bmpBytes[index] != null)
                    {

                        using (var ms = new MemoryStream(bmpBytes[index]))
                        {
                            var partBmp = new Bitmap(ms);

                            lock (_graphics)
                            {
                                _graphics.DrawImage(partBmp, point);

                            }
                        }
                    }
                });
            });

            return _bmp;
        }


        private async Task DisplayFrame(byte[][] frameBytes)
        {
            var bmp = await ConcatenateBitmap(frameBytes);
            await Application.Current.Dispatcher.InvokeAsync(async () =>
            {
                frame.Source = await ToImageSource(bmp);
                _frameCounter++;
            }, DispatcherPriority.Render);
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

        private void OnTimerCallBack(object? sender, ElapsedEventArgs e)
        {
            Application.Current.Dispatcher.InvokeAsync(() =>
            {
                fpsTextBlockContent.Text = _frameCounter.ToString();
                fpsTextBlockContent.Text = $"Fusion - Remote Desktop | {_frameCounter} FPS : 30 MS";
                _frameCounter = 0;
            });
        }

        private async void screens_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            await _serverSession.SendPacketAsync(new RemoteDesktopDTO
            {
                Screen = new[] { "dd" }
            });
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
