//using FFmpeg.AutoGen;
//using System.Buffers;
//using System.Collections.Concurrent;
//using System.IO;
//using System.IO.Pipelines;
//using System.Runtime.InteropServices;
//using System.Timers;
//using System.Windows;
//using System.Windows.Media.Imaging;
//using static RemoteDesktopPlugin.Plugin;

//namespace Server.VideoProcessing
//{
//    internal unsafe class VideoStreamPlayer
//    {
//        private WriteableBitmap writeableBitmap;
//        private PipeReader pipeReader;
//        private MemoryStream probeStream = new MemoryStream();
//        private bool isProbing = false;
//        private CancellationTokenSource cts = new CancellationTokenSource();
//        private CancellationToken ct;
//        private Thread decodingThread;
//        private Thread bitmapThread;
//        private Thread frameUpdaterThread;
//        private BlockingCollection<AVFrameWrapper> decodedFramesBuffer;
//        public bool IsPlaying { get; private set; }

//        public EventHandler<VideoStreamInfo> OnFPSCallback;

//        public VideoStreamPlayer()
//        {
//            ct = cts.Token;
//            IsPlaying = false;
//        }

//        public void Start(PipeReader p, WriteableBitmap wr)
//        {
//            writeableBitmap = wr;
//            pipeReader = p;
//            cts = new CancellationTokenSource();
//            ct = cts.Token;
//            decodingThread = new Thread(DecodingThreadMethod);
//            decodedFramesBuffer = new BlockingCollection<AVFrameWrapper>();
//            decodingThread.Start();
//            IsPlaying = true;
//        }

//        private void DecodingThreadMethod()
//        {
//            int result = 0;
//            ulong bufferSize = 4096;
//            int probeSize = 1024 * 500;
//            AVFormatContext* formatContext = ffmpeg.avformat_alloc_context();
//            formatContext->probesize = probeSize;
//            IntPtr buffer = (IntPtr)ffmpeg.av_malloc(bufferSize);
//            GCHandle gcHandle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
//            AVIOContext* ioContext = ffmpeg.avio_alloc_context((byte*)buffer, (int)bufferSize, 0, null, new avio_alloc_context_read_packet(ReadPacket), null, new avio_alloc_context_seek(Seek));
//            formatContext->pb = ioContext;
//            formatContext->flags |= ffmpeg.AVFMT_FLAG_CUSTOM_IO | ffmpeg.AVFMT_FLAG_IGNIDX;
//            formatContext->duration = ffmpeg.AV_NOPTS_VALUE;
//            DateTime startTime = DateTime.Now;
//            var readResult = pipeReader.ReadAtLeastAsync(probeSize).AsTask().GetAwaiter().GetResult();
//            probeStream.Write(readResult.Buffer.ToArray());
//            probeStream.Seek(0, SeekOrigin.Begin);
//            pipeReader.AdvanceTo(readResult.Buffer.Start);
//            isProbing = true;
//            result = ffmpeg.avformat_open_input(&formatContext, null, null, null);
//            isProbing = false;
//            readResult = pipeReader.ReadAsync().AsTask().GetAwaiter().GetResult();
//            pipeReader.AdvanceTo(readResult.Buffer.GetPosition(probeStream.Position));

//            AVStream* stream = null;
//            for (int i = 0; i < formatContext->nb_streams; i++)
//            {
//                if (formatContext->streams[i]->codecpar->codec_type == AVMediaType.AVMEDIA_TYPE_VIDEO)
//                {
//                    stream = formatContext->streams[i];
//                    break;
//                }
//            }

//            frameUpdaterThread = new Thread(() =>
//            {
//                FrameUpdaterWorkerMethod(ffmpeg.av_q2d(stream->time_base), startTime);
//            });
//            frameUpdaterThread.Start();
//            bitmapThread = new Thread(() =>
//            {
//                BitmapThreadMethod();
//            });

//            bitmapThread.Start();

//            AVCodec* codec = ffmpeg.avcodec_find_decoder(stream->codecpar->codec_id);
//            AVCodecContext* codecContext = ffmpeg.avcodec_alloc_context3(codec);
//            ffmpeg.av_opt_set(codecContext->priv_data, "preset", "ultrafast", 0);
//            ffmpeg.av_opt_set(codecContext->priv_data, "tune", "fastdecode", 0);
//            ffmpeg.avcodec_parameters_to_context(codecContext, stream->codecpar);
//            codecContext->skip_loop_filter = AVDiscard.AVDISCARD_ALL;
//            codecContext->profile = ffmpeg.FF_PROFILE_H264_BASELINE;
//            codecContext->thread_count = 4;
//            codecContext->thread_type = ffmpeg.FF_THREAD_FRAME;

