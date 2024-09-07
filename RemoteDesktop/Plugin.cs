using System.Runtime.InteropServices;
using System.Collections.Concurrent;
using Common.DTOs.MessagePack;
using System.Drawing.Imaging;
using Common.Communication;
using System.Drawing;
using FFmpeg.AutoGen;
using System.IO.Pipelines;
using SharpDX.DXGI;
using SharpDX.Direct3D11;
using SharpDX;
using System.Runtime.CompilerServices;
using System.Buffers;
using Common.Plugin;

namespace RemoteDesktopPlugin
{
    public unsafe class Plugin
    {
        private BlockingCollection<IntPtr> framesToEncodeBuffer = new BlockingCollection<IntPtr>();
        private CancellationTokenSource cts = new CancellationTokenSource();
        private CancellationToken ct;
        Pipe pipe = new Pipe();
        Thread startThread;
        Thread screenCaptureThread;
        Session _session;

        public Plugin(Session session)
        {


            _session = session;
            _session.OnRemoteDesktop += HandlePacket;
            ct = cts.Token;
            string currentPath = Path.GetFullPath(Directory.GetCurrentDirectory());
            string ffmpegRootPath = Path.GetFullPath(ffmpeg.RootPath);
            if (AreDirectoriesSame(currentPath,ffmpegRootPath))
            {
                RequestDependencies();
            }
            else
            {
                SendScreens();
            }
        }

        bool AreDirectoriesSame(string path1, string path2)
        {
            var fullPath1 = Path.GetFullPath(path1).TrimEnd(Path.DirectorySeparatorChar);
            var fullPath2 = Path.GetFullPath(path2).TrimEnd(Path.DirectorySeparatorChar);
            return string.Equals(fullPath1, fullPath2, StringComparison.OrdinalIgnoreCase);
        }

        private void RequestDependencies()
        {
            _session.SendPacketAsync(new RemoteDesktopDTO()
            {
                LibAVFiles = new Dictionary<string, byte[]>()
            });
        }

        private void HandlePacket(object? sender, EventArgs e)
        {
            var dto = (RemoteDesktopDTO)sender;

            if (dto.LibAVFiles != null)
            {
                InitilizeDependencies(dto.LibAVFiles);
                return;
            }

            if (dto.Options != null)
            {
                int adapter = Convert.ToInt32(dto.Options[0]);
                int output = Convert.ToInt32(dto.Options[1]);
                Start(60, 2500000, adapter, output);
                return;
            }

            Stop();
        }




        private void InitilizeDependencies(Dictionary<string, byte[]> nameFilePairs)
        {
            ffmpeg.RootPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

            Directory.CreateDirectory(ffmpeg.RootPath);

            foreach (var dependency in nameFilePairs)
            {
                File.WriteAllBytes(Path.Combine(ffmpeg.RootPath, dependency.Key), dependency.Value);
            }

            SendScreens();
        }

        private void SendScreens()
        {
            _session.SendPacketAsync(new RemoteDesktopDTO()
            {
                Options = GetScreenInfo().ToArray(),
            });
        }

        public static List<string> GetScreenInfo()
        {
            var screens = new List<string>();
            using (var factory = new Factory1())
            {
                for (int adapterIndex = 0; adapterIndex < factory.Adapters.Count(); adapterIndex++)
                {
                    var adapter = factory.Adapters[adapterIndex];
                    for (int outputIndex = 0; outputIndex < adapter.Outputs.Count(); outputIndex++)
                    {
                        var output = adapter.Outputs[outputIndex];
                        var description = output.Description;
                        string info = $"{adapterIndex}|{outputIndex}|{description.DesktopBounds.Right - description.DesktopBounds.Left}|{description.DesktopBounds.Bottom - description.DesktopBounds.Top}|{description.DeviceName}";
                        screens.Add(info);
                        output.Dispose();
                    }
                    adapter.Dispose();
                }
            }
            return screens;
        }


