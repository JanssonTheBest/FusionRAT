using Common.Comunication;
using Common.DTOs.MessagePack;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace RemoteDesktopPlugin
{
    public class Plugin
    {
        public Session _session { get; set; }
        private List<string[]> screens = new();
        public Plugin(Session session)
        {
            _session = session;
            _session.OnRemoteDesktop += HandlePacket;
            StartRemoteDesktop();
        }

        private void StartRemoteDesktop()
        {
            EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, EnumMonitorsCallback, IntPtr.Zero);
        }

        private void HandlePacket(object? sender, EventArgs e)
        {
            
        }

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
            string[] screen = new string[] { monitorInfo.szDevice, monitorInfo.rcMonitor.Left.ToString(), monitorInfo.rcMonitor.Top.ToString() }
            screens.Add(screen);
            _session.SendPacketAsync(new RemoteDesktopDTO()
            {
                Screen = screen,
            }).GetAwaiter().GetResult();

            return true;
        }

    }
}