//            ffmpeg.avcodec_open2(codecContext, null, null);

//            AVPacket* packet = ffmpeg.av_packet_alloc();
//            try
//            {
//                while (!ct.IsCancellationRequested)
//                {
//                    ffmpeg.av_packet_unref(packet);
//                    result = ffmpeg.av_read_frame(formatContext, packet);

//                    if (packet->stream_index != stream->index)
//                    {
//                        continue;
//                    }

//                    result = ffmpeg.avcodec_send_packet(codecContext, packet);

//                    while (!ct.IsCancellationRequested)
//                    {
//                        AVFrame* frame = ffmpeg.av_frame_alloc();
//                        result = ffmpeg.avcodec_receive_frame(codecContext, frame);

//                        if (result == ffmpeg.AVERROR(ffmpeg.EAGAIN) || result == ffmpeg.AVERROR_EOF)
//                        {
//                            ffmpeg.av_frame_free(&frame);
//                            break;
//                        }

//                        var frameWrapper = new AVFrameWrapper(frame);
//                        decodedFramesBuffer.Add(frameWrapper, ct);
//                    }
//                }
//            }
//            catch (Exception e)
//            {
//                // Handle or log exception
//            }
//            finally
//            {
//                ffmpeg.av_packet_free(&packet);
//                ffmpeg.avcodec_close(codecContext);
//                ffmpeg.avcodec_free_context(&codecContext);
//                ffmpeg.avformat_close_input(&formatContext);
//                ffmpeg.av_free(ioContext->buffer);
//                ffmpeg.avio_context_free(&ioContext);
//                gcHandle.Free();

//                while (decodedFramesBuffer.TryTake(out var frameWrapper))
//                {
//                    frameWrapper.Dispose();
//                }

//                decodedFramesBuffer.CompleteAdding();
//                probeStream.SetLength(0);
//                probeStream.Seek(0, SeekOrigin.Begin);
//            }
//        }

//        int frameCounter = 0;
//        double ms = 0;
//        private void BitmapThreadMethod()
//        {
//            SwsContext* swsContext = null;
//            System.Timers.Timer timer = new System.Timers.Timer(1000);
//            timer.Elapsed += FPSCallback;
//            Task timerTask = Task.Run(timer.Start);
//            try
//            {
//                while (!ct.IsCancellationRequested && !decodedFramesBuffer.IsCompleted)
//                {
//                    using (var frameWrapper = decodedFramesBuffer.Take(ct))
//                    {
//                        int width = frameWrapper.Width;
//                        int height = frameWrapper.Height;

//                        if (swsContext == null)
//                        {
//                            swsContext = ffmpeg.sws_getContext(
//                                width, height, AVPixelFormat.AV_PIX_FMT_YUV420P,
//                                width, height, AVPixelFormat.AV_PIX_FMT_BGR24,
//                                ffmpeg.SWS_BILINEAR, null, null, null);
//                        }

//                        byte[] rgbBuffer = new byte[width * height * 3];
//                        fixed (byte* rgbPtr = rgbBuffer)
//                        {
//                            byte_ptrArray4 srcData = new byte_ptrArray4();
//                            srcData[0] = (byte*)frameWrapper.YPlane.ToPointer();
//                            srcData[1] = (byte*)frameWrapper.UPlane.ToPointer();
//                            srcData[2] = (byte*)frameWrapper.VPlane.ToPointer();
//                            srcData[3] = null;

//                            int_array4 srcLinesize = new int_array4();
//                            srcLinesize[0] = frameWrapper.Width;
//                            srcLinesize[1] = frameWrapper.Width / 2;
//                            srcLinesize[2] = frameWrapper.Width / 2;
//                            srcLinesize[3] = 0;

//                            byte_ptrArray4 dstData = new byte_ptrArray4();
//                            dstData[0] = rgbPtr;
//                            dstData[1] = null;
//                            dstData[2] = null;
//                            dstData[3] = null;

//                            int_array4 dstLinesize = new int_array4();
//                            dstLinesize[0] = width * 3;
//                            dstLinesize[1] = 0;
//                            dstLinesize[2] = 0;
//                            dstLinesize[3] = 0;

