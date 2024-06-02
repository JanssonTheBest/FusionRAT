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

        private async Task DisplayFrame(byte[] frameBytes)
        {
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                frame.Source = ByteArrayToImageSource(frameBytes);
                _frameCounter++;
            });
        }

        private void OnTimerCallBack(object? sender, ElapsedEventArgs e)
        {
            Application.Current.Dispatcher.InvokeAsync(() =>
            {
                fpsLabel.Content = _frameCounter;
                _frameCounter = 0;
            });
        }

        private byte[] BitmapToJpegBytes(byte[] byteArray)
        {
            using (var stream = new MemoryStream(byteArray))
            {
                Bitmap bmp = new Bitmap(stream);

                // Create a new memory stream to save the JPEG image
                using (MemoryStream jpegStream = new MemoryStream())
                {
                    // Save the bitmap as JPEG to the memory stream
                    bmp.Save(jpegStream, System.Drawing.Imaging.ImageFormat.Jpeg);

                    // Return the byte array of the JPEG image
                    return jpegStream.ToArray();
                }
            }
        }

        private ImageSource ByteArrayToImageSource(byte[] byteArray)
        {
            using (var stream = new MemoryStream(byteArray))
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.StreamSource = stream;
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                bitmap.Freeze();
                return bitmap;
            }
        }
        public BitmapSource ByteArrayToBitmapSource(byte[] bitmapBytes, int width, int height)
        {
            var bitmapSource = BitmapSource.Create(
                width, height, 96, 96, // dpiX and dpiY
                System.Windows.Media.PixelFormats.Bgra32, // Pixel format
                null, // Palette
                bitmapBytes, // Bitmap data
                width * 4 // Stride (width * 4 bytes per pixel)
            );

            return bitmapSource;
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
