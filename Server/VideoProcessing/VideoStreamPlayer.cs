using Common.DTOs.MessagePack;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Timers;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using FFmpeg.AutoGen;
using System.Buffers;
using System.Windows;
using System.Runtime.Intrinsics.X86;
using System.Runtime.Intrinsics;

namespace Server.VideoProcessing
{
    internal unsafe class VideoStreamPlayer
    {
        private WriteableBitmap writeableBitmap;
        private PipeReader pipeReader;
        private MemoryStream probeStream = new MemoryStream();
        private bool isProbing = false;
        private CancellationTokenSource cts = new CancellationTokenSource();
        private CancellationToken ct;
        private Thread decodingThread;
        private Thread bitmapThread;
        private BlockingCollection<AVFrameWrapper> decodedFramesBuffer = new BlockingCollection<AVFrameWrapper>();

        public VideoStreamPlayer(WriteableBitmap wb, PipeReader pr)
        {
            ct = cts.Token;
            writeableBitmap = wb;
            pipeReader = pr;
        }

        public void Start()
        {
            bool result = cts.TryReset();
            ct = cts.Token;
            decodingThread = new Thread(DecodingThreadMethod);
            decodingThread.Start();
        }

        private void DecodingThreadMethod()
        {
            int result = 0;
            ulong bufferSize = 4096;
            int probeSize = 1024 * 150;
            AVFormatContext* formatContext = ffmpeg.avformat_alloc_context();
            formatContext->probesize = probeSize;
            IntPtr buffer = (IntPtr)ffmpeg.av_malloc(bufferSize);
            GCHandle gcHandle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
            AVIOContext* ioContext = ffmpeg.avio_alloc_context((byte*)buffer, (int)bufferSize, 0, null, new avio_alloc_context_read_packet(ReadPacket), null, new avio_alloc_context_seek(Seek));
            formatContext->pb = ioContext;
            formatContext->flags |= ffmpeg.AVFMT_FLAG_CUSTOM_IO | ffmpeg.AVFMT_FLAG_IGNIDX;
            formatContext->duration = ffmpeg.AV_NOPTS_VALUE;

            var readResult = pipeReader.ReadAtLeastAsync(probeSize, ct).AsTask().GetAwaiter().GetResult();
            probeStream.Write(readResult.Buffer.ToArray());
            probeStream.Seek(0, SeekOrigin.Begin);
            pipeReader.AdvanceTo(readResult.Buffer.Start);
            isProbing = true;
            result = ffmpeg.avformat_open_input(&formatContext, null, null, null);
            isProbing = false;
            readResult = pipeReader.ReadAsync(ct).AsTask().GetAwaiter().GetResult();
            pipeReader.AdvanceTo(readResult.Buffer.GetPosition(probeStream.Position));

            AVStream* stream = null;
            for (int i = 0; i < formatContext->nb_streams; i++)
            {
                if (formatContext->streams[i]->codecpar->codec_type == AVMediaType.AVMEDIA_TYPE_VIDEO)
                {
                    stream = formatContext->streams[i];
                    break;
                }
            }

            bitmapThread = new Thread(() =>
            {
                BitmapThreadMethod(ffmpeg.av_q2d(stream->time_base));
            });

            bitmapThread.Start();

            AVCodec* codec = ffmpeg.avcodec_find_decoder(stream->codecpar->codec_id);
            AVCodecContext* codecContext = ffmpeg.avcodec_alloc_context3(codec);
            ffmpeg.av_opt_set(codecContext->priv_data, "preset", "ultrafast", 0);
            ffmpeg.av_opt_set(codecContext->priv_data, "tune", "fastdecode", 0);
            ffmpeg.avcodec_parameters_to_context(codecContext, stream->codecpar);
            codecContext->skip_loop_filter = AVDiscard.AVDISCARD_ALL;
            codecContext->profile = ffmpeg.FF_PROFILE_H264_BASELINE;
            codecContext->thread_count = 4;
            codecContext->thread_type = ffmpeg.FF_THREAD_FRAME;



            ffmpeg.avcodec_open2(codecContext, null, null);

            AVPacket* packet = ffmpeg.av_packet_alloc();
            try
            {
                while (!ct.IsCancellationRequested)
                {
                    ffmpeg.av_packet_unref(packet);
                    result = ffmpeg.av_read_frame(formatContext, packet);

                    if (packet->stream_index != stream->index)
                    {
                        continue;
                    }

                    result = ffmpeg.avcodec_send_packet(codecContext, packet);

                    while (!ct.IsCancellationRequested)
                    {
                        AVFrame* frame = ffmpeg.av_frame_alloc();
                        result = ffmpeg.avcodec_receive_frame(codecContext, frame);

                        if (result == ffmpeg.AVERROR(ffmpeg.EAGAIN) || result == ffmpeg.AVERROR_EOF)
                        {
                            ffmpeg.av_frame_free(&frame);
                            break;
                        }

                        var frameWrapper = new AVFrameWrapper(frame);


                        decodedFramesBuffer.Add(frameWrapper, ct);


                    }
                }
            }
            catch (Exception e)
            {
            }
            finally
            {
                ffmpeg.av_packet_free(&packet);
                ffmpeg.avcodec_free_context(&codecContext);
                ffmpeg.avformat_close_input(&formatContext);
                ffmpeg.av_free(ioContext->buffer);
                ffmpeg.avio_context_free(&ioContext);
                gcHandle.Free();
                while (decodedFramesBuffer.TryTake(out _)) ;
                decodedFramesBuffer.CompleteAdding();
                probeStream.SetLength(0);
                probeStream.Seek(0, SeekOrigin.Begin);
            }
        }


