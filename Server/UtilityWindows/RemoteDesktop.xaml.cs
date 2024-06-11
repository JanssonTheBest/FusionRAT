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
            var tasks = new ConcurrentBag<Task>();

            for (int i = 0; i < _verticalAmount; i++)
            {
                for (int j = 0; j < _horizontalAmount; j++)
                {
                    int index = j + i * _horizontalAmount;
                    var point = new System.Drawing.Point(j * _bmpPartSize, i * _bmpPartSize);

                    if (bmpBytes[index] != null)
                    {
                        tasks.Add(Task.Run(async () =>
                        {
                            using (var ms = new MemoryStream(bmpBytes[index]))
                            {
                                var partBmp = new Bitmap(ms);
                                await _semaphoreSlim.WaitAsync();
                                try
                                {
                                    _graphics.DrawImage(partBmp, point);
                                }
                                finally
                                {
                                    _semaphoreSlim.Release();
                                }
                            }
                        }));
                    }
                }
            }

            await Task.WhenAll(tasks);
            return (Bitmap)_bmp.Clone();
        }

        private async Task DisplayFrame(byte[][] frameBytes)
        {
            var bmp = await ConcatenateBitmap(frameBytes);
            await Application.Current.Dispatcher.InvokeAsync(async () =>
            {
                frame.Source = await ToImageSource(bmp);
                _frameCounter++;
            }, DispatcherPriority.Send);
        }

        private async Task<BitmapImage> ToImageSource(Bitmap bmp)
        {
            using (var memory = new MemoryStream())
            {
                await _semaphoreSlim.WaitAsync();
                try
                {
                    bmp.Save(memory, System.Drawing.Imaging.ImageFormat.Bmp);
                }
                finally
                {
                    _semaphoreSlim.Release();
                }

                memory.Position = 0;
                var bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = memory;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();
                return bitmapImage;
            }
        }

        private void OnTimerCallBack(object? sender, ElapsedEventArgs e)
        {
            Application.Current.Dispatcher.InvokeAsync(() =>
            {
                fpsLabel.Content = _frameCounter;
                _frameCounter = 0;
            });
        }

        private void icon_Loaded(object sender, RoutedEventArgs e) { }

        private async void screens_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            await _serverSession.SendPacketAsync(new RemoteDesktopDTO
            {
                Screen = new[] { "dd" }
            });
        }
    }
}
