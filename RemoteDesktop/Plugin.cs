using Common.DTOs.MessagePack;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Common.Comunication;

namespace RemoteDesktopPlugin
{
    public class Plugin
    {
        private readonly Session _session;
        private readonly int _bitrate = 6;
        private readonly int[] _screen = { 1920, 1080 };
        private readonly Bitmap[] _oldBitmaps;
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
        private readonly ImageCodecInfo _jpegEncoder;
        private readonly EncoderParameters _encoderParameters;

        public Plugin(Session session)
        {
            _session = session;
            _session.OnRemoteDesktop += HandlePacket;
            _oldBitmaps = new Bitmap[CalculateBitmapArraySize()];
            _jpegEncoder = GetEncoder(ImageFormat.Jpeg);
            _encoderParameters = new EncoderParameters(1)
            {
                Param = new[] { new EncoderParameter(Encoder.Quality, 50L) }
            };
            Task.Run(RecLoop);
        }

        private void HandlePacket(object? sender, EventArgs e)
        {
            // Handle incoming packets
        }

        private int CalculateBitmapArraySize()
        {
            int screenArea = _screen[0] * _screen[1];
            int bmpPartSize = (int)Math.Round(MathF.Sqrt(screenArea / _bitrate));
            int horizontalAmount = _screen[0] / bmpPartSize + 1;
            int verticalAmount = _screen[1] / bmpPartSize + 1;
            return horizontalAmount * verticalAmount;
        }

        private async Task RecLoop()
        {
            int screenArea = _screen[0] * _screen[1];
            int bmpPartSize = (int)Math.Round(MathF.Sqrt(screenArea / _bitrate));
            int horizontalAmount = _screen[0] / bmpPartSize + 1;
            int verticalAmount = _screen[1] / bmpPartSize + 1;

            while (true)
            {
                var frameTasks = new ConcurrentBag<Task>();
                var dt = new RemoteDesktopDTO
                {
                    Frame = new byte[horizontalAmount * verticalAmount][]
                };

                for (int i = 0; i < verticalAmount; i++)
                {
                    for (int j = 0; j < horizontalAmount; j++)
                    {
                        int index = j + i * horizontalAmount;
                        var point = new Point(j * bmpPartSize, i * bmpPartSize);
                        frameTasks.Add(ProcessFramePart(index, point, bmpPartSize, dt));
                    }
                }

                await Task.WhenAll(frameTasks);
                await _session.SendPacketAsync(dt);
            }
        }

        private async Task ProcessFramePart(int index, Point point, int bmpPartSize, RemoteDesktopDTO dt)
        {
            using (var bmp = new Bitmap(bmpPartSize, bmpPartSize))
            using (var ms = new MemoryStream())
            using (var g = Graphics.FromImage(bmp))
            {
                g.CopyFromScreen(point, Point.Empty, new Size(bmpPartSize, bmpPartSize));
                bmp.Save(ms, _jpegEncoder, _encoderParameters);
                byte[] newBmp = ms.ToArray();

                await _semaphore.WaitAsync();
                try
                {
                    if (_oldBitmaps[index] == null || !CompareBitmaps(_oldBitmaps[index], bmp))
                    {
                        _oldBitmaps[index]?.Dispose();
                        _oldBitmaps[index] = new Bitmap(bmp);
                        dt.Frame[index] = newBmp;
                    }
                }
                finally
                {
                    _semaphore.Release();
                }
            }
        }

        [DllImport("msvcrt.dll")]
        private static extern int memcmp(IntPtr b1, IntPtr b2, long count);

        private static bool CompareBitmaps(Bitmap b1, Bitmap b2)
        {
            if (b1 == null || b2 == null || b1.Size != b2.Size) return false;

            var bd1 = b1.LockBits(new Rectangle(Point.Empty, b1.Size), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            var bd2 = b2.LockBits(new Rectangle(Point.Empty, b2.Size), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

            try
            {
                return memcmp(bd1.Scan0, bd2.Scan0, bd1.Stride * b1.Height) == 0;
            }
            finally
            {
                b1.UnlockBits(bd1);
                b2.UnlockBits(bd2);
            }
        }

        private static ImageCodecInfo GetEncoder(ImageFormat format)
        {
            return ImageCodecInfo.GetImageEncoders().FirstOrDefault(codec => codec.FormatID == format.Guid);
        }
    }
}
