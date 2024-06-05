using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Common.Comunication;
using Common.DTOs.MessagePack;

namespace RemoteDesktopPlugin
{
    public class Plugin
    {
        Session _session;
        int bitrate = 12;
        int[] screen;
        public Plugin(Session session)
        {
            _session = session;
            session.OnRemoteDesktop += HandlePacket;
            var screenStateLogger = new ScreenStateLogger();
            screenStateLogger.ScreenRefreshed += OnNewFrame;
            //screenStateLogger.Start();
            screenRecTask = Task.Run(() =>
            {
                RecLoop();
            });
            screen = new int[] { 1920, 1080 };
        }

        private void HandlePacket(object? sender, EventArgs e)
        {

        }

        Task screenRecTask = Task.CompletedTask;

        //private async void RecLoop()
        //{
        //    MemoryStream ms = new MemoryStream();
        //    SHA256 sha256Hash = SHA256.Create();
        //    int screenArea = screen[0] * screen[1];
        //    int bmpPartSize = (int)Math.Round(MathF.Sqrt(screenArea / bitrate));
        //    Bitmap bmp = new Bitmap(bmpPartSize, bmpPartSize);
        //    int horizontalAmount = screen[0] / bmpPartSize + 1;
        //    int verticalAmount = screen[1] / bmpPartSize + 1;
        //    byte[][] oldbmp = new byte[horizontalAmount * verticalAmount][];
        //    Graphics g = Graphics.FromImage(bmp);
        //    EncoderParameters encoderParameters = new EncoderParameters(1);
        //    encoderParameters.Param[0] = new EncoderParameter(Encoder.Quality, 10L);
        //    ImageCodecInfo ic = GetEncoder(ImageFormat.Jpeg);
        //    while (true)
        //    {
        //        var dt = new RemoteDesktopDTO();
        //        dt.Frame = new byte[horizontalAmount * verticalAmount][];

        //        for (int i = 0; i < verticalAmount; i++)
        //        {
        //            for (int j = 0; j < horizontalAmount; j++)
        //            {
        //                g.CopyFromScreen(new Point(j * bmpPartSize, i * bmpPartSize), new Point(0, 0), new Size(bmpPartSize, bmpPartSize));
        //                bmp.Save(ms, ic, encoderParameters);
        //                byte[] newBmp = ms.ToArray();
        //                ms.SetLength(0);
        //                int index = j + i * horizontalAmount;

        //                if (oldbmp[index] == null)
        //                {
        //                    oldbmp[index] = newBmp;
        //                    dt.Frame[index] = newBmp;
        //                }
        //                else
        //                {
        //                    byte[] hashBytesOld = sha256Hash.ComputeHash(oldbmp[index]);
        //                    byte[] hashBytesNew = sha256Hash.ComputeHash(newBmp);

        //                    if (!hashBytesOld.SequenceEqual(hashBytesNew))
        //                    {
        //                        oldbmp[index] = newBmp;
        //                        dt.Frame[index] = newBmp;
        //                    }
        //                }
        //            }
        //        }
        //        _session.SendPacketAsync(dt);
        //    }
        //}



        //private async void RecLoop()
        //{
        //    int screenArea = screen[0] * screen[1];
        //    int bmpPartSize = (int)Math.Round(MathF.Sqrt(screenArea / bitrate));
        //    int horizontalAmount = screen[0] / bmpPartSize + 1;
        //    int verticalAmount = screen[1] / bmpPartSize + 1;
        //    byte[][] oldbmp = new byte[horizontalAmount * verticalAmount][];
        //    SemaphoreSlim semaphore = new SemaphoreSlim(1, 1);

        //    EncoderParameters encoderParameters = new EncoderParameters(1);
        //    encoderParameters.Param[0] = new EncoderParameter(Encoder.Quality, 10L);
        //    ImageCodecInfo ic = GetEncoder(ImageFormat.Jpeg);
        //    List<Task> tasks = new List<Task>();
        //    using (SHA256 sha256Hash = SHA256.Create())
        //    {
        //        while (true)
        //        {
        //            var dt = new RemoteDesktopDTO();
        //            dt.Frame = new byte[horizontalAmount * verticalAmount][];

        //            for (int i = 0; i < verticalAmount; i++)
        //            {
        //                for (int j = 0; j < horizontalAmount; j++)
        //                {
        //                    int index = j + i * horizontalAmount;
        //                    System.Drawing.Point point = new Point(j * bmpPartSize, i * bmpPartSize);
        //                    tasks.Add(Task.Run(async () =>
        //                    {
        //                        using (Bitmap bmp = new Bitmap(bmpPartSize, bmpPartSize))
        //                        using (MemoryStream ms = new MemoryStream())
        //                        using (Graphics g = Graphics.FromImage(bmp))
        //                        {
        //                            g.CopyFromScreen(point, new Point(0, 0), new Size(bmpPartSize, bmpPartSize));
        //                            bmp.Save(ms, ic, encoderParameters);
        //                            byte[] newBmp = ms.ToArray();

