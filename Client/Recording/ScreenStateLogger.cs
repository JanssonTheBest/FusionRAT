using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Client.Recording
{
    public class ScreenStateLogger : IDisposable
    {
        private byte[] _previousScreen;
        private bool _run;
        private bool _init;
        private Factory1 _factory;
        private SharpDX.Direct3D11.Device _device;
        private Output1 _output1;
        private Texture2D _screenTexture;
        private int _width;
        private int _height;
        private Bitmap _bitmap;
        private MemoryStream _memoryStream;

        public int Size { get; private set; }
        public EventHandler<byte[]> ScreenRefreshed;

        public ScreenStateLogger()
        {
        }

        public void Start()
        {
            _run = true;
            InitializeDirectX();

            Task.Factory.StartNew(CaptureScreen, TaskCreationOptions.LongRunning);

            SpinWait.SpinUntil(() => _init);
        }

        private void InitializeDirectX()
        {
            _factory = new Factory1();
            var adapter = _factory.GetAdapter1(0);
            _device = new SharpDX.Direct3D11.Device(adapter);
            var output = adapter.GetOutput(0);
            _output1 = output.QueryInterface<Output1>();

            _width = output.Description.DesktopBounds.Right;
            _height = output.Description.DesktopBounds.Bottom;

            var textureDesc = new Texture2DDescription
            {
                CpuAccessFlags = CpuAccessFlags.Read,
                BindFlags = BindFlags.None,
                Format = Format.B8G8R8A8_UNorm,
                Width = _width,
                Height = _height,
                OptionFlags = ResourceOptionFlags.None,
                MipLevels = 1,
                ArraySize = 1,
                SampleDescription = { Count = 1, Quality = 0 },
                Usage = ResourceUsage.Staging
            };
            _screenTexture = new Texture2D(_device, textureDesc);

            _bitmap = new Bitmap(_width, _height, PixelFormat.Format32bppArgb);
            _memoryStream = new MemoryStream();
        }

        private void CaptureScreen()
        {
            using (var duplicatedOutput = _output1.DuplicateOutput(_device))
            {
                while (_run)
                {
                    try
                    {
                        if (TryCaptureFrame(duplicatedOutput, out var screenResource, out var mapSource))
                        {
                            CopyPixelsToBitmap(_bitmap, mapSource);
                            _device.ImmediateContext.UnmapSubresource(_screenTexture, 0);

                            _memoryStream.SetLength(0); // Reset the stream

                            SaveJpegWithQuality(_bitmap, _memoryStream, 50L);
                            ScreenRefreshed?.Invoke(this, _memoryStream.ToArray());
                            _init = true;

                            screenResource.Dispose();
                            duplicatedOutput.ReleaseFrame();
                        }
                    }
                    catch (SharpDXException e)
                    {
                        if (e.ResultCode.Code != SharpDX.DXGI.ResultCode.WaitTimeout.Result.Code)
                        {
                            Trace.TraceError(e.Message);
                            Trace.TraceError(e.StackTrace);
                        }
                    }
                }
            }
        }

        private bool TryCaptureFrame(OutputDuplication duplicatedOutput, out SharpDX.DXGI.Resource screenResource, out DataBox mapSource)
        {
            OutputDuplicateFrameInformation duplicateFrameInformation;

            if (duplicatedOutput.TryAcquireNextFrame(1, out duplicateFrameInformation, out screenResource).Failure)
            {
                mapSource = default;
                return false;
            }

            using (var screenTexture2D = screenResource.QueryInterface<Texture2D>())
                _device.ImmediateContext.CopyResource(screenTexture2D, _screenTexture);

            mapSource = _device.ImmediateContext.MapSubresource(_screenTexture, 0, MapMode.Read, SharpDX.Direct3D11.MapFlags.None);
            return true;
        }

        private void CopyPixelsToBitmap(Bitmap bitmap, DataBox mapSource)
        {
            var boundsRect = new Rectangle(0, 0, _width, _height);
            var mapDest = bitmap.LockBits(boundsRect, ImageLockMode.WriteOnly, bitmap.PixelFormat);
            var sourcePtr = mapSource.DataPointer;
            var destPtr = mapDest.Scan0;

            for (int y = 0; y < _height; y++)
            {
                SharpDX.Utilities.CopyMemory(destPtr, sourcePtr, _width * 4);
                sourcePtr = IntPtr.Add(sourcePtr, mapSource.RowPitch);
                destPtr = IntPtr.Add(destPtr, mapDest.Stride);
            }

            bitmap.UnlockBits(mapDest);
        }

        private void SaveJpegWithQuality(Bitmap bitmap, Stream stream, long quality)
        {
            var encoder = GetEncoder(ImageFormat.Jpeg);
            var encoderParams = new EncoderParameters(1)
            {
                Param = { [0] = new EncoderParameter(Encoder.Quality, quality) }
            };
            bitmap.Save(stream, encoder, encoderParams);
        }

        private ImageCodecInfo GetEncoder(ImageFormat format)
        {
            var codecs = ImageCodecInfo.GetImageDecoders();
            foreach (var codec in codecs)
            {
                if (codec.FormatID == format.Guid)
                {
                    return codec;
                }
            }
            return null;
        }

        public void Stop()
        {
            _run = false;
        }

        public void Dispose()
        {
            _screenTexture?.Dispose();
            _output1?.Dispose();
            _device?.Dispose();
            _factory?.Dispose();
            _bitmap?.Dispose();
            _memoryStream?.Dispose();
        }
    }
}
