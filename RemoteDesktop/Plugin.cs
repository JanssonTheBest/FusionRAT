using System;
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
using System.Diagnostics;
using System.Threading;

namespace RemoteDesktopPlugin
{
    public unsafe class Plugin
    {
        private BlockingCollection<AVFrameWrapper> framesToEncodeBuffer = new BlockingCollection<AVFrameWrapper>();
        private CancellationTokenSource cts = new CancellationTokenSource();
        private CancellationToken ct;
        Pipe pipe = new Pipe();
        Thread startThread;
        Thread screenCaptureThread;
        Session _session;
        bool isStreaming = false;
        avio_alloc_context_write_packet writeCallback;
        public Plugin(Session session)
        {
            _session = session;
            _session.OnRemoteDesktop += HandlePacket;
            ct = cts.Token;

            writeCallback = new avio_alloc_context_write_packet(WriteCallback);
            string currentPath = Path.GetFullPath(Directory.GetCurrentDirectory());
            string ffmpegRootPath = Path.GetFullPath(ffmpeg.RootPath);
            if (AreDirectoriesSame(currentPath, ffmpegRootPath))
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
                if (isStreaming)
                {
                    Stop();
                    Thread.Sleep(200);
                }

                Start(30, 100000, adapter, output);
                return;
            }


            if (dto.MouseButton != 0)
            {
                HandleMouseButton(new MouseInput()
                {
                    button = (MouseButton)dto.MouseButton,
                    state = (MouseButtonState)(Convert.ToInt32(!dto.IsPressed)),
                });
                return;
            }

            if (dto.yFactor != null || dto.xFactor != null)
            {

                MoveCursor((int)(dto.xFactor * width), (int)(dto.yFactor*height));
                return;
            }

            if (dto.Char != null)
            {
                HandleKeyboardInput();
                return;
            }

            if (dto.scrollDelta != 0)
            {
                HandleMouseScroll();
                return;
            }


            Stop(true);
        }

        private void HandleMouseButton(MouseInput mouseInput)
        {
            if (mouseInput.button == MouseButton.Left)
            {
                if(mouseInput.state == MouseButtonState.Pressed)
                {
                    MouseDown(MOUSEEVENTF_LEFTDOWN);
                    return;
                }
                MouseDown(MOUSEEVENTF_LEFTUP);
                return;
            }

            if(mouseInput.button == MouseButton.Right)
            {
                if (mouseInput.state == MouseButtonState.Pressed)
                {
                    MouseDown(MOUSEEVENTF_RIGHTDOWN);


                    return;
                }
                MouseDown(MOUSEEVENTF_RIGHTUP);
                return;
            }
        }

        private void HandleMouseMove(int xDelta, int yDelta)
        {

        }

        private void HandleMouseScroll()
        {

        }

        private void HandleKeyboardInput()
        {

        }



        public enum MouseButton
        {
            Right = 1,

            Left,

        }
        public enum MouseButtonState
        {
            Pressed,
            Released,
        }

        public struct MouseInput
        {
            public MouseButton? button;
            public MouseButtonState? state;
            public double xFactor;
            public double yFactor;
            public int? scrollDelta;
        }


        // Input type constant
        const uint INPUT_MOUSE = 0;

        // Mouse event constants
        const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
        const uint MOUSEEVENTF_LEFTUP = 0x0004;
        const uint MOUSEEVENTF_RIGHTDOWN = 0x0008;
        const uint MOUSEEVENTF_RIGHTUP = 0x0010;

        [StructLayout(LayoutKind.Sequential)]
        struct INPUT
        {
            public uint type;
            public InputUnion u;
        }

        [StructLayout(LayoutKind.Explicit)]
        struct InputUnion
        {
            [FieldOffset(0)] public MOUSEINPUT mi;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct MOUSEINPUT
        {
            public int dx;
            public int dy;
            public uint mouseData;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [DllImport("user32.dll", SetLastError = true)]
        static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

        [DllImport("user32.dll")]
        static extern IntPtr GetMessageExtraInfo();

        [DllImport("user32.dll", SetLastError = true)]
        static extern bool SetCursorPos(int X, int Y);

        static void MoveCursor(int x, int y)
        {
            if (!SetCursorPos(x, y))
            {
                Console.WriteLine("Failed to set cursor position");
            }
        }

        static void MouseDown(uint mouseEvent)
        {
            INPUT[] inputs = new INPUT[1];

            inputs[0].type = INPUT_MOUSE;
            inputs[0].u.mi = new MOUSEINPUT
            {
                dx = 0,
                dy = 0,
                mouseData = 0,
                dwFlags = mouseEvent,
                time = 0,
                dwExtraInfo = GetMessageExtraInfo()
            };

            SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(INPUT)));
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