//                            ffmpeg.sws_scale(swsContext, srcData, srcLinesize, 0, height, dstData, dstLinesize);
//                        }

//                        frameUpdateBuffer.Add((rgbBuffer, frameWrapper.BestEffortTimestamp));
//                    }

//                    Interlocked.Increment(ref frameCounter);
//                }
//            }
//            catch (OperationCanceledException)
//            {
//                return;
//            }
//            catch (InvalidOperationException)
//            {
//                return;
//            }
//            finally
//            {
//                timer.Stop();
//                timerTask.Wait();
//                if (swsContext != null)
//                {
//                    ffmpeg.sws_freeContext(swsContext);
//                }
//            }
//        }

//        BlockingCollection<(byte[], long)> frameUpdateBuffer = new BlockingCollection<(byte[], long)>();
//        private void FrameUpdaterWorkerMethod(double timeBase, DateTime startTime)
//        {
//            try
//            {
//                while (!ct.IsCancellationRequested)
//                {
//                    var result = frameUpdateBuffer.Take(ct);
//                    double timeOccurensMS = result.Item2 * timeBase * 1000;
//                    double currentTimeMS = (DateTime.Now - startTime).TotalMilliseconds;
//                    ms = timeOccurensMS - currentTimeMS;
//                    if (ms > 0)
//                    {
//                        Thread.Sleep((int)(ms));
//                    }
//                    Application.Current.Dispatcher.InvokeAsync(() =>
//                    {
//                        writeableBitmap.WritePixels(new Int32Rect(0, 0, 1920, 1080), result.Item1, 1920 * 3, 0);
//                    });
//                }
//            }
//            catch
//            {
//                // Handle or log exception
//            }
//            finally
//            {
//                while (frameUpdateBuffer.TryTake(out _)) ;
//            }
//        }

//        private void FPSCallback(object? sender, ElapsedEventArgs e)
//        {
//            int temp = 0;

//            if (ms > 0)
//            {
//                temp = (int)ms;
//            }

//            OnFPSCallback?.Invoke(this, new VideoStreamInfo() { DelayMS = temp, FPS = frameCounter });
//            Interlocked.Exchange(ref frameCounter, 0);
//        }

//        private int ReadPacket(void* opaque, byte* buf, int bufSize)
//        {
//            try
//            {
//                if (isProbing)
//                {
//                    return probeStream.Read(new Span<byte>(buf, bufSize));
//                }

//                var readResult = pipeReader.ReadAtLeastAsync(bufSize, ct).AsTask().GetAwaiter().GetResult();
//                var buffer = readResult.Buffer;
//                int bytesRead = (int)Math.Min(buffer.Length, bufSize);
//                buffer.Slice(0, bytesRead).CopyTo(new Span<byte>(buf, bytesRead));
//                pipeReader.AdvanceTo(buffer.GetPosition(bytesRead));
//                return bytesRead;
//            }
//            catch (Exception)
//            {
//                return ffmpeg.AVERROR_EOF;
//            }
//        }

//        public unsafe long Seek(void* opaque, long offset, int whence)
//        {
//            switch (whence)
//            {
//                case 0: // SEEK_SET
//                    probeStream.Seek(offset, SeekOrigin.Begin);
//                    break;
//                case 1: // SEEK_CUR
//                    probeStream.Seek(offset, SeekOrigin.Current);
//                    break;
//                case 2: // SEEK_END
//                    probeStream.Seek(offset, SeekOrigin.End);
//                    break;
//                case ffmpeg.AVSEEK_SIZE:
//                    return probeStream.Length;
//            }

//            return probeStream.Position;
//        }

//        public void Stop()
//        {
//            IsPlaying = false;
//            cts.Cancel();
//            try
//            {
//                pipeReader.CancelPendingRead();
//                pipeReader.Complete();
//            }
//            catch (Exception ex) { }
//            decodingThread.Join();
//            bitmapThread.Join();
//            GC.Collect();
//            GC.WaitForPendingFinalizers();
//        }
//    }

//    internal unsafe class AVFrameWrapper : IDisposable
//    {
//        public int Width { get; }
//        public int Height { get; }
//        public IntPtr YPlane { get; }
//        public IntPtr UPlane { get; }
//        public IntPtr VPlane { get; }
//        public long BestEffortTimestamp { get; }

//        public AVFrameWrapper(AVFrame* frame)
//        {
//            Width = frame->width;
//            Height = frame->height;
//            BestEffortTimestamp = frame->best_effort_timestamp;

