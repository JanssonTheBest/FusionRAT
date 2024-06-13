using Common.Comunication;
using Common.DTOs.MessagePack;
using System.Collections.Concurrent;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

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
        private readonly int _bmpPartSize;
        private readonly int _horizontalAmount;
        private readonly int _verticalAmount;

        public Plugin(Session session)
        {
            _session = session;
            _session.OnRemoteDesktop += HandlePacket;
            _bmpPartSize = (int)Math.Round(MathF.Sqrt(_screen[0] * _screen[1] / (float)_bitrate));
            _horizontalAmount = _screen[0] / _bmpPartSize + 1;
            _verticalAmount = _screen[1] / _bmpPartSize + 1;
            _oldBitmaps = new Bitmap[_horizontalAmount * _verticalAmount];
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

        private async Task RecLoop()
        {
            while (true)
            {
                var frameTasks = new ConcurrentBag<Task>();
                var dt = new RemoteDesktopDTO
                {
                    Frame = new byte[_horizontalAmount * _verticalAmount][]
                };

                for (int i = 0; i < _verticalAmount; i++)
                {
                    for (int j = 0; j < _horizontalAmount; j++)
                    {
                        int index = j + i * _horizontalAmount;
                        var point = new Point(j * _bmpPartSize, i * _bmpPartSize);
                        frameTasks.Add(ProcessFramePart(index, point, _bmpPartSize, dt));
                    }
                }

                await Task.WhenAll(frameTasks);
                await _session.SendPacketAsync(dt);
            }
        }

        private async Task ProcessFramePart(int index, Point point, int bmpPartSize, RemoteDesktopDTO dt)
        {
            using (var bmp = new Bitmap(bmpPartSize, bmpPartSize))
            using (var g = Graphics.FromImage(bmp))
            {
                g.CopyFromScreen(point, Point.Empty, new Size(bmpPartSize, bmpPartSize));
                var newBmp = BitmapToByteArray(bmp);

                await _semaphore.WaitAsync();
                try
                {
                    if (_oldBitmaps[index] == null || !CompareBitmaps(_oldBitmaps[index], bmp))
                    {
                        _oldBitmaps[index]?.Dispose();
                        _oldBitmaps[index] = new Bitmap(bmp);
                        dt.Frame[index] = newBmp;
                    }
                    else
                    {
                        dt.Frame[index] = null;
                    }
                }
                finally
                {
                    _semaphore.Release();
                }
            }
        }

        private byte[] BitmapToByteArray(Bitmap bmp)
        {
            using (var ms = new MemoryStream())
            {
                bmp.Save(ms, _jpegEncoder, _encoderParameters);
                return ms.ToArray();
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
