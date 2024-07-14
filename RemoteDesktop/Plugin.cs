using System.Runtime.InteropServices;
using System.Collections.Concurrent;
using Common.DTOs.MessagePack;
using System.Drawing.Imaging;
using Common.Communication;
using System.Drawing;

namespace RemoteDesktopPlugin
{
    public class Plugin
    {
        private readonly ConcurrentDictionary<int, Bitmap> _oldBitmaps = new();
        private readonly EncoderParameters _encoderParameters;
        private readonly int[] _screen = [1920, 1080];
        private readonly ImageCodecInfo _jpegEncoder;
        private readonly Session _session;
        private readonly int _bitrate = 6;

        public Plugin(Session session)
        {
            _session = session;
            _session.OnRemoteDesktop += HandlePacket;
            _jpegEncoder = GetEncoder(ImageFormat.Jpeg);
            _encoderParameters = new EncoderParameters(1)
            {
                Param = [new EncoderParameter(Encoder.Quality, 50L)]
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
                var dt = new RemoteDesktopDTO
                {
                    Frame = new byte[horizontalAmount * verticalAmount][]
                };
                    Parallel.For(0, verticalAmount, i =>
                    {
                        for (int j = 0; j < horizontalAmount; j++)
                        {
                            int index = j + i * horizontalAmount;
                            var point = new Point(j * bmpPartSize, i * bmpPartSize);
                            ProcessFramePart(index, point, bmpPartSize, dt).Wait();
                        }
                    });
               
                await _session.SendPacketAsync(dt);
            }
        }

        private async Task ProcessFramePart(int index, Point point, int bmpPartSize, RemoteDesktopDTO dt)
        {
            using var bmp = new Bitmap(bmpPartSize, bmpPartSize);
            using var ms = new MemoryStream();
            using var g = Graphics.FromImage(bmp);
            g.CopyFromScreen(point, Point.Empty, new Size(bmpPartSize, bmpPartSize));
            bmp.Save(ms, _jpegEncoder, _encoderParameters);
            byte[] newBmp = ms.ToArray();

            Bitmap oldBitmap;
            if (_oldBitmaps.TryGetValue(index, out oldBitmap))
            {
                if (!CompareBitmaps(oldBitmap, bmp))
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
            else
            {
                _oldBitmaps[index] = new Bitmap(bmp);
                dt.Frame[index] = newBmp;
            }
        }

        [DllImport("msvcrt.dll")]
        private static extern int memcmp(IntPtr b1, IntPtr b2, long count);

        private static unsafe bool CompareBitmaps(Bitmap b1, Bitmap b2)
        {
            if (b1 == null || b2 == null || b1.Size != b2.Size) return false;

            var bd1 = b1.LockBits(new Rectangle(Point.Empty, b1.Size), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            var bd2 = b2.LockBits(new Rectangle(Point.Empty, b2.Size), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

            try
            {
                int* p1 = (int*)bd1.Scan0;
                int* p2 = (int*)bd2.Scan0;
                int len = bd1.Stride * b1.Height / sizeof(int);

                for (int i = 0; i < len; i++)
                {
                    if (p1[i] != p2[i]) return false;
                }
                return true;
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
