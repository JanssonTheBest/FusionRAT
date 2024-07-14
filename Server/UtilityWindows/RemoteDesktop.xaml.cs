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

namespace Server.UtilityWindows
{

    public partial class RemoteDesktop : Window, IUtilityWindow
    {
        private readonly SemaphoreSlim _semaphoreSlim = new(1, 1);

        private readonly System.Timers.Timer _fpsTimer;

        private readonly ServerSession _serverSession;

        private readonly int[] _screen = [1920, 1080];
        private readonly int _horizontalAmount;
        private readonly int _verticalAmount;
        private readonly int _screenArea;

        private readonly Graphics _graphics;
        private readonly int _bmpPartSize;
        private readonly Bitmap _bmp;

        private readonly int _bitrate = 6;
        private int _frameCounter;

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

            triggerd = (FindResource("ControlPanel_Triggered") as Storyboard);
            default_Down = (FindResource("ControlPanel_Default_Down") as Storyboard);
            default_Up = (FindResource("ControlPanel_Default_Up") as Storyboard);
        }

        private async void HandlePacket(object? sender, EventArgs e)
        {
            var dto = sender as RemoteDesktopDTO;
            if (dto?.Screen != null)
            {
                //await Application.Current.Dispatcher.InvokeAsync(() => screens.Items.Add(string.Join("|", dto.Screen)));
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

                        using var ms = new MemoryStream(bmpBytes[index]);
                        var partBmp = new Bitmap(ms);

                        lock (_graphics)
                        {
                            _graphics.DrawImage(partBmp, point);

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

        private readonly MemoryStream memory = new();
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

        //private async void screens_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        //{
        //    await _serverSession.SendPacketAsync(new RemoteDesktopDTO
        //    {
        //        Screen = new[] { "dd" }
        //    });
        //}

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
