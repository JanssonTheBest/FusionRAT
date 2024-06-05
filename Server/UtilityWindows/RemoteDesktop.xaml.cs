using System.IO;
using System.Timers;
using System.Windows;
using System.Windows.Media;
using Common.DTOs.MessagePack;
using System.Windows.Media.Imaging;
using Server.CoreServerFunctionality;
using Server.UtilityWindows.Interface;
using System.Windows.Controls;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Threading;

namespace Server.UtilityWindows
{
    public partial class RemoteDesktop : Window, IUtilityWindow
    {
        private readonly ServerSession _serverSession;
        private readonly System.Timers.Timer _fpsTimer;
        private int _frameCounter;

        public RemoteDesktop(ServerSession serverSession)
        {
            InitializeComponent();
            _serverSession = serverSession;
            _serverSession.OnRemoteDesktop += HandlePacket;
            _serverSession.SendPlugin(typeof(RemoteDesktopPlugin.Plugin));

            screenArea = screen[0] * screen[1];
            bmpPartSize = (int)Math.Round(MathF.Sqrt(screenArea / bitrate));
            horizontalAmount = screen[0] / bmpPartSize + 1;
            verticalAmount = screen[1] / bmpPartSize + 1;

            bmp = new(screen[0], screen[1]);
            g =Graphics.FromImage(bmp);

            _fpsTimer = new System.Timers.Timer(1000);
            _fpsTimer.Elapsed += OnTimerCallBack;
            _fpsTimer.AutoReset = true;
            _fpsTimer.Start();
        }

        private async void HandlePacket(object? sender, EventArgs e)
        {
            var dto = (RemoteDesktopDTO)sender;
            if (dto.Screen is not null)
            {
                await Application.Current.Dispatcher.InvokeAsync(() => screens.Items.Add(string.Join("|", dto.Screen)));
                return;
            }
            await DisplayFrame(dto.Frame);
        }

        Bitmap bmp;
        int bitrate = 6;
        int[] screen = new int[] { 1920, 1080 };
        int screenArea;
        int bmpPartSize;
        int horizontalAmount;
        int verticalAmount;
        Graphics g;
        SemaphoreSlim semaphoreSlim = new SemaphoreSlim(1, 1);
        private async Task<Bitmap> ConcatinateBitmap(byte[][] bmpBytes)
        {
            List<Task> tasks = new List<Task>();
            for (int i = 0; i < verticalAmount; i++)
            {
                for (int j = 0; j < horizontalAmount; j++)
                {
                    int index = j + i * horizontalAmount;
                    System.Drawing.Point point = new System.Drawing.Point(j * bmpPartSize, i * bmpPartSize);
                    tasks.Add(Task.Run(async () =>
                    {
                        if (bmpBytes[index] != null)
                        {
                            using (MemoryStream ms = new MemoryStream(bmpBytes[index]))
                            {
                                ms.Position = 0; // Reset the stream position to the beginning
                                Bitmap partBmp = new Bitmap(ms);
                                await semaphoreSlim.WaitAsync();
                                try
                                {
                                    g.DrawImage(partBmp, point);
                                }
                                finally
                                {
                                    semaphoreSlim.Release();
                                }
                            }
                        }
                    }));
                }
            }
            await Task.WhenAll(tasks);
            tasks.Clear();
          return (Bitmap)bmp.Clone();
        }

        private async Task DisplayFrame(byte[][] frameBytes)
        {
            

            var b = await ConcatinateBitmap(frameBytes);
            await Application.Current.Dispatcher.InvokeAsync(async () =>
            {
                frame.Source = await ToImageSource(b);
                _frameCounter++;
            }, DispatcherPriority.Send);
        }

        private async Task<BitmapImage> ToImageSource(Bitmap b)
        {
            using (MemoryStream memory = new MemoryStream())
            {
                await semaphoreSlim.WaitAsync();
                try
                {
                    b.Save(memory, System.Drawing.Imaging.ImageFormat.Bmp);
                }
                finally
                {
                    semaphoreSlim.Release();
                }
                memory.Position = 0;

                BitmapImage bitmapImage = new BitmapImage();
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

        private void icon_Loaded(object sender, RoutedEventArgs e)
        {
        }

        private async void screens_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            await _serverSession.SendPacketAsync(new RemoteDesktopDTO
            {
                Screen = new string[] { "dd" }
            });
        }
    }
}