        private void Stop()
        {
            cts.Cancel();
            startThread.Join();
            screenCaptureThread.Join();
            pipe.Writer.Complete();
            while (framesToEncodeBuffer.TryTake(out _)) ;
            _session.OnRemoteDesktop -= HandlePacket;
            _session.OnDisposePlugin.DynamicInvoke(this, EventArgs.Empty);
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern IntPtr GetModuleHandle(string lpModuleName);

        // Import FreeLibrary from kernel32.dll
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool FreeLibrary(IntPtr hModule);

        public static void FreeLibrariesFromPath(string path)
        {
            if (!Directory.Exists(path))
            {
                Console.WriteLine($"Directory not found: {path}");
                return;
            }

            // Get all DLL files in the specified path
            string[] dllFiles = Directory.GetFiles(path, "*.dll");

            foreach (string dll in dllFiles)
            {
                string fileName = Path.GetFileName(dll);
                IntPtr handle = GetModuleHandle(fileName);

                if (handle != IntPtr.Zero)
                {
                    // Try to free the library
                    if (FreeLibrary(handle))
                    {
                        Console.WriteLine($"Successfully freed {fileName}");
                    }
                    else
                    {
                        Console.WriteLine($"Failed to free {fileName}");
                    }
                }
                else
                {
                    Console.WriteLine($"Library {fileName} not loaded.");
                }
            }
        }

        private int WriteCallback(void* opaque, byte* buf, int buf_size)
        {
            Span<byte> temp = new Span<byte>(buf, buf_size);
            _session.SendPacketAsync(new RemoteDesktopDTO()
            {
                VideoChunk = temp.ToArray()
            }).GetAwaiter().GetResult();

            return buf_size;
        }

        private void Start(int fps, int bitrate, int adapter, int output)
        {
            cts.TryReset();
            ct = cts.Token;
            if (startThread != null)
            {
                if (startThread.IsAlive)
                {
                    Stop();
                }
            }

            startThread = new Thread(() =>
            {
                try
                {
                    pipe.Reset();
                }
                catch (System.InvalidOperationException ex)
                {
                    //Reset is not required
                }

                AVRational timeBase = new AVRational { num = 1, den = fps };

                Tuple<int, int> dimensions = ScreenCaptureLoop(timeBase, adapter, output);

                AVFormatContext* formatContext = null;
                AVCodecContext* codecContext = null;
                SwsContext* swsContext = null;
                AVPacket* packet = null;
                AVIOContext* ioContext = null;
                GCHandle gcHandle = new GCHandle();
                try
                {
                    bool navidia = true;
                    //AVCodec* codec = ffmpeg.avcodec_find_encoder_by_name("h264_nvenc");
                    AVCodec* codec = null;
                    //if (codec == null)
                    //{
                    codec = ffmpeg.avcodec_find_encoder(AVCodecID.AV_CODEC_ID_H264);
                    navidia = false;
                    //}

                    formatContext = ffmpeg.avformat_alloc_context();

                    int bufferSize = 4096;
                    IntPtr buffer = (IntPtr)ffmpeg.av_malloc((ulong)bufferSize);
                    gcHandle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
                    ioContext = ffmpeg.avio_alloc_context(
                   (byte*)buffer,
                   bufferSize,
                   1,
                   null,
                   null,
                   new avio_alloc_context_write_packet(WriteCallback),
                   null
               );
                    formatContext->pb = ioContext;
                    formatContext->flags |= ffmpeg.AVFMT_FLAG_CUSTOM_IO;
                    formatContext->oformat = ffmpeg.av_guess_format("mp4", null, null);

                    codecContext = ffmpeg.avcodec_alloc_context3(codec);
                    codecContext->codec_type = AVMediaType.AVMEDIA_TYPE_VIDEO;
                    codecContext->width = dimensions.Item1;
                    codecContext->height = dimensions.Item2;
                    codecContext->time_base = timeBase;
                    codecContext->bit_rate = bitrate;
                    codecContext->max_b_frames = 0;
                    codecContext->pix_fmt = AVPixelFormat.AV_PIX_FMT_YUV420P;


                    codecContext->flags |= ffmpeg.AV_CODEC_FLAG_LOW_DELAY;  // Enable low delay mode

                    if (!navidia)
                    {
                        ffmpeg.av_opt_set(codecContext->priv_data, "preset", "ultrafast", 0);
                        ffmpeg.av_opt_set(codecContext->priv_data, "tune", "zerolatency", 0);
                        ffmpeg.av_opt_set(codecContext->priv_data, "crf", "23", 0);
                        ffmpeg.av_opt_set(codecContext->priv_data, "x264opts", "no-mbtree:sliced-threads:sync-lookahead=0", 0);
                    }
                    else
                    {
                        ffmpeg.av_opt_set(codecContext->priv_data, "preset", "p4", 0);
                        ffmpeg.av_opt_set(codecContext->priv_data, "tune", "ll", 0);
                        ffmpeg.av_opt_set(codecContext->priv_data, "zerolatency", "1", 0);
                        ffmpeg.av_opt_set(codecContext->priv_data, "rc", "vbr", 0);
                        ffmpeg.av_opt_set(codecContext->priv_data, "cq", "23", 0);
                    }

                    codecContext->thread_count = 4;
                    codecContext->gop_size = 1;
                    codecContext->thread_type = ffmpeg.FF_THREAD_FRAME | ffmpeg.FF_THREAD_SLICE;


                    codecContext->flags |= ffmpeg.AV_CODEC_FLAG_GLOBAL_HEADER;
                    int result = ffmpeg.avcodec_open2(codecContext, codec, null);
                    AVStream* stream = ffmpeg.avformat_new_stream(formatContext, null);
                    stream->time_base = codecContext->time_base;
                    result = ffmpeg.avcodec_parameters_from_context(stream->codecpar, codecContext);
                    AVDictionary* options = null;
                    ffmpeg.av_dict_set(&options, "movflags", "frag_keyframe+empty_moov+default_base_moof+faststart", 0);
                    result = ffmpeg.avformat_write_header(formatContext, &options);
                    swsContext = ffmpeg.sws_getContext(dimensions.Item1, dimensions.Item2, AVPixelFormat.AV_PIX_FMT_BGRA,
                                                       dimensions.Item1, dimensions.Item2, codecContext->pix_fmt,
                                                       ffmpeg.SWS_BILINEAR, null, null, null);

                    packet = ffmpeg.av_packet_alloc();
                    EncodingLoop(formatContext, codecContext, swsContext, stream, packet);

                    result = ffmpeg.av_write_trailer(formatContext);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"An error occurred: {ex.Message}");
                }
                finally
                {
                    ffmpeg.avcodec_close(codecContext);
                    ffmpeg.av_packet_free(&packet);
                    ffmpeg.sws_freeContext(swsContext);
                    ffmpeg.av_free(codecContext);
                    ffmpeg.avformat_free_context(formatContext);
                    ffmpeg.av_free(ioContext->buffer);
                    ffmpeg.avio_context_free(&ioContext);
                    gcHandle.Free();
                }
            });
            startThread.Start();
        }