//            YPlane = (IntPtr)frame->data[0];
//            UPlane = (IntPtr)frame->data[1];
//            VPlane = (IntPtr)frame->data[2];

//            // We're not freeing the frame here anymore, as we're using the raw pointers
//        }

//        public void Dispose()
//        {
//            // No need to free anything here, as we're using the raw pointers
//        }
//    }

//    public class VideoStreamInfo
//    {
//        public int FPS { get; set; }
//        public int DelayMS { get; set; }
//    }
//}






using FFmpeg.AutoGen;
using System.Buffers;
using System.Collections.Concurrent;
using System.IO;
using System.IO.Pipelines;
using System.Runtime.InteropServices;
using System.Timers;
using System.Windows;
using System.Windows.Media.Imaging;
using static RemoteDesktopPlugin.Plugin;

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
        private Thread frameUpdaterThread;
        private BlockingCollection<AVFrameWrapper> decodedFramesBuffer;
        public bool IsPlaying { get; private set; }

        public EventHandler<VideoStreamInfo> OnFPSCallback;

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

            frameUpdaterThread = new Thread(() =>
            {
                FrameUpdaterWorkerMethod(ffmpeg.av_q2d(stream->time_base), startTime);
            });
            frameUpdaterThread.Start();
            bitmapThread = new Thread(() =>
            {
                BitmapThreadMethod();
            });

            bitmapThread.Start();

            //AVCodec* codec = ffmpeg.avcodec_find_decoder(stream->codecpar->codec_id);
            //AVCodecContext* codecContext = ffmpeg.avcodec_alloc_context3(codec);
            //ffmpeg.av_opt_set(codecContext->priv_data, "preset", "ultrafast", 0);
            //ffmpeg.av_opt_set(codecContext->priv_data, "tune", "fastdecode", 0);
            //ffmpeg.avcodec_parameters_to_context(codecContext, stream->codecpar);
            //codecContext->skip_loop_filter = AVDiscard.AVDISCARD_ALL;
            //codecContext->profile = ffmpeg.FF_PROFILE_H264_BASELINE;
            //codecContext->thread_count = 4;
            //codecContext->thread_type = ffmpeg.FF_THREAD_FRAME;

            AVCodec* codec = ffmpeg.avcodec_find_decoder(stream->codecpar->codec_id);
            AVCodecContext* codecContext = ffmpeg.avcodec_alloc_context3(codec);
            ffmpeg.av_opt_set(codecContext->priv_data, "preset", "ultrafast", 0);
            ffmpeg.av_opt_set(codecContext->priv_data, "tune", "zerolatency", 0);
            ffmpeg.avcodec_parameters_to_context(codecContext, stream->codecpar);
            codecContext->flags |= ffmpeg.AV_CODEC_FLAG_LOW_DELAY;
            codecContext->flags2 |= ffmpeg.AV_CODEC_FLAG2_FAST;
            codecContext->skip_loop_filter = AVDiscard.AVDISCARD_ALL;
            codecContext->skip_frame = AVDiscard.AVDISCARD_NONREF;
            codecContext->profile = ffmpeg.FF_PROFILE_H264_BASELINE;
            codecContext->thread_count = Environment.ProcessorCount;
            codecContext->thread_type = ffmpeg.FF_THREAD_SLICE | ffmpeg.FF_THREAD_FRAME;

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
                // Handle or log exception
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
        double ms = 0;
        private void BitmapThreadMethod()
        {
            SwsContext* swsContext = null;
            System.Timers.Timer timer = new System.Timers.Timer(1000);
            timer.Elapsed += FPSCallback;
            Task timerTask = Task.Run(timer.Start);
            try
            {
                while (!ct.IsCancellationRequested && !decodedFramesBuffer.IsCompleted)
                {
                    using (var frameWrapper = decodedFramesBuffer.Take(ct))
                    {
                        int width = frameWrapper.Width;
                        int height = frameWrapper.Height;

                        if (swsContext == null)
                        {
                            swsContext = ffmpeg.sws_getContext(
                                width, height, AVPixelFormat.AV_PIX_FMT_YUV420P,
                                width, height, AVPixelFormat.AV_PIX_FMT_BGR24,
                                ffmpeg.SWS_BILINEAR, null, null, null);
                        }

                        byte[] rgbBuffer = new byte[width * height * 3];
                        fixed (byte* rgbPtr = rgbBuffer)
                        {
                            byte_ptrArray4 srcData = new byte_ptrArray4();
                            srcData[0] = (byte*)frameWrapper.YPlane;
                            srcData[1] = (byte*)frameWrapper.UPlane;
                            srcData[2] = (byte*)frameWrapper.VPlane;
                            srcData[3] = null;

                            int_array4 srcLinesize = new int_array4();
                            srcLinesize[0] = frameWrapper.Width;
                            srcLinesize[1] = frameWrapper.Width / 2;
                            srcLinesize[2] = frameWrapper.Width / 2;
                            srcLinesize[3] = 0;

                            byte_ptrArray4 dstData = new byte_ptrArray4();
                            dstData[0] = rgbPtr;
                            dstData[1] = null;
                            dstData[2] = null;
                            dstData[3] = null;

                            int_array4 dstLinesize = new int_array4();
                            dstLinesize[0] = width * 3;
                            dstLinesize[1] = 0;
                            dstLinesize[2] = 0;
                            dstLinesize[3] = 0;

                            ffmpeg.sws_scale(swsContext, srcData, srcLinesize, 0, height, dstData, dstLinesize);
                        }

                        frameUpdateBuffer.Add((rgbBuffer, frameWrapper.BestEffortTimestamp));
                    }

                    Interlocked.Increment(ref frameCounter);
                }
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (InvalidOperationException)
            {
                return;
            }
            finally
            {
                timer.Stop();
                timerTask.Wait();
                if (swsContext != null)
                {
                    ffmpeg.sws_freeContext(swsContext);
                }
            }
        }

        BlockingCollection<(byte[], long)> frameUpdateBuffer = new BlockingCollection<(byte[], long)>();
        private void FrameUpdaterWorkerMethod(double timeBase, DateTime startTime)
        {
            try
            {
                while (!ct.IsCancellationRequested)
                {
                    var result = frameUpdateBuffer.Take(ct);
                    double timeOccurensMS = result.Item2 * timeBase * 1000;
                    double currentTimeMS = (DateTime.Now - startTime).TotalMilliseconds;
                    ms = timeOccurensMS - currentTimeMS;
                    if (ms > 0)
                    {
                        Thread.Sleep((int)(ms));
                    }
                    Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        writeableBitmap.WritePixels(new Int32Rect(0, 0, 1920, 1080), result.Item1, 1920 * 3, 0);
                    });
                }
            }
            catch
            {
                // Handle or log exception
            }
            finally
            {
                while (frameUpdateBuffer.TryTake(out _)) ;
            }
        }

        private void FPSCallback(object? sender, ElapsedEventArgs e)
        {
            int temp = 0;

            if (ms > 0)
            {
                temp = (int)ms;
            }

            OnFPSCallback?.Invoke(this, new VideoStreamInfo() { DelayMS = temp, FPS = frameCounter });
            Interlocked.Exchange(ref frameCounter, 0);
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


        private int ReadPacket(void* opaque, byte* buf, int bufSize)
        {
            try
            {
                if (isProbing)
                {
                    return probeStream.Read(new Span<byte>(buf, bufSize));
                }

                var readResult = pipeReader.ReadAtLeastAsync(bufSize, ct).AsTask().GetAwaiter().GetResult();
                var buffer = readResult.Buffer;
                int bytesRead = (int)Math.Min(buffer.Length, bufSize);
                buffer.Slice(0, bytesRead).CopyTo(new Span<byte>(buf, bytesRead));
                pipeReader.AdvanceTo(buffer.GetPosition(bytesRead));
                return bytesRead;
            }
            catch (Exception)
            {
                return ffmpeg.AVERROR_EOF;
            }
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

    }

    internal unsafe class AVFrameWrapper : IDisposable
    {
        public int Width { get; }
        public int Height { get; }
        public IntPtr YPlane { get; }
        public IntPtr UPlane { get; }
        public IntPtr VPlane { get; }
        public long BestEffortTimestamp { get; }

        public AVFrameWrapper(AVFrame* frame)
        {
            Width = frame->width;
            Height = frame->height;
            BestEffortTimestamp = frame->best_effort_timestamp;

            YPlane = (IntPtr)frame->data[0];
            UPlane = (IntPtr)frame->data[1];
            VPlane = (IntPtr)frame->data[2];

        }

        public void Dispose()
        {
        }
    }

    public class VideoStreamInfo
    {
        public int FPS { get; set; }
        public int DelayMS { get; set; }
    }
}