
using Common.DTOs.MessagePack;
using Server.CoreServerFunctionality;
using Server.UtilityWindows.Interface;
using System.IO;
using System.Timers;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Server.UtilityWindows
{
    public partial class RemoteDesktop :Window, IUtilityWindow
    {
        public RemoteDesktop(ServerSession serverSession)
        {
            InitializeComponent();
            serverSession.OnRemoteDesktop += HandlePacket;
            serverSession.SendPacketAsync(new RemoteDesktopDTO()
            {
                Frame=new byte[1]
            });
        }

        private void HandlePacket(object? sender, EventArgs e)
        {
            DisplayFrame(((RemoteDesktopDTO)sender).Frame);
        }

        System.Timers.Timer fpsTimer =new System.Timers.Timer();
        int frameCounter = 0;
        private async Task DisplayFrame(byte[] frameBytes)
        {
            if (!fpsTimer.Enabled)
            {
                Task.Run(() =>
                {
                    fpsTimer.Interval = 1000;
                    fpsTimer.Elapsed += OnTimerCallBack;
                    fpsTimer.AutoReset = true;
                    fpsTimer.Start();
                });
            }

            await Application.Current.Dispatcher.InvokeAsync(async () =>
            {
                frame.Source = await ByteArrayToImageSource(frameBytes);
            });
            frameCounter++;
        }

        private void OnTimerCallBack(object? sender, ElapsedEventArgs e)
        {
            Application.Current.Dispatcher.InvokeAsync(() =>
            {
                fpsLabel.Content = frameCounter;
                frameCounter = 0;
            });
        }

        private async Task<ImageSource> ByteArrayToImageSource(byte[] byteArray)
        {
            BitmapImage bitmap = new BitmapImage();
            using (MemoryStream stream = new MemoryStream(byteArray))
            {
                stream.Seek(0, SeekOrigin.Begin);
                bitmap.BeginInit();
                bitmap.StreamSource = stream;
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                bitmap.Freeze();
            }
            return bitmap;
        }

        private void icon_Loaded(object sender, RoutedEventArgs e)
        {

        }
    }
}