        private Tuple<int, int> ScreenCaptureLoop(AVRational timeBase, int adapterIndex, int outputIndex)
        {
            Factory1 factory = new Factory1();
            var adapter = factory.GetAdapter1(adapterIndex);
            var device = new SharpDX.Direct3D11.Device(adapter);
            var output = adapter.GetOutput(outputIndex);
            var output1 = output.QueryInterface<Output1>();
            var outputDuplication = output1.DuplicateOutput(device);
            int delay = (int)Math.Round(ffmpeg.av_q2d(timeBase) * 1000);

            var textureDesc = new Texture2DDescription
            {
                CpuAccessFlags = CpuAccessFlags.Read,
                BindFlags = BindFlags.None,
                Format = Format.B8G8R8A8_UNorm,
                Width = output.Description.DesktopBounds.Right - output.Description.DesktopBounds.Left,
                Height = output.Description.DesktopBounds.Bottom - output.Description.DesktopBounds.Top,
                OptionFlags = ResourceOptionFlags.None,
                MipLevels = 1,
                ArraySize = 1,
                SampleDescription = { Count = 1, Quality = 0 },
                Usage = ResourceUsage.Staging
            };

            screenCaptureThread = new Thread(() =>
            {
                var texture = new Texture2D(device, textureDesc);
                long startTime = ffmpeg.av_gettime();

                DateTime debugDate = DateTime.Now;

                while (!ct.IsCancellationRequested)
                {
                    try
                    {
                        var result = outputDuplication.TryAcquireNextFrame(1, out var frameInfo, out var resource);
                        if (result.Failure)
                        {
                            continue;
                        }

                        long currentTime = ffmpeg.av_gettime();
                        long pts = ffmpeg.av_rescale_q(currentTime - startTime, ffmpeg.av_get_time_base_q(), timeBase);

                        using (var t = resource.QueryInterface<Texture2D>())
                        {
                            device.ImmediateContext.CopyResource(t, texture);
                        }

                        var frame = ffmpeg.av_frame_alloc();
                        frame->format = (int)AVPixelFormat.AV_PIX_FMT_BGRA;
                        frame->width = textureDesc.Width;
                        frame->height = textureDesc.Height;
                        frame->pts = pts;

                        int ret = ffmpeg.av_frame_get_buffer(frame, 32);
                        if (ret < 0)
                        {
                            ffmpeg.av_frame_unref(frame);
                            continue;
                        }

                        var map = device.ImmediateContext.MapSubresource(texture, 0, MapMode.Read, SharpDX.Direct3D11.MapFlags.None);
                        var dataPtr = map.DataPointer;

                        for (int y = 0; y < frame->height; y++)
                        {
                            System.Buffer.MemoryCopy(dataPtr.ToPointer(),
                                frame->data[0] + y * frame->linesize[0],
                                frame->linesize[0],
                                frame->width * 4);
                            dataPtr = IntPtr.Add(dataPtr, map.RowPitch);
                        }

                        device.ImmediateContext.UnmapSubresource(texture, 0);

                        framesToEncodeBuffer.Add((IntPtr)frame);

                        resource.Dispose();
                        outputDuplication.ReleaseFrame();

                        long elapsedTime = ffmpeg.av_gettime() - currentTime;
                        long sleepTime = (delay * 1000) - elapsedTime;
                        if (sleepTime > 0)
                        {
                            Thread.Sleep((int)(sleepTime / 1000));
                        }
                    }
                    catch (SharpDXException ex) when (ex.ResultCode.Code == SharpDX.DXGI.ResultCode.WaitTimeout.Result.Code)
                    {
                        // GPU timeout, continue
                    }
                }

                Console.WriteLine("Estimated video length: " + (DateTime.Now - debugDate).TotalMilliseconds + "ms");

                output1.Dispose();
                output.Dispose();
                adapter.Dispose();
                device.Dispose();
                factory.Dispose();
                outputDuplication.Dispose();
            });

            screenCaptureThread.Start();

            return new Tuple<int, int>(textureDesc.Width, textureDesc.Height);
        }