        //                            await semaphore.WaitAsync();
        //                            try
        //                            {
        //                                if (oldbmp[index] == null || !sha256Hash.ComputeHash(oldbmp[index]).SequenceEqual(sha256Hash.ComputeHash(newBmp)))
        //                                {
        //                                    oldbmp[index] = newBmp;
        //                                    dt.Frame[index] = newBmp;
        //                                }
        //                            }
        //                            finally
        //                            {
        //                                semaphore.Release();
        //                            }
        //                        }
        //                    }));
        //                }
        //            }
        //            await Task.WhenAll(tasks);
        //            _session.SendPacketAsync(dt);
        //            tasks.Clear();
        //        }
        //    }
        //}


        private async void RecLoop()
        {
            int screenArea = screen[0] * screen[1];
            int bmpPartSize = (int)Math.Round(MathF.Sqrt(screenArea / bitrate));
            int horizontalAmount = screen[0] / bmpPartSize + 1;
            int verticalAmount = screen[1] / bmpPartSize + 1;
            Bitmap[] oldbmp = new Bitmap[horizontalAmount * verticalAmount];
            SemaphoreSlim semaphore = new SemaphoreSlim(1, 1);

            EncoderParameters encoderParameters = new EncoderParameters(1);
            encoderParameters.Param[0] = new EncoderParameter(Encoder.Quality, 50L);
            ImageCodecInfo ic = GetEncoder(ImageFormat.Jpeg);
            List<Task> tasks = new List<Task>();

            while (true)
            {
                var dt = new RemoteDesktopDTO();
                dt.Frame = new byte[horizontalAmount * verticalAmount][];

                for (int i = 0; i < verticalAmount; i++)
                {
                    for (int j = 0; j < horizontalAmount; j++)
                    {
                        int index = j + i * horizontalAmount;
                        System.Drawing.Point point = new Point(j * bmpPartSize, i * bmpPartSize);
                        tasks.Add(Task.Run(async () =>
                        {
                            using (Bitmap bmp = new Bitmap(bmpPartSize, bmpPartSize))
                            using (MemoryStream ms = new MemoryStream())
                            using (Graphics g = Graphics.FromImage(bmp))
                            {
                                g.CopyFromScreen(point, new Point(0, 0), new Size(bmpPartSize, bmpPartSize));
                                bmp.Save(ms, ic, encoderParameters);
                                byte[] newBmp = ms.ToArray();

                                await semaphore.WaitAsync();
                                try
                                {
                                    if (oldbmp[index] == null || !CompareMemCmp(oldbmp[index], bmp))
                                    {
                                        oldbmp[index]?.Dispose(); // Dispose old bitmap
                                        oldbmp[index] = new Bitmap(bmp); // Store a copy of the new bitmap
                                        dt.Frame[index] = newBmp;
                                    }
                                }
                                finally
                                {
                                    semaphore.Release();
                                }
                            }
                        }));
                    }
                }
                await Task.WhenAll(tasks);
                _session.SendPacketAsync(dt);
                tasks.Clear();
            }
        }

        [DllImport("msvcrt.dll")]
        private static extern int memcmp(IntPtr b1, IntPtr b2, long count);

        public static bool CompareMemCmp(Bitmap b1, Bitmap b2)
        {
            if ((b1 == null) != (b2 == null)) return false;
            if (b1.Size != b2.Size) return false;

            var bd1 = b1.LockBits(new Rectangle(new Point(0, 0), b1.Size), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            var bd2 = b2.LockBits(new Rectangle(new Point(0, 0), b2.Size), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

            try
            {
                IntPtr bd1scan0 = bd1.Scan0;
                IntPtr bd2scan0 = bd2.Scan0;

                int stride = bd1.Stride;
                int len = stride * b1.Height;

                return memcmp(bd1scan0, bd2scan0, len) == 0;
            }
            finally
            {
                b1.UnlockBits(bd1);
                b2.UnlockBits(bd2);
            }
        }



        private static ImageCodecInfo GetEncoder(ImageFormat format)
        {
            // Get all available image encoders
            ImageCodecInfo[] codecs = ImageCodecInfo.GetImageEncoders();

            // Find and return the encoder with the specified format
            foreach (ImageCodecInfo codec in codecs)
            {
                if (codec.FormatID == format.Guid)
                {
                    return codec;
                }
            }

            return null;
        }

        private async void OnNewFrame(object? sender, byte[] e)
        {
            await _session.SendPacketAsync(new RemoteDesktopDTO()
            {
                //Frame = e
            });
        }
    }
}
