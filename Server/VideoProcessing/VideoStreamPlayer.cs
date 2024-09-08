using System.Collections.Concurrent;
using System.IO.Pipelines;
using System.IO;
using System.Runtime.InteropServices;
using System.Timers;
using System.Windows.Media.Imaging;
using FFmpeg.AutoGen;
using System.Buffers;
using System.Windows;

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
        private BlockingCollection<AVFrameWrapper> decodedFramesBuffer;
        public bool IsPlaying { get; private set; }

        public VideoStreamPlayer()
        {
            ct = cts.Token;
            IsPlaying = false;
        }

        public void Start(PipeReader p, WriteableBitmap wr)
        {
            writeableBitmap = wr;
            pipeReader = p;
            cts = new CancellationTokenSource();
            ct = cts.Token;
            decodingThread = new Thread(DecodingThreadMethod);
            decodedFramesBuffer = new BlockingCollection<AVFrameWrapper>();
            decodingThread.Start();
            IsPlaying = true;
        }

        private void DecodingThreadMethod()
        {
            int result = 0;
            ulong bufferSize = 4096;
            int probeSize = 1024 * 500;
            AVFormatContext* formatContext = ffmpeg.avformat_alloc_context();
            formatContext->probesize = probeSize;
            IntPtr buffer = (IntPtr)ffmpeg.av_malloc(bufferSize);
            GCHandle gcHandle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
            AVIOContext* ioContext = ffmpeg.avio_alloc_context((byte*)buffer, (int)bufferSize, 0, null, new avio_alloc_context_read_packet(ReadPacket), null, new avio_alloc_context_seek(Seek));
            formatContext->pb = ioContext;
            formatContext->flags |= ffmpeg.AVFMT_FLAG_CUSTOM_IO | ffmpeg.AVFMT_FLAG_IGNIDX;
            formatContext->duration = ffmpeg.AV_NOPTS_VALUE;
            DateTime startTime = DateTime.Now;
            var readResult = pipeReader.ReadAtLeastAsync(probeSize).AsTask().GetAwaiter().GetResult();
            probeStream.Write(readResult.Buffer.ToArray());
            probeStream.Seek(0, SeekOrigin.Begin);
            pipeReader.AdvanceTo(readResult.Buffer.Start);
            isProbing = true;
            result = ffmpeg.avformat_open_input(&formatContext, null, null, null);
            isProbing = false;
            readResult = pipeReader.ReadAsync().AsTask().GetAwaiter().GetResult();
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
                BitmapThreadMethod(ffmpeg.av_q2d(stream->time_base), startTime);
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
                ffmpeg.avcodec_close(codecContext);
                ffmpeg.avcodec_free_context(&codecContext);
                ffmpeg.avformat_close_input(&formatContext);
                ffmpeg.av_free(ioContext->buffer);
                ffmpeg.avio_context_free(&ioContext);
                gcHandle.Free();

                while (decodedFramesBuffer.TryTake(out var frameWrapper))
                {
                    frameWrapper.Dispose();
                }

                decodedFramesBuffer.CompleteAdding();
                probeStream.SetLength(0);
                probeStream.Seek(0, SeekOrigin.Begin);
            }
        }


        int frameCounter = 0;
        private void BitmapThreadMethod(double timeBase, DateTime startTime)
        {
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

                        if (currentTimeMS <= timeOccurensMS)
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
            catch (Exception ex)
            {
                return ffmpeg.AVERROR_EOF;
            }

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
            IsPlaying = false;
            cts.Cancel();
            try
            {
                pipeReader.CancelPendingRead();
                pipeReader.Complete();
            }
            catch (Exception ex) { }
            decodingThread.Join();
            bitmapThread.Join();
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }
    }



    internal unsafe class FastYuvToRgbConverter
    {
        // Integer constants to avoid floating point calculations
        private const int YMultiplier = 76283;    // 1.164383 * 65536
        private const int UMultiplierB = 132252;  // 2.017232 * 65536
        private const int VMultiplierR = 104595;  // 1.596027 * 65536
        private const int UMultiplierG = -25624;  // -0.392 * 65536
        private const int VMultiplierG = -53280;  // -0.813 * 65536

        public unsafe void ConvertYuvToRgb(byte[] yPlane, byte[] uPlane, byte[] vPlane, byte[] rgbBuffer, int width, int height)
        {
            int halfWidth = width >> 1;  // Use bitwise right shift instead of division by 2
            fixed (byte* yPtr = yPlane, uPtr = uPlane, vPtr = vPlane, rgbPtr = rgbBuffer)
            {
                for (int y = 0; y < height; y++)
                {
                    int yIndex = y * width;
                    int uvIndex = (y >> 1) * halfWidth;  // Use bitwise right shift for division by 2
                    int rgbIndex = yIndex * 3;

                    // Process four pixels per loop iteration (unrolling)
                    for (int x = 0; x < width; x += 2)
                    {
                        // Shared U, V values for two pixels
                        int u = uPtr[uvIndex + (x >> 1)] - 128; // Right shift instead of division by 2
                        int v = vPtr[uvIndex + (x >> 1)] - 128;

                        // Process first pixel (x)
                        int yValue1 = (yPtr[yIndex + x] - 16) * YMultiplier;
                        int r1 = (yValue1 + VMultiplierR * v) >> 16;
                        int g1 = (yValue1 + UMultiplierG * u + VMultiplierG * v) >> 16;
                        int b1 = (yValue1 + UMultiplierB * u) >> 16;

                        // Manual clamping to ensure colors are within the valid range
                        r1 = r1 < 0 ? 0 : (r1 > 255 ? 255 : r1);
                        g1 = g1 < 0 ? 0 : (g1 > 255 ? 255 : g1);
                        b1 = b1 < 0 ? 0 : (b1 > 255 ? 255 : b1);

                        int pixelIndex1 = rgbIndex + x * 3;
                        rgbPtr[pixelIndex1] = (byte)b1;
                        rgbPtr[pixelIndex1 + 1] = (byte)g1;
                        rgbPtr[pixelIndex1 + 2] = (byte)r1;

                        // Process second pixel (x + 1)
                        if (x + 1 < width)
                        {
                            int yValue2 = (yPtr[yIndex + x + 1] - 16) * YMultiplier;
                            int r2 = (yValue2 + VMultiplierR * v) >> 16;
                            int g2 = (yValue2 + UMultiplierG * u + VMultiplierG * v) >> 16;
                            int b2 = (yValue2 + UMultiplierB * u) >> 16;

                            // Manual clamping for second pixel
                            r2 = r2 < 0 ? 0 : (r2 > 255 ? 255 : r2);
                            g2 = g2 < 0 ? 0 : (g2 > 255 ? 255 : g2);
                            b2 = b2 < 0 ? 0 : (b2 > 255 ? 255 : b2);

                            int pixelIndex2 = rgbIndex + (x + 1) * 3;
                            rgbPtr[pixelIndex2] = (byte)b2;
                            rgbPtr[pixelIndex2 + 1] = (byte)g2;
                            rgbPtr[pixelIndex2 + 2] = (byte)r2;
                        }
                    }
                }
            }
        }
    }



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


