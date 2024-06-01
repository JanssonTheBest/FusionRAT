using System.IO;
using System.Timers;
using System.Windows;
using System.Windows.Media;
using Common.DTOs.MessagePack;
using System.Windows.Media.Imaging;
using Server.CoreServerFunctionality;
using Server.UtilityWindows.Interface;

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
            _serverSession.SendPacketAsync(new RemoteDesktopDTO { Frame = new byte[1] });

            _fpsTimer = new System.Timers.Timer(1000);
            _fpsTimer.Elapsed += OnTimerCallBack;
            _fpsTimer.AutoReset = true;
            _fpsTimer.Start();
        }

        private void HandlePacket(object? sender, EventArgs e)
        {
            DisplayFrame(((RemoteDesktopDTO)sender).Frame);
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

        private void icon_Loaded(object sender, RoutedEventArgs e)
        {
        }
    }
}