        int frameCounter = 0;
        private void BitmapThreadMethod(double timeBase)
        {
            DateTime startTime = DateTime.Now;
            var converter = new FastYuvToRgbConverter();
            System.Timers.Timer timer = new System.Timers.Timer(1000);
            timer.Elapsed += FPSCallback;
            Task timerTask = Task.Run(timer.Start);

            while (!ct.IsCancellationRequested && !decodedFramesBuffer.IsCompleted)
            {
                try
                {
                    using (var frameWrapper = decodedFramesBuffer.Take(ct))
                    {
                        int width = frameWrapper.Width;
                        int height = frameWrapper.Height;

                        byte[] rgbBuffer = new byte[width * height * 3];

                        converter.ConvertYuvToRgb(frameWrapper.YPlane, frameWrapper.UPlane, frameWrapper.VPlane, rgbBuffer, width, height);

                        double timeOccurensMS = frameWrapper.BestEffortTimestamp * timeBase * 1000;
                        double currentTimeMS = (DateTime.Now - startTime).TotalMilliseconds;

                        if (currentTimeMS < timeOccurensMS)
                        {
                            Thread.Sleep((int)(timeOccurensMS - currentTimeMS));
                        }

                        Application.Current.Dispatcher.InvokeAsync(() =>
                        {
                            writeableBitmap.WritePixels(new Int32Rect(0, 0, width, height), rgbBuffer, width * 3, 0);
                        });
                    }

                    Interlocked.Increment(ref frameCounter);

                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (InvalidOperationException)
                {
                    break;
                }
                finally
                {
                    timer.Stop();
                    timerTask.Wait();
                }
            }
        }

        private void FPSCallback(object? sender, ElapsedEventArgs e)
        {
            Console.WriteLine($"fps: {frameCounter}");
            Interlocked.Exchange(ref frameCounter, 0);
        }

        private int ReadPacket(void* opaque, byte* buf, int bufSize)
        {
            int bytesRead = -1;
            try
            {
                if (isProbing)
                {
                    Span<byte> probeBuffer = new Span<byte>(buf, bufSize);
                    bytesRead = probeStream.Read(probeBuffer);
                }
                else
                {
                    var readResult = pipeReader.ReadAsync(ct).AsTask().GetAwaiter().GetResult();
                    Span<byte> buffer = new Span<byte>(buf, bufSize);
                    bytesRead = (int)Math.Min(readResult.Buffer.Length, bufSize);
                    readResult.Buffer.Slice(0, bytesRead).CopyTo(buffer);
                    pipeReader.AdvanceTo(readResult.Buffer.GetPosition(bytesRead));
                }
            }
            catch (OperationCanceledException) { }

            return bytesRead;
        }

        public unsafe long Seek(void* opaque, long offset, int whence)
        {
            switch (whence)
            {
                case 0: // SEEK_SET
                    probeStream.Seek(offset, SeekOrigin.Begin);
                    break;
                case 1: // SEEK_CUR
                    probeStream.Seek(offset, SeekOrigin.Current);
                    break;
                case 2: // SEEK_END
                    probeStream.Seek(offset, SeekOrigin.End);
                    break;
                case ffmpeg.AVSEEK_SIZE:
                    return probeStream.Length;
            }

            return probeStream.Position;
        }

        public void Stop()
        {
            cts.Cancel();
            decodingThread.Join();
            bitmapThread.Join();
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }
    }