        private void Stop(bool cleanupResources = false)
        {
            isStreaming = false;
            cts.Cancel();
            startThread?.Join();
            screenCaptureThread?.Join();
            Thread.Sleep(500);
            try
            {

                pipe.Writer.Complete();
                pipe.Reader.CancelPendingRead();
                pipe.Reader.Complete();
                pipe = new Pipe();
            }
            catch (Exception ex)
            {
                // Log or handle the exception
            }

            try
            {
                while (framesToEncodeBuffer.TryTake(out var frameWrapper))
                {
                    frameWrapper.Dispose();
                }
            }
            catch (Exception ex)
            {
                // Log or handle the exception
            }

            if (cleanupResources)
            {
                CleanupResources();
            }
        }

        private void CleanupResources()
        {
            _session.OnRemoteDesktop -= HandlePacket;
            _session.OnDisposePlugin.DynamicInvoke(this, EventArgs.Empty);
            cts.Dispose();
            pipe.Reader.Complete();
            pipe.Writer.Complete();
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern IntPtr GetModuleHandle(string lpModuleName);

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

            string[] dllFiles = Directory.GetFiles(path, "*.dll");

            foreach (string dll in dllFiles)
            {
                string fileName = Path.GetFileName(dll);
                IntPtr handle = GetModuleHandle(fileName);

                if (handle != IntPtr.Zero)
                {
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
            byte[] videoChunk = new byte[buf_size];
            fixed (byte* ptrArray = videoChunk)
            {
                System.Buffer.MemoryCopy(buf, ptrArray, buf_size, buf_size);
            }
            _session.SendPacketAsync(new RemoteDesktopDTO
            {
                VideoChunk = videoChunk,
            }).Wait();

            return buf_size;
        }

        int width = 0;
        int height = 0;
        private void Start(int fps, int bitrate, int adapter, int output)
        {
            isStreaming = true;
            cts = new CancellationTokenSource();
            ct = cts.Token;


            startThread = new Thread(() =>
            {
                AVRational timeBase = new AVRational { num = 1, den = fps };

                Tuple<int, int> dimensions = ScreenCaptureLoop(timeBase, adapter, output);

                AVFormatContext* formatContext = null;
                AVCodecContext* codecContext = null;
                SwsContext* swsContext = null;
                AVPacket* packet = null;
                AVIOContext* ioContext = null;
                try
                {
                    bool navidia = true;
                    AVCodec* codec = null;

                    codec = ffmpeg.avcodec_find_decoder_by_name("h264_nvenc");
                    if (codec == null)
                    {
                        codec = ffmpeg.avcodec_find_encoder(AVCodecID.AV_CODEC_ID_H264);
                        navidia = false;
                    }


                    formatContext = ffmpeg.avformat_alloc_context();

                    int bufferSize = 4096 * 8;
                    void* buffer = ffmpeg.av_malloc((ulong)bufferSize);
                    ioContext = ffmpeg.avio_alloc_context(
                   (byte*)buffer,
                   bufferSize,
                   1,
                   null,
                   null,
                   writeCallback,
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
                    codecContext->pix_fmt = AVPixelFormat.AV_PIX_FMT_YUV420P;

                    width=dimensions.Item1;
                    height=dimensions.Item2;


                    if (!navidia)
                    {
                        ffmpeg.av_opt_set(codecContext->priv_data, "preset", "ultrafast", 0);
                        ffmpeg.av_opt_set(codecContext->priv_data, "tune", "zerolatency", 0);
                        ffmpeg.av_opt_set(codecContext->priv_data, "crf", "35", 0);
                        ffmpeg.av_opt_set(codecContext->priv_data, "x264opts", "no-mbtree:sliced-threads:sync-lookahead=0:scenecut=0:intra-refresh=1", 0);
                    }
                    else
                    {
                        ffmpeg.av_opt_set(codecContext->priv_data, "preset", "p1", 0);
                        ffmpeg.av_opt_set(codecContext->priv_data, "tune", "ull", 0);
                        ffmpeg.av_opt_set(codecContext->priv_data, "zerolatency", "1", 0);
                        ffmpeg.av_opt_set(codecContext->priv_data, "rc", "vbr", 0);
                        codecContext->rc_max_rate = 2500000;
                        codecContext->rc_min_rate = bitrate / 8;
                        codecContext->rc_buffer_size = 15000000;
                        ffmpeg.av_opt_set_int(codecContext->priv_data, "cq", 23, 0);
                    }

                    codecContext->max_b_frames = 0;
                    codecContext->gop_size = 2;
                    codecContext->flags |= ffmpeg.AV_CODEC_FLAG_LOW_DELAY;
                    codecContext->thread_count = 8;
                    codecContext->thread_type = ffmpeg.FF_THREAD_FRAME | ffmpeg.FF_THREAD_SLICE;
                    codecContext->flags |= ffmpeg.AV_CODEC_FLAG_GLOBAL_HEADER;
                    codecContext->refs = 1;
                    codecContext->trellis = 0;
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
                                                       ffmpeg.SWS_FAST_BILINEAR, null, null, null);

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
                using var texture = new Texture2D(device, textureDesc);
                long startTime = ffmpeg.av_gettime();
                DateTime debugDate = DateTime.Now;

                try
                {
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

                            var frameWrapper = new AVFrameWrapper();
                            frameWrapper.Frame->format = (int)AVPixelFormat.AV_PIX_FMT_BGRA;
                            frameWrapper.Frame->width = textureDesc.Width;
                            frameWrapper.Frame->height = textureDesc.Height;
                            frameWrapper.Frame->pts = pts;

                            int ret = ffmpeg.av_frame_get_buffer(frameWrapper.Frame, 32);
                            if (ret < 0)
                            {
                                continue;
                            }

                            var map = device.ImmediateContext.MapSubresource(texture, 0, MapMode.Read, SharpDX.Direct3D11.MapFlags.None);
                            var dataPtr = map.DataPointer;

                            for (int y = 0; y < frameWrapper.Frame->height; y++)
                            {
                                System.Buffer.MemoryCopy(dataPtr.ToPointer(),
                                    frameWrapper.Frame->data[0] + y * frameWrapper.Frame->linesize[0],
                                    frameWrapper.Frame->linesize[0],
                                    frameWrapper.Frame->width * 4);
                                dataPtr = IntPtr.Add(dataPtr, map.RowPitch);
                            }

                            device.ImmediateContext.UnmapSubresource(texture, 0);

                            framesToEncodeBuffer.Add(frameWrapper);

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
                }
                catch (Exception ex)
                {
                    // Log or handle the exception
                }
                finally
                {
                    Console.WriteLine("Estimated video length: " + (DateTime.Now - debugDate).TotalMilliseconds + "ms");

                    output1.Dispose();
                    output.Dispose();
                    adapter.Dispose();
                    device.Dispose();
                    factory.Dispose();
                    outputDuplication.Dispose();
                }
            });

            screenCaptureThread.Start();

            return new Tuple<int, int>(textureDesc.Width, textureDesc.Height);
        }




        private void EncodingLoop(AVFormatContext* formatContext, AVCodecContext* codecContext, SwsContext* swsContext, AVStream* stream, AVPacket* packet)
        {
            AVFrameWrapper convertedFrameWrapper = new AVFrameWrapper();

            try
            {
                convertedFrameWrapper.Frame->format = (int)codecContext->pix_fmt;
                convertedFrameWrapper.Frame->width = codecContext->width;
                convertedFrameWrapper.Frame->height = codecContext->height;
                ffmpeg.av_frame_get_buffer(convertedFrameWrapper.Frame, 32);

                while (!ct.IsCancellationRequested)
                {
                    using (var frameWrapper = framesToEncodeBuffer.Take(ct))
                    {
                        ffmpeg.sws_scale(swsContext, frameWrapper.Frame->data, frameWrapper.Frame->linesize, 0, frameWrapper.Frame->height,
                                         convertedFrameWrapper.Frame->data, convertedFrameWrapper.Frame->linesize);

                        convertedFrameWrapper.Frame->pts = frameWrapper.Frame->pts;

                        int result = ffmpeg.avcodec_send_frame(codecContext, convertedFrameWrapper.Frame);
                        if (result < 0) continue;

                        while (result >= 0)
                        {
                            result = ffmpeg.avcodec_receive_packet(codecContext, packet);
                            if (result == ffmpeg.AVERROR(ffmpeg.EAGAIN) || result == ffmpeg.AVERROR_EOF)
                            {
                                break;
                            }

                            packet->stream_index = stream->index;
                            ffmpeg.av_packet_rescale_ts(packet, codecContext->time_base, stream->time_base);
                            ffmpeg.av_interleaved_write_frame(formatContext, packet);
                        }

                        frameWrapper.Unref();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in EncodingLoop: {ex.Message}");
            }
            finally
            {
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
                    ffmpeg.av_interleaved_write_frame(formatContext, packet);
                }
                convertedFrameWrapper.Unref();
            }
        }






        public unsafe class AVFrameWrapper : IDisposable
        {
            public AVFrame* Frame;
            private bool _disposed = false;

            public AVFrameWrapper()
            {
                Frame = ffmpeg.av_frame_alloc();
                if (Frame == null)
                {
                    throw new InvalidOperationException("Failed to allocate AVFrame");
                }
            }

            public void Unref()
            {
                if (Frame != null)
                {
                    ffmpeg.av_frame_unref(Frame);
                }
            }

            public void Dispose()
            {
                Dispose(true);
                GC.Collect();
                GC.SuppressFinalize(this);
            }

            protected virtual void Dispose(bool disposing)
            {
                if (!_disposed)
                {
                    if (disposing)
                    {
                        // Dispose managed resources
                    }

                    // Dispose unmanaged resources
                    if (Frame != null)
                    {
                        fixed (AVFrame** frame = &Frame)
                        {
                            ffmpeg.av_frame_free(frame);
                        }
                        Frame = null;
                    }

                    _disposed = true;
                }
            }

            ~AVFrameWrapper()
            {
                Dispose(false);
            }
        }

    }
}