        private void EncodingLoop(AVFormatContext* formatContext, AVCodecContext* codecContext, SwsContext* swsContext, AVStream* stream, AVPacket* packet)
        {
            AVFrame* convertedFrame = ffmpeg.av_frame_alloc();

            while (!ct.IsCancellationRequested)
            {
                try
                {
                    IntPtr framePtr = framesToEncodeBuffer.Take(ct);
                    AVFrame* frame = (AVFrame*)framePtr;

                    convertedFrame->format = (int)codecContext->pix_fmt;
                    convertedFrame->width = codecContext->width;
                    convertedFrame->height = codecContext->height;
                    ffmpeg.av_frame_get_buffer(convertedFrame, 32);

                    ffmpeg.sws_scale(swsContext, frame->data, frame->linesize, 0, frame->height,
                                     convertedFrame->data, convertedFrame->linesize);

                    convertedFrame->pts = frame->pts;

                    int result = ffmpeg.avcodec_send_frame(codecContext, convertedFrame);

                    while (result >= 0)
                    {
                        result = ffmpeg.avcodec_receive_packet(codecContext, packet);
                        if (result == ffmpeg.AVERROR(ffmpeg.EAGAIN) || result == ffmpeg.AVERROR_EOF)
                        {
                            break;
                        }

                        packet->stream_index = stream->index;
                        ffmpeg.av_packet_rescale_ts(packet, codecContext->time_base, stream->time_base);

                        result = ffmpeg.av_interleaved_write_frame(formatContext, packet);
                    }

                    ffmpeg.av_frame_unref(frame);
                    ffmpeg.av_frame_unref(convertedFrame);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error in EncodingLoop: {ex.Message}");
                }
            }

            int ret = ffmpeg.avcodec_send_frame(codecContext, null);
            while (ret >= 0)
            {
                ret = ffmpeg.avcodec_receive_packet(codecContext, packet);
                if (ret == ffmpeg.AVERROR_EOF)
                {
                    break;
                }

                packet->stream_index = stream->index;
                ffmpeg.av_packet_rescale_ts(packet, codecContext->time_base, stream->time_base);

                ret = ffmpeg.av_interleaved_write_frame(formatContext, packet);
            }
        }
    }
}