    internal unsafe class FastYuvToRgbConverter
    {
        // Integer constants to avoid floating point calculations
        private const int YMultiplier = 76283; // 1.164383 * 65536
        private const int UMultiplierR = 132252; // 2.017232 * 65536
        private const int VMultiplierB = 52427; // -0.391762 * 65536
        private const int UMultiplierG = -25624; // -0.392 * 65536
        private const int VMultiplierG = -53280; // -0.813 * 65536

        public void ConvertYuvToRgb(byte[] yPlane, byte[] uPlane, byte[] vPlane, byte[] rgbBuffer, int width, int height)
        {
            int halfWidth = width / 2;

            for (int y = 0; y < height; y++)
            {
                int yIndex = y * width;
                int uvIndex = (y / 2) * halfWidth;
                int rgbIndex = yIndex * 3;

                for (int x = 0; x < width; x++)
                {
                    int yValue = (yPlane[yIndex + x] - 16) * YMultiplier;
                    int u = uPlane[uvIndex + (x / 2)] - 128;
                    int v = vPlane[uvIndex + (x / 2)] - 128;

                    int r = (yValue + UMultiplierR * v) >> 16;
                    int g = (yValue + UMultiplierG * u + VMultiplierG * v) >> 16;
                    int b = (yValue + VMultiplierB * u) >> 16;

                    r = Math.Clamp(r, 0, 255);
                    g = Math.Clamp(g, 0, 255);
                    b = Math.Clamp(b, 0, 255);

                    int pixelIndex = rgbIndex + x * 3;
                    rgbBuffer[pixelIndex] = (byte)b;
                    rgbBuffer[pixelIndex + 1] = (byte)g;
                    rgbBuffer[pixelIndex + 2] = (byte)r;
                }
            }
        }
    }







    //internal unsafe class FastYuvToRgbConverter
    //{
    //    private const int YOffset = 16;
    //    private const int UVOffset = 128;
    //    private const int YMultiplier = 298;
    //    private const int UMultiplierB = 516;
    //    private const int UMultiplierG = -100;
    //    private const int VMultiplierG = -208;
    //    private const int VMultiplierR = 409;

    //    public void ConvertYuvToRgb(byte[] yPlane, byte[] uPlane, byte[] vPlane, byte[] rgbBuffer, int width, int height)
    //    {
    //        if (yPlane == null || uPlane == null || vPlane == null || rgbBuffer == null)
    //            throw new ArgumentNullException("Input arrays cannot be null");

    //        if (width <= 0 || height <= 0)
    //            throw new ArgumentException("Width and height must be positive");

    //        int ySize = width * height;
    //        int uvSize = (width / 2) * (height / 2);
    //        int rgbSize = rgbBuffer.Length;

    //        if (yPlane.Length < ySize || uPlane.Length < uvSize || vPlane.Length < uvSize)
    //            throw new ArgumentException("Input YUV arrays are not large enough for the specified dimensions");

    //        if (rgbSize < width * height * 3)
    //            throw new ArgumentException("RGB buffer is not large enough for the specified dimensions");

    //        fixed (byte* yPtr = yPlane)
    //        fixed (byte* uPtr = uPlane)
    //        fixed (byte* vPtr = vPlane)
    //        fixed (byte* rgbPtr = rgbBuffer)
    //        {
    //            if (Avx2.IsSupported)
    //            {
    //                ConvertYuvToRgbAvx2(yPtr, uPtr, vPtr, rgbPtr, width, height);
    //            }
    //            else
    //            {
    //                ConvertYuvToRgbFallback(yPtr, uPtr, vPtr, rgbPtr, width, height);
    //            }
    //        }
    //    }

    //    private static unsafe void ConvertYuvToRgbAvx2(byte* yPtr, byte* uPtr, byte* vPtr, byte* rgbPtr, int width, int height)
    //    {
    //        Vector256<int> yMul = Vector256.Create(YMultiplier);
    //        Vector256<int> uMulB = Vector256.Create(UMultiplierB);
    //        Vector256<int> uMulG = Vector256.Create(UMultiplierG);
    //        Vector256<int> vMulG = Vector256.Create(VMultiplierG);
    //        Vector256<int> vMulR = Vector256.Create(VMultiplierR);
    //        Vector256<short> yOffset = Vector256.Create((short)YOffset);
    //        Vector256<short> uvOffset = Vector256.Create((short)UVOffset);

    //        int vectorWidth = Vector256<byte>.Count / 2; // 16 pixels at a time (changed from 32)
    //        int vectorizedWidth = (width / vectorWidth) * vectorWidth;

    //        for (int y = 0; y < height; y++)
    //        {
    //            byte* yLine = yPtr + y * width;
    //            byte* uLine = uPtr + (y / 2) * (width / 2);
    //            byte* vLine = vPtr + (y / 2) * (width / 2);
    //            byte* rgbLine = rgbPtr + y * width * 3;

    //            for (int x = 0; x < vectorizedWidth; x += vectorWidth)
    //            {
    //                Vector256<short> yVec = Avx2.ConvertToVector256Int16(yLine + x);
    //                Vector256<short> uVec = Avx2.ConvertToVector256Int16(uLine + x / 2);
    //                Vector256<short> vVec = Avx2.ConvertToVector256Int16(vLine + x / 2);

    //                // Expand U and V to match Y
    //                uVec = Avx2.UnpackLow(uVec, uVec);
    //                vVec = Avx2.UnpackLow(vVec, vVec);

    //                // Subtract offset
    //                yVec = Avx2.SubtractSaturate(yVec, yOffset);
    //                uVec = Avx2.SubtractSaturate(uVec, uvOffset);
    //                vVec = Avx2.SubtractSaturate(vVec, uvOffset);

    //                // Convert to int32 and multiply
    //                Vector256<int> yLow = Avx2.MultiplyLow(Avx2.ConvertToVector256Int32(yVec.GetLower()), yMul);
    //                Vector256<int> yHigh = Avx2.MultiplyLow(Avx2.ConvertToVector256Int32(yVec.GetUpper()), yMul);

    //                Vector256<int> uLow = Avx2.ConvertToVector256Int32(uVec.GetLower());
    //                Vector256<int> uHigh = Avx2.ConvertToVector256Int32(uVec.GetUpper());
    //                Vector256<int> vLow = Avx2.ConvertToVector256Int32(vVec.GetLower());
    //                Vector256<int> vHigh = Avx2.ConvertToVector256Int32(vVec.GetUpper());

    //                // Calculate RGB values
    //                Vector256<int> bLow = Avx2.Add(yLow, Avx2.MultiplyLow(uLow, uMulB));
    //                Vector256<int> bHigh = Avx2.Add(yHigh, Avx2.MultiplyLow(uHigh, uMulB));
    //                Vector256<int> gLow = Avx2.Add(Avx2.Add(yLow, Avx2.MultiplyLow(uLow, uMulG)), Avx2.MultiplyLow(vLow, vMulG));
    //                Vector256<int> gHigh = Avx2.Add(Avx2.Add(yHigh, Avx2.MultiplyLow(uHigh, uMulG)), Avx2.MultiplyLow(vHigh, vMulG));
    //                Vector256<int> rLow = Avx2.Add(yLow, Avx2.MultiplyLow(vLow, vMulR));
    //                Vector256<int> rHigh = Avx2.Add(yHigh, Avx2.MultiplyLow(vHigh, vMulR));

    //                // Right shift and saturate
    //                Vector256<short> b = Avx2.PackSignedSaturate(Avx2.ShiftRightArithmetic(bLow, 8), Avx2.ShiftRightArithmetic(bHigh, 8));
    //                Vector256<short> g = Avx2.PackSignedSaturate(Avx2.ShiftRightArithmetic(gLow, 8), Avx2.ShiftRightArithmetic(gHigh, 8));
    //                Vector256<short> r = Avx2.PackSignedSaturate(Avx2.ShiftRightArithmetic(rLow, 8), Avx2.ShiftRightArithmetic(rHigh, 8));

    //                // Pack to bytes with unsigned saturation
    //                Vector256<byte> bBytes = Avx2.PackUnsignedSaturate(b, b);
    //                Vector256<byte> gBytes = Avx2.PackUnsignedSaturate(g, g);
    //                Vector256<byte> rBytes = Avx2.PackUnsignedSaturate(r, r);

    //                // Interleave B, G, and R components
    //                Vector256<byte> bg = Avx2.UnpackLow(bBytes, gBytes);
    //                Vector256<byte> ra = Avx2.UnpackLow(rBytes, Vector256<byte>.Zero);

    //                Vector256<byte> bgr0 = Avx2.UnpackLow(bg, ra);
    //                Vector256<byte> bgr1 = Avx2.UnpackHigh(bg, ra);

    //                // Store results
    //                Avx2.Store(rgbLine + x * 3, bgr0);
    //                Avx2.Store(rgbLine + x * 3 + 24, bgr1);
    //            }

    //            // Handle remaining pixels
    //            for (int x = vectorizedWidth; x < width; x++)
    //            {
    //                int yValue = (yLine[x] - YOffset) * YMultiplier;
    //                int u = uLine[x / 2] - UVOffset;
    //                int v = vLine[x / 2] - UVOffset;

    //                int r = (yValue + VMultiplierR * v) >> 8;
    //                int g = (yValue + UMultiplierG * u + VMultiplierG * v) >> 8;
    //                int b = (yValue + UMultiplierB * u) >> 8;

    //                int index = x * 3;
    //                rgbLine[index] = (byte)Math.Clamp(b, 0, 255);
    //                rgbLine[index + 1] = (byte)Math.Clamp(g, 0, 255);
    //                rgbLine[index + 2] = (byte)Math.Clamp(r, 0, 255);
    //            }
    //        }
    //    }

    //    private static unsafe void ConvertYuvToRgbFallback(byte* yPtr, byte* uPtr, byte* vPtr, byte* rgbPtr, int width, int height)
    //    {
    //        for (int y = 0; y < height; y++)
    //        {
    //            byte* yLine = yPtr + y * width;
    //            byte* uLine = uPtr + (y / 2) * (width / 2);
    //            byte* vLine = vPtr + (y / 2) * (width / 2);
    //            byte* rgbLine = rgbPtr + y * width * 3;

    //            for (int x = 0; x < width; x++)
    //            {
    //                int yValue = (yLine[x] - YOffset) * YMultiplier;
    //                int u = uLine[x / 2] - UVOffset;
    //                int v = vLine[x / 2] - UVOffset;

    //                int r = (yValue + VMultiplierR * v) >> 8;
    //                int g = (yValue + UMultiplierG * u + VMultiplierG * v) >> 8;
    //                int b = (yValue + UMultiplierB * u) >> 8;

    //                int index = x * 3;
    //                rgbLine[index] = (byte)Math.Clamp(b, 0, 255);
    //                rgbLine[index + 1] = (byte)Math.Clamp(g, 0, 255);
    //                rgbLine[index + 2] = (byte)Math.Clamp(r, 0, 255);
    //            }
    //        }
    //    }
    //}





    internal unsafe class AVFrameWrapper : IDisposable
    {
        public int Width { get; }
        public int Height { get; }
        public byte[] YPlane { get; }
        public byte[] UPlane { get; }
        public byte[] VPlane { get; }
        public long BestEffortTimestamp { get; }
        public double TimeBase { get; }

        public AVFrameWrapper(AVFrame* frame)
        {
            Width = frame->width;
            Height = frame->height;
            BestEffortTimestamp = frame->best_effort_timestamp;

            int ySize = frame->linesize[0] * frame->height;
            int uSize = frame->linesize[1] * frame->height / 2;
            int vSize = frame->linesize[2] * frame->height / 2;

            YPlane = new byte[ySize];
            UPlane = new byte[uSize];
            VPlane = new byte[vSize];

            Marshal.Copy((IntPtr)frame->data[0], YPlane, 0, ySize);
            Marshal.Copy((IntPtr)frame->data[1], UPlane, 0, uSize);
            Marshal.Copy((IntPtr)frame->data[2], VPlane, 0, vSize);

            ffmpeg.av_frame_free(&frame);
        }

        public void Dispose()
        {
            GC.Collect();
        }
    }
}
