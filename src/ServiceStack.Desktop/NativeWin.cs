using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using ServiceStack.Text.Pools;

namespace ServiceStack.Desktop
{
    public static partial class NativeWin
    {
        public static Dictionary<string,string> GetDesktopInfo() => new Dictionary<string, string> {
            ["tool"] = DesktopState.Tool,
            ["toolVersion"] = DesktopState.ToolVersion,
            ["chromeVersion"] = DesktopState.ChromeVersion,
        };

        public static void SetDesktopInfo(Dictionary<string, string> info)
        {
            if (info.TryGetValue("tool", out var tool))
                DesktopState.Tool = tool;
            if (info.TryGetValue("toolVersion", out var toolVersion))
                DesktopState.ToolVersion = toolVersion;
            if (info.TryGetValue("chromeVersion", out var chromeVersion))
                DesktopState.ChromeVersion = chromeVersion;
        }

        public static Dictionary<string, object> ToObject(System.Drawing.Size size) => new Dictionary<string, object> {
            ["width"] = size.Width,
            ["height"] = size.Height,
        };

        public static Dictionary<string, object> ToObject(System.Drawing.Rectangle rect) => new Dictionary<string, object> {
            ["top"] = rect.Top,
            ["left"] = rect.Left,
            ["bottom"] = rect.Bottom,
            ["right"] = rect.Right,
        };

        public static Dictionary<string, object> ToObject(Rectangle rect) => new Dictionary<string, object> {
            ["top"] = rect.Top,
            ["left"] = rect.Left,
            ["bottom"] = rect.Bottom,
            ["right"] = rect.Right,
        };

        public static Dictionary<string, object> ToObject(MonitorInfo mi) => new Dictionary<string, object> {
            ["monitor"] = ToObject(mi.Monitor),
            ["work"] = ToObject(mi.WorkArea),
            ["flags"] = (int)mi.Flags,
        };

        public static bool Open(string cmd)
        {
            Start(cmd);
            return true;
        }
        
        public static Process Start(string url)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return Process.Start(new ProcessStartInfo("cmd", $"/c start {url}"));
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                return Process.Start("xdg-open", url);
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                return Process.Start("open", url);
            throw new NotSupportedException("Unknown platform");
        }

        public static IntPtr FindWindowByName(string name) => FindWindow(null, null);

        public static string ExpandEnvVars(string path) => String.IsNullOrEmpty(path) || path.IndexOf('%') == -1
            ? path
            : Environment.ExpandEnvironmentVariables(path);

        public static DialogResult OpenFile(this IntPtr hWnd, Dictionary<string, object> options)
        {
            if (hWnd == IntPtr.Zero) return default;
            var isFolderPicker = options.TryGetValue("isFolderPicker", out var oIsFolderPicker) && oIsFolderPicker is bool b && b;

            string normalizeFilter(string filter) => !String.IsNullOrEmpty(filter)
                ? filter.IndexOf('\0') >= 0
                    ? filter
                    : filter.Replace("|","\0") + "\0\0"
                : isFolderPicker
                    ? "Folder only\0$$$.$$$\0\0"
                    : "All Files\0*.*\0\0"; 
            
            var dlgArgs = new OpenFileName();
            dlgArgs.lStructSize = Marshal.SizeOf(dlgArgs);
            dlgArgs.lpstrFile = new string(new char[256]);
            dlgArgs.nMaxFile = dlgArgs.lpstrFile.Length;
            dlgArgs.lpstrFileTitle = new string(new char[46]);
            dlgArgs.nMaxFileTitle = dlgArgs.lpstrFileTitle.Length;

            dlgArgs.hwndOwner = hWnd;

            dlgArgs.Flags = options.TryGetValue("flags", out var oFlags) ? Convert.ToInt32(oFlags) : 0x00080000;
            dlgArgs.lpstrTitle = options.TryGetValue("title", out var oTitle) ? oTitle as string 
                : isFolderPicker
                    ? "Select a Folder"
                    : "Open File Dialog...";
            dlgArgs.lpstrFilter = normalizeFilter(options.TryGetValue("filter", out var oFilter) ? oFilter as string : null);
            if (options.TryGetValue("filterIndex", out var oFilterIndex))
                dlgArgs.nFilterIndex = Convert.ToInt32(oFilterIndex);
            dlgArgs.lpstrInitialDir = options.TryGetValue("initialDir", out var oInitialDir)
                ? ExpandEnvVars(oInitialDir as string)
                : Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            if (options.TryGetValue("templateName", out var oTemplateName))
                dlgArgs.lpTemplateName = oTemplateName as string;
            if (options.TryGetValue("defaultExt", out var oDefaultExt))
                dlgArgs.lpstrDefExt = oDefaultExt as string;

            if (isFolderPicker)
            {
                //HACK http://unafaltadecomprension.blogspot.com/2013/04/browsing-for-files-and-folders-c.html
                dlgArgs.Flags |= (int) (FileOpenOptions.NoValidate | FileOpenOptions.PathMustExist);
                dlgArgs.lpstrFile = "Folder Selection.";
            }

            if (GetOpenFileName(dlgArgs))
            {
                var file = isFolderPicker
                    ? dlgArgs.lpstrFile.Replace("Folder Selection", "")
                    : dlgArgs.lpstrFile;
                var ret = new DialogResult {
                    File = file,
                    FileTitle = dlgArgs.lpstrFileTitle,
                    Ok = true,
                };
                return ret;
            }

            return new DialogResult();
        }
        
        public static void CenterToScreen(this IntPtr hWnd, bool useWorkArea = true)
        {
            if (GetNearestMonitorInfo(hWnd, out var mi))
            {
                var rectangle = useWorkArea ? mi.WorkArea : mi.Monitor;
                var num1 = rectangle.Width / 2;
                var num2 = rectangle.Height / 2;
                var windowSize = GetWindowSize(hWnd);
                SetPosition(hWnd, num1 - windowSize.Width / 2, num2 - windowSize.Height / 2);
            }
        }
        
        public static bool SetSize(this IntPtr hWnd, int width, int height)
        {
            if (hWnd == IntPtr.Zero) return default;
            return SetWindowPos(hWnd, IntPtr.Zero, -1, -1, width, height, 
                SetWindowPosFlags.NoActivate | SetWindowPosFlags.NoMove | SetWindowPosFlags.NoZOrder);
        }

        public static bool SetPosition(this IntPtr hWnd, int x, int y)
        {
            if (hWnd == IntPtr.Zero) return default;
            return SetWindowPos(hWnd, IntPtr.Zero, x, y, -1, -1, 
                SetWindowPosFlags.NoActivate | SetWindowPosFlags.NoSize | SetWindowPosFlags.NoZOrder);
        }

        public static bool SetPosition(this IntPtr hWnd, int x, int y, int width, int height)
        {
            if (hWnd == IntPtr.Zero) return default;
            return SetWindowPos(hWnd, IntPtr.Zero, x, y, width, height, 
                SetWindowPosFlags.NoActivate | SetWindowPosFlags.NoZOrder);
        }

        public static bool SetPosition(this IntPtr hWnd, int x, int y, int width, int height, SetWindowPosFlags flags)
        {
            if (hWnd == IntPtr.Zero) return default;
            return SetWindowPos(hWnd, IntPtr.Zero, x, y, width, height, flags);
        }
        public static bool SetPosition(this IntPtr hWnd, ref Rectangle rect) => SetPosition(hWnd, 
            rect.Left, rect.Top, rect.Width, rect.Height, SetWindowPosFlags.NoActivate | SetWindowPosFlags.NoZOrder);
        public static bool SetPosition(this IntPtr hWnd, Rectangle rect) => SetPosition(hWnd, ref rect);
        public static bool SetPosition(this IntPtr hWnd, Rectangle rect, SetWindowPosFlags flags) => SetPosition(hWnd, ref rect, flags);
        public static bool SetPosition(this IntPtr hWnd, ref Rectangle rect, SetWindowPosFlags flags) => 
            SetPosition(hWnd, rect.Left, rect.Top, rect.Width, rect.Height, flags);

        public static void RedrawFrame(this IntPtr hWnd) => SetPosition(hWnd, new Rectangle(),
                SetWindowPosFlags.FrameChanged | SetWindowPosFlags.NoMove | SetWindowPosFlags.NoSize);

        public static float GetScalingFactor(this IntPtr hdc)
        {
            int logicalScreenHeight = GetDeviceCaps(hdc, VERTRES);
            int physicalScreenHeight = GetDeviceCaps(hdc, DESKTOPVERTRES);
            return (float) physicalScreenHeight / (float) logicalScreenHeight;
        }

        public static System.Drawing.Size GetScreenResolution()
        {
            IntPtr hdc = GetDC(IntPtr.Zero);
            try
            {
                var scalingFactor = GetScalingFactor(hdc);
                return new System.Drawing.Size(
                    (int) (GetSystemMetrics(SystemMetric.SM_CXSCREEN) * scalingFactor),
                    (int) (GetSystemMetrics(SystemMetric.SM_CYSCREEN) * scalingFactor)
                );
            }
            finally
            {
                NativeWin.ReleaseDC(IntPtr.Zero, hdc);
            }
        }

        public static MonitorInfo? SetKioskMode(this IntPtr hWnd)
        {
            if (hWnd == IntPtr.Zero) return null;
            //https://devblogs.microsoft.com/oldnewthing/20100412-00/?p=14353
            var dwStyle = GetWindowLongPtr(hWnd, (int) WindowLongFlags.GWL_STYLE);
            if (GetPrimaryMonitorInfo(hWnd, out var mi))
            {
                SetWindowLongPtr(hWnd, (int) WindowLongFlags.GWL_STYLE,
                    new IntPtr((int) dwStyle & (int) ~WindowStyles.WS_OVERLAPPEDWINDOW));

                var mr = mi.Monitor;
                SetWindowPos(hWnd, (IntPtr) HwndZOrder.HWND_TOPMOST,
                    mr.Left, mr.Top,
                    mr.Right - mr.Left,
                    mr.Bottom - mr.Top,
                    SetWindowPosFlags.NoZOrder | SetWindowPosFlags.FrameChanged);
                return mi;
            }
            return null;
        }
        
        public static void SetWindowFullScreen(this IntPtr hWnd)
        {
            if (hWnd == IntPtr.Zero) return;
            var res = GetScreenResolution();
            SetWindowPos(hWnd, IntPtr.Zero,
                0, 0,
                res.Width,
                res.Height,
                SetWindowPosFlags.ShowWindow);
        }

        public static void ResizeWindow(this IntPtr hWnd, int width, int height)
        {
            if (hWnd == IntPtr.Zero) return;
            SetWindowPos(hWnd, IntPtr.Zero,
                0, 0, width, height,
                SetWindowPosFlags.NoZOrder
            );
        }
        public static Rectangle GetClientRect(this IntPtr hWnd)
        {
            if (hWnd == IntPtr.Zero) return default;
            GetClientRect(hWnd, out var lpRect);
            return lpRect;
        }

        public static System.Drawing.Size GetClientSize(this IntPtr hWnd)
        {
            if (hWnd == IntPtr.Zero) return default;
            GetClientRect(hWnd, out var rectangle);
            return new System.Drawing.Size
            {
                Width = rectangle.Width,
                Height = rectangle.Height
            };
        }

        public static System.Drawing.Size GetWindowSize(this IntPtr hWnd)
        {
            if (hWnd == IntPtr.Zero) return default;
            GetWindowRect(hWnd, out var rectangle);
            return new System.Drawing.Size
            {
                Width = rectangle.Width,
                Height = rectangle.Height
            };
        }

        public static bool GetPrimaryMonitorInfo(this IntPtr hWnd, out MonitorInfo monitorInfo)
        {
            if (hWnd == IntPtr.Zero) { monitorInfo = default; return default; }
            var mi = new MonitorInfo();
            mi.Size = Marshal.SizeOf(mi);
            var ret = GetMonitorInfo(MonitorFromWindow(hWnd, MONITOR_DEFAULTTOPRIMARY), ref mi);
            monitorInfo = mi;
            return ret;
        }

        public static bool GetNearestMonitorInfo(this IntPtr hWnd, out MonitorInfo monitorInfo)
        {
            if (hWnd == IntPtr.Zero) { monitorInfo = default; return default; }
            var mi = new MonitorInfo();
            mi.Size = Marshal.SizeOf(mi);
            var ret = GetMonitorInfo(MonitorFromWindow(hWnd, MONITOR_DEFAULTTONEAREST), ref mi);
            monitorInfo = mi;
            return ret;
        }

        public static void ShowScrollBar(this IntPtr hWnd, bool show)
        {
            if (hWnd == IntPtr.Zero) return;
            ShowScrollBar(hWnd, SB_BOTH, show);
        }
        
        public static bool SetText(this IntPtr hWnd, string text)
        {
            if (hWnd == IntPtr.Zero) return default;
            return SetWindowText(hWnd, text);
        }

        public static string GetText(this IntPtr hWnd)
        {
            if (hWnd == IntPtr.Zero) return default;
            var size = GetWindowTextLength(hWnd);
            if (size > 0)
            {
                var len = size + 1;
                var sb = new StringBuilder(len);
                return GetWindowText(hWnd, sb, len) > 0 ? sb.ToString() : string.Empty;
            }
            return string.Empty;
        }

        public static void TryOpenClipboard()
        {
            var num = 10;
            while (true)
            {
                if (OpenClipboard(default))
                {
                    break;
                }

                if (--num == 0)
                {
                    ThrowWin32();
                }

                Thread.Sleep(100);
            }
        }

        public static string GetClipboardAsString()
        {
            TryOpenClipboard();
            
            IntPtr handle = default;
            IntPtr pointer = default;
            byte[] buff = null;
            try
            {
                handle = GetClipboardData(cfUnicodeText);
                if (handle == default)
                    return null;

                pointer = GlobalLock(handle);
                if (pointer == default)
                    return null;

                var size = GlobalSize(handle);
                buff = BufferPool.GetBuffer(size);
                Marshal.Copy(pointer, buff, 0, size);
                return Encoding.Unicode.GetString(buff, 0, size).TrimEnd('\0');
            }
            finally
            {
                if (buff != null)
                    BufferPool.ReleaseBufferToPool(ref buff);
                if (pointer != default)
                    GlobalUnlock(handle);
                CloseClipboard();
            }
        }

        public static bool SetStringInClipboard(string text)
        {
            TryOpenClipboard();
            
            IntPtr hGlobal = default;
            try
            {
                EmptyClipboard();

                var bytes = (text.Length + 1) * 2;
                hGlobal = Marshal.AllocHGlobal(bytes);

                if (hGlobal == default)
                    ThrowWin32();

                var target = GlobalLock(hGlobal);

                if (target == default)
                    ThrowWin32();

                try
                {
                    Marshal.Copy(text.ToCharArray(), 0, target, text.Length);
                }
                finally
                {
                    GlobalUnlock(target);
                }

                if (SetClipboardData(cfUnicodeText, hGlobal) == default)
                    ThrowWin32();

                hGlobal = default;
            }
            finally
            {
                if (hGlobal != default)
                    Marshal.FreeHGlobal(hGlobal);

                CloseClipboard();
            }
            return true;
        }

        static void ThrowWin32() => throw new Win32Exception(Marshal.GetLastWin32Error());

        public static void ApplyTransparency(IntPtr hWnd, byte transparency)
        {
            const int GWL_EXSTYLE = (int) WindowLongFlags.GWL_EXSTYLE;
            const int WS_EX_LAYERED = (int)WindowStylesEx.WS_EX_LAYERED;
            SetWindowLongPtr(hWnd, GWL_EXSTYLE, new IntPtr(GetWindowLongPtr(hWnd, GWL_EXSTYLE).ToInt32() | WS_EX_LAYERED));
            SetLayeredWindowAttributes(hWnd, 0, transparency, LWA_ALPHA);
        }
    }
    
    [Flags]
    public enum WindowPlacementFlags
    {
        SETMINPOSITION = 0x0001,
        RESTORETOMAXIMIZED = 0x0002,
        ASYNCWINDOWPLACEMENT = 0x0004
    }
    
    [StructLayout(LayoutKind.Sequential)]
    public struct WindowPlacement
    {
        public uint Size;
        public WindowPlacementFlags Flags;
        public ShowWindowCommands ShowCmd;
        public Point MinPosition;
        public Point MaxPosition;
        public Rectangle NormalPosition;
    }
    
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    public struct MonitorInfo
    {
        public int Size;
        public Rectangle Monitor;
        public Rectangle WorkArea;
        public uint Flags;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = NativeWin.CCHDEVICENAME)]
        public string DeviceName;
        public void Init()
        {
            this.Size = 40 + 2 * NativeWin.CCHDEVICENAME;
            this.DeviceName = string.Empty;
        }
    }
    
    [StructLayout(LayoutKind.Sequential)]
    public struct Rectangle : IEquatable<Rectangle>
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;

        public Rectangle(int left = 0, int top = 0, int right = 0, int bottom = 0)
        {
            this.Left = left;
            this.Top = top;
            this.Right = right;
            this.Bottom = bottom;
        }

        public Rectangle(int width = 0, int height = 0)
            : this(0, 0, width, height) {}

        public Rectangle(int all = 0) : this(all, all, all, all) {}

        public int Width => Right - Left;
        public int Height => Bottom - Top;
        public bool Equals(Rectangle other) => Left == other.Left && Right == other.Right && Top == other.Top && Bottom == other.Bottom;
        public override bool Equals(object obj) => obj is Rectangle other && this.Equals(other);
        public static bool operator ==(Rectangle left, Rectangle right) => left.Equals(right);
        public static bool operator !=(Rectangle left, Rectangle right) => !(left == right);
        public override int GetHashCode() => ((Left * 397 ^ Top) * 397 ^ Right) * 397 ^ Bottom;
        public Size Size => new Size(this.Width, this.Height);
        public bool IsEmpty => Left == 0 && Top == 0 && Right == 0 && Bottom == 0;
    }
    
    [StructLayout(LayoutKind.Sequential)]
    public struct Size : IEquatable<Size>
    {
        public int Width, Height;
        public Size(int width, int height)
        {
            this.Width = width;
            this.Height = height;
        }
        public bool IsEmpty => this.Width == 0 && this.Height == 0;
        public bool Equals(Size other) => Width == other.Width && Height == other.Height;
        public override bool Equals(object obj) => obj is Size && Equals((Size)obj);
        public override int GetHashCode() { unchecked { return (Width * 397) ^ Height; } }
        public static bool operator ==(Size left, Size right) => left.Equals(right);
        public static bool operator !=(Size left, Size right) => !(left == right);
        public void Offset(int  width, int  height) { Width += width; Height += height; }
        public void Set(int  width, int  height) { Width = width; Height = height; }
        public override string ToString() {
            var culture = CultureInfo.CurrentCulture;
            return $"{{ Width = {Width.ToString(culture)}, Height = {Height.ToString(culture)} }}";
        }
    }
    
    [StructLayout(LayoutKind.Sequential)]
    public struct Point : IEquatable<Point>
    {
        public int X, Y;
        public Point(int  x, int  y)
        {
            X = x;
            Y = y;
        }
        public bool IsEmpty => this.X == 0 && this.Y == 0;
        public bool Equals(Point other) => X == other.X && Y == other.Y;
        public override bool Equals(object obj) => obj is Point point && Equals(point);
        public override int GetHashCode() { unchecked { return (X*397) ^ Y; } }
        public static bool operator ==(Point left, Point right) => left.Equals(right);
        public static bool operator !=(Point left, Point right) => !(left == right);
        public void Offset(int  x, int  y) { X += x; Y += y; }
        public void Set(int  x, int  y) { X = x; Y = y; }
        public override string ToString() {
            var culture = CultureInfo.CurrentCulture;
            return $"{{ X = {X.ToString(culture)}, Y = {Y.ToString(culture)} }}";
        }
    }
    
    public enum HwndZOrder
    {
        HWND_BOTTOM = 1,
        HWND_NOTOPMOST = -2,
        HWND_TOP = 0,
        HWND_TOPMOST = -1
    }
    
    [Flags]
    public enum WindowLongFlags
    {
        GWL_EXSTYLE = -20,
        GWLP_HINSTANCE = -6,
        GWLP_HWNDPARENT = -8,
        GWLP_ID = -12,
        GWL_STYLE = -16,
        GWLP_USERDATA = -21,
        GWLP_WNDPROC = -4,
        DWLP_DLGPROC = 0x4,
        DWLP_MSGRESULT = 0,
        DWLP_USER = 0x8
    }
    
    [Flags]
    public enum SetWindowPosFlags : uint
    {
        AsyncWindowPosition = 0x4000,
        DeferErase = 0x2000,
        DrawFrame = 0x0020,
        FrameChanged = 0x0020,
        HideWindow = 0x0080,
        NoActivate = 0x0010,
        NoCopyBits = 0x0100,
        NoMove = 0x0002,
        NoOwnerZOrder = 0x0200,
        NoRedraw = 0x0008,
        NoReposition = 0x0200,
        NoSendChanging = 0x0400,
        NoSize = 0x0001,
        NoZOrder = 0x0004,
        ShowWindow = 0x0040,
    }
    
    [Flags]
    public enum WindowStyles
    {
        WS_BORDER = 0x00800000,
        WS_CAPTION = 0x00C00000,
        WS_CHILD = 0x40000000,
        WS_CHILDWINDOW = 0x40000000,
        WS_CLIPCHILDREN = 0x02000000,
        WS_CLIPSIBLINGS = 0x04000000,
        WS_DISABLED = 0x08000000,
        WS_DLGFRAME = 0x00400000,
        WS_GROUP = 0x00020000,
        WS_HSCROLL = 0x00100000,
        WS_ICONIC = 0x20000000,
        WS_MAXIMIZE = 0x01000000,
        WS_MAXIMIZEBOX = 0x00010000,
        WS_MINIMIZE = 0x20000000,
        WS_MINIMIZEBOX = 0x00020000,
        WS_OVERLAPPED = 0x00000000,
        WS_OVERLAPPEDWINDOW =
            WS_OVERLAPPED | WS_CAPTION | WS_SYSMENU | WS_THICKFRAME | WS_MINIMIZEBOX | WS_MAXIMIZEBOX,
        WS_POPUP = unchecked ((int) 0x80000000),
        WS_POPUPWINDOW = WS_POPUP | WS_BORDER | WS_SYSMENU,
        WS_SIZEBOX = 0x00040000,
        WS_SYSMENU = 0x00080000,
        WS_TABSTOP = 0x00010000,
        WS_THICKFRAME = 0x00040000,
        WS_TILED = 0x00000000,
        WS_TILEDWINDOW = WS_OVERLAPPED | WS_CAPTION | WS_SYSMENU | WS_THICKFRAME | WS_MINIMIZEBOX | WS_MAXIMIZEBOX,
        WS_VISIBLE = 0x10000000,
        WS_VSCROLL = 0x00200000
    }
    
    [Flags]
    public enum WindowStylesEx : uint
    {
        WS_EX_ACCEPTFILES = 0x00000010,
        WS_EX_APPWINDOW = 0x00040000,
        WS_EX_CLIENTEDGE = 0x00000200,
        WS_EX_COMPOSITED = 0x02000000,
        WS_EX_CONTEXTHELP = 0x00000400,
        WS_EX_CONTROLPARENT = 0x00010000,
        WS_EX_DLGMODALFRAME = 0x00000001,
        WS_EX_LAYERED = 0x00080000,
        WS_EX_LAYOUTRTL = 0x00400000,
        WS_EX_LEFT = 0x00000000,
        WS_EX_LEFTSCROLLBAR = 0x00004000,
        WS_EX_LTRREADING = 0x00000000,
        WS_EX_MDICHILD = 0x00000040,
        WS_EX_NOACTIVATE = 0x08000000,
        WS_EX_NOINHERITLAYOUT = 0x00100000,
        WS_EX_NOPARENTNOTIFY = 0x00000004,
        WS_EX_NOREDIRECTIONBITMAP = 0x00200000,
        WS_EX_OVERLAPPEDWINDOW = WS_EX_WINDOWEDGE | WS_EX_CLIENTEDGE,
        WS_EX_PALETTEWINDOW = WS_EX_WINDOWEDGE | WS_EX_TOOLWINDOW | WS_EX_TOPMOST,
        WS_EX_RIGHT = 0x00001000,
        WS_EX_RIGHTSCROLLBAR = 0x00000000,
        WS_EX_RTLREADING = 0x00002000,
        WS_EX_STATICEDGE = 0x00020000,
        WS_EX_TOOLWINDOW = 0x00000080,
        WS_EX_TOPMOST = 0x00000008,
        WS_EX_TRANSPARENT = 0x00000020,
        WS_EX_WINDOWEDGE = 0x00000100
    }
    
    [Flags]
    public enum ShowWindowCommands
    {
        ForceMinimize = 11,
        Hide = 0,
        Maximize = 3,
        Minimize = 6,
        Restore = 9,
        Show = 5,
        ShowDefault = 10,
        ShowMaximized = 3,
        ShowMinimized = 2,
        ShowMinNoActive = ShowMinimized | Show,
        ShowNA = 8,
        ShowNoActivate = 4,
        Normal = 1
    }

    public enum SystemMetric
    {
        SM_CXSCREEN = 0,  // 0x00
        SM_CYSCREEN = 1,  // 0x01
        SM_CXVSCROLL = 2,  // 0x02
        SM_CYHSCROLL = 3,  // 0x03
        SM_CYCAPTION = 4,  // 0x04
        SM_CXBORDER = 5,  // 0x05
        SM_CYBORDER = 6,  // 0x06
        SM_CXDLGFRAME = 7,  // 0x07
        SM_CXFIXEDFRAME = 7,  // 0x07
        SM_CYDLGFRAME = 8,  // 0x08
        SM_CYFIXEDFRAME = 8,  // 0x08
        SM_CYVTHUMB = 9,  // 0x09
        SM_CXHTHUMB = 10, // 0x0A
        SM_CXICON = 11, // 0x0B
        SM_CYICON = 12, // 0x0C
        SM_CXCURSOR = 13, // 0x0D
        SM_CYCURSOR = 14, // 0x0E
        SM_CYMENU = 15, // 0x0F
        SM_CXFULLSCREEN = 16, // 0x10
        SM_CYFULLSCREEN = 17, // 0x11
        SM_CYKANJIWINDOW = 18, // 0x12
        SM_MOUSEPRESENT = 19, // 0x13
        SM_CYVSCROLL = 20, // 0x14
        SM_CXHSCROLL = 21, // 0x15
        SM_DEBUG = 22, // 0x16
        SM_SWAPBUTTON = 23, // 0x17
        SM_CXMIN = 28, // 0x1C
        SM_CYMIN = 29, // 0x1D
        SM_CXSIZE = 30, // 0x1E
        SM_CYSIZE = 31, // 0x1F
        SM_CXSIZEFRAME = 32, // 0x20
        SM_CXFRAME = 32, // 0x20
        SM_CYSIZEFRAME = 33, // 0x21
        SM_CYFRAME = 33, // 0x21
        SM_CXMINTRACK = 34, // 0x22
        SM_CYMINTRACK = 35, // 0x23
        SM_CXDOUBLECLK = 36, // 0x24
        SM_CYDOUBLECLK = 37, // 0x25
        SM_CXICONSPACING = 38, // 0x26
        SM_CYICONSPACING = 39, // 0x27
        SM_MENUDROPALIGNMENT = 40, // 0x28
        SM_PENWINDOWS = 41, // 0x29
        SM_DBCSENABLED = 42, // 0x2A
        SM_CMOUSEBUTTONS = 43, // 0x2B
        SM_SECURE = 44, // 0x2C
        SM_CXEDGE = 45, // 0x2D
        SM_CYEDGE = 46, // 0x2E
        SM_CXMINSPACING = 47, // 0x2F
        SM_CYMINSPACING = 48, // 0x30
        SM_CXSMICON = 49, // 0x31
        SM_CYSMICON = 50, // 0x32
        SM_CYSMCAPTION = 51, // 0x33
        SM_CXSMSIZE = 52, // 0x34
        SM_CYSMSIZE = 53, // 0x35
        SM_CXMENUSIZE = 54, // 0x36
        SM_CYMENUSIZE = 55, // 0x37
        SM_ARRANGE = 56, // 0x38
        SM_CXMINIMIZED = 57, // 0x39
        SM_CYMINIMIZED = 58, // 0x3A
        SM_CXMAXTRACK = 59, // 0x3B
        SM_CYMAXTRACK = 60, // 0x3C
        SM_CXMAXIMIZED = 61, // 0x3D
        SM_CYMAXIMIZED = 62, // 0x3E
        SM_NETWORK = 63, // 0x3F
        SM_CLEANBOOT = 67, // 0x43
        SM_CXDRAG = 68, // 0x44
        SM_CYDRAG = 69, // 0x45
        SM_SHOWSOUNDS = 70, // 0x46
        SM_CXMENUCHECK = 71, // 0x47
        SM_CYMENUCHECK = 72, // 0x48
        SM_SLOWMACHINE = 73, // 0x49
        SM_MIDEASTENABLED = 74, // 0x4A
        SM_MOUSEWHEELPRESENT = 75, // 0x4B
        SM_XVIRTUALSCREEN = 76, // 0x4C
        SM_YVIRTUALSCREEN = 77, // 0x4D
        SM_CXVIRTUALSCREEN = 78, // 0x4E
        SM_CYVIRTUALSCREEN = 79, // 0x4F
        SM_CMONITORS = 80, // 0x50
        SM_SAMEDISPLAYFORMAT = 81, // 0x51
        SM_IMMENABLED = 82, // 0x52
        SM_CXFOCUSBORDER = 83, // 0x53
        SM_CYFOCUSBORDER = 84, // 0x54
        SM_TABLETPC = 86, // 0x56
        SM_MEDIACENTER = 87, // 0x57
        SM_STARTER = 88, // 0x58
        SM_SERVERR2 = 89, // 0x59
        SM_MOUSEHORIZONTALWHEELPRESENT = 91, // 0x5B
        SM_CXPADDEDBORDER = 92, // 0x5C
        SM_DIGITIZER = 94, // 0x5E
        SM_MAXIMUMTOUCHES = 95, // 0x5F

        SM_REMOTESESSION = 0x1000, // 0x1000
        SM_SHUTTINGDOWN = 0x2000, // 0x2000
        SM_REMOTECONTROL = 0x2001, // 0x2001


        SM_CONVERTIBLESLATEMODE = 0x2003,
        SM_SYSTEMDOCKED = 0x2004,
    }
    
    //File Dialog
    [Flags]
    public enum FileOpenOptions : int
    {
        OverwritePrompt = 0x00000002,
        StrictFileTypes = 0x00000004,
        NoChangeDirectory = 0x00000008,
        PickFolders = 0x00000020,
        // Ensure that items returned are filesystem items.
        ForceFilesystem = 0x00000040,
        // Allow choosing items that have no storage.
        AllNonStorageItems = 0x00000080,
        NoValidate = 0x00000100,
        AllowMultiSelect = 0x00000200,
        PathMustExist = 0x00000800,
        FileMustExist = 0x00001000,
        CreatePrompt = 0x00002000,
        ShareAware = 0x00004000,
        NoReadOnlyReturn = 0x00008000,
        NoTestFileCreate = 0x00010000,
        HideMruPlaces = 0x00020000,
        HidePinnedPlaces = 0x00040000,
        NoDereferenceLinks = 0x00100000,
        DontAddToRecent = 0x02000000,
        ForceShowHidden = 0x10000000,
        DefaultNoMiniMode = 0x20000000,
        OFN_EXPLORER = 0x00080000, // Old explorer dialog
    }
        
    public delegate IntPtr WndProc(IntPtr hWnd, Int32 msg, IntPtr wParam, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential, CharSet=CharSet.Unicode)]
    public class OpenFileName
    {
        public int      lStructSize = SizeOf();
        public IntPtr   hwndOwner;
        public IntPtr   hInstance;
        public string   lpstrFilter; // separate filters with \0
        public IntPtr   lpstrCustomFilter;
        public int      nMaxCustFilter;
        public int      nFilterIndex;
        //public IntPtr   lpstrFile;
        public string   lpstrFile;
        public int      nMaxFile = NativeWin.MAX_PATH;
        //public IntPtr   lpstrFileTitle;
        public string   lpstrFileTitle;
        public int      nMaxFileTitle = NativeWin.MAX_PATH;
        public string   lpstrInitialDir;
        public string   lpstrTitle;
        public int      Flags;
        public short    nFileOffset;
        public short    nFileExtension;
        public string   lpstrDefExt;
        public IntPtr   lCustData;
        public WndProc  lpfnHook;
        public string   lpTemplateName;
        public IntPtr   pvReserved;
        public int      dwReserved;
        public int      FlagsEx;
            
        [System.Security.SecuritySafeCritical]
        private static int SizeOf() => Marshal.SizeOf(typeof(OpenFileName));
    }
        
    public class DialogOptions
    {
        public int? Flags { get; set; }
        public string Title { get; set; }
        public string Filter { get; set; }
        public string InitialDir { get; set; }
        public string DefaultExt { get; set; }
        public bool IsFolderPicker { get; set; }
    }

    public class DialogResult
    {
        public string File { get; set; }
        public string FileTitle { get; set; }
        public bool Ok { get; set; }
    }
    
    [Flags]
    public enum KnownFolderFlags : uint
    {
        SimpleIDList              = 0x00000100,
        NotParentRelative         = 0x00000200,
        DefaultPath               = 0x00000400,
        Init                      = 0x00000800,
        NoAlias                   = 0x00001000,
        DontUnexpand              = 0x00002000,
        DontVerify                = 0x00004000,
        Create                    = 0x00008000,
        NoAppcontainerRedirection = 0x00010000,
        AliasOnly                 = 0x80000000
    }
    
    public static class KnownFolders
    {
        public static Guid Contacts = new Guid("{56784854-C6CB-462B-8169-88E350ACB882}");
        public static Guid Desktop = new Guid("{B4BFCC3A-DB2C-424C-B029-7FE99A87C641}");
        public static Guid Documents = new Guid("{FDD39AD0-238F-46AF-ADB4-6C85480369C7}");
        public static Guid Downloads = new Guid("{374DE290-123F-4565-9164-39C4925E467B}");
        public static Guid Favorites = new Guid("{1777F761-68AD-4D8A-87BD-30B759FA33DD}");
        public static Guid Links = new Guid("{BFB9D5E0-C6A9-404C-B2B2-AE6DB6AF4968}");
        public static Guid Music = new Guid("{4BD8D571-6D19-48D3-BE97-422220080E43}");
        public static Guid Pictures = new Guid("{33E28130-4E1E-4676-835A-98395C3BC3BB}");
        public static Guid SavedGames = new Guid("{4C5C32FF-BB9D-43B0-B5B4-2D72E54EAAA4}");
        public static Guid SavedSearches = new Guid("{7D1D3A04-DEBB-4115-95CF-2F29DA2920DA}");
        public static Guid Videos = new Guid("{18989B1D-99B5-455B-841C-AB7C74E4DDFC}");
        
        static Dictionary<string, Guid> Map { get; } = new Dictionary<string, Guid> {
            { nameof(Contacts), Contacts },
            { nameof(Desktop), Desktop },
            { nameof(Documents), Documents },
            { nameof(Downloads), Downloads },
            { nameof(Favorites), Favorites },
            { nameof(Links), Links },
            { nameof(Music), Music },
            { nameof(Pictures), Pictures },
            { nameof(SavedGames), SavedGames },
            { nameof(SavedSearches), SavedSearches },
            { nameof(Videos), Videos },
        };

        public static string GetPath(string knownFolder,
            KnownFolderFlags flags = KnownFolderFlags.DontVerify, bool defaultUser = false) =>
            Map.TryGetValue(knownFolder, out var knownFolderId)
                ? GetPath(knownFolderId, flags, defaultUser)
                : ThrowUnknownFolder();
        
        public static string GetPath(Guid knownFolderId, 
            KnownFolderFlags flags=KnownFolderFlags.DontVerify, bool defaultUser=false)
        {
            if (SHGetKnownFolderPath(knownFolderId, (uint)flags, new IntPtr(defaultUser ? -1 : 0), out var outPath) >= 0)
            {
                string path = Marshal.PtrToStringUni(outPath);
                Marshal.FreeCoTaskMem(outPath);
                return path;
            }
            return ThrowUnknownFolder();
        }
        
        //[DoesNotReturn]
        static string ThrowUnknownFolder() => 
            throw new NotSupportedException("Unable to retrieve the path for known folder. It may not be available on this system.");
        
        [DllImport("Shell32.dll")]
        private static extern int SHGetKnownFolderPath(
            [MarshalAs(UnmanagedType.LPStruct)]Guid rfid, uint dwFlags, IntPtr hToken, out IntPtr ppszPath);
    }

    public static partial class NativeWin
    {
        public const int MAX_PATH = 260;
        
        public const int CCHDEVICENAME = 32; // size of a device name string
        public const int MONITOR_DEFAULTTONULL = 0;
        public const int MONITOR_DEFAULTTOPRIMARY = 1;
        public const int MONITOR_DEFAULTTONEAREST = 2;
        
        public const int VERTRES = 10;
        public const int DESKTOPVERTRES = 117;
        public const int LOGPIXELSX = 88;
        public const int LOGPIXELSY = 90;

        public const int SB_HORZ = 0;
        public const int SB_VERT = 1;
        public const int SB_CTL = 2;
        public const int SB_BOTH = 3;

        public const int LWA_ALPHA = 0x2;
        public const int LWA_COLORKEY = 0x1;

        public static int ScreenX => GetSystemMetrics(SystemMetric.SM_CXSCREEN);
        public static int ScreenY => GetSystemMetrics(SystemMetric.SM_CYSCREEN);
        
        public const uint cfUnicodeText = 13;
        
        public const string LibUser = "user32.dll";
        public const string LibCommonDlg = "comdlg32.dll";
        public const string LibKernel = "kernel32.dll";
        public const string LibGdi = "gdi32.dll";
        
        //Message Box
        [DllImport(LibUser, SetLastError = true, CharSet= CharSet.Auto)]
        public static extern int MessageBox(int hWnd, string text, string caption, uint type);        
        
        //Window Operations
        [DllImport(LibUser, CharSet = CharSet.Auto)]
        public static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport(LibUser, CharSet = CharSet.Auto)]
        public static extern int GetWindowTextLength(IntPtr hWnd);
        [DllImport(LibUser, CharSet = CharSet.Auto)]
        public static extern bool SetWindowText(IntPtr hwnd, string lpString);
        
        [DllImport(LibUser, ExactSpelling = true)]
        public static extern bool ShowWindow(this IntPtr hWnd, ShowWindowCommands nCmdShow);

        [DllImport(LibUser, ExactSpelling = true)]
        public static extern bool IsWindowVisible(this IntPtr hWnd);

        [DllImport(LibUser, ExactSpelling = true)]
        public static extern bool IsWindowEnabled(this IntPtr hWnd);
        
        [DllImport(LibUser)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetForegroundWindow(this IntPtr hWnd);
        
        [DllImport(LibCommonDlg, SetLastError=true, CharSet = CharSet.Auto)]
        public static extern bool GetOpenFileName([In, Out] OpenFileName ofn);
        
        //Clipboard from: https://github.com/SimonCropp/TextCopy/blob/master/src/TextCopy/WindowsClipboard.cs
        [DllImport(LibUser, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool IsClipboardFormatAvailable(uint format);

        [DllImport(LibUser, ExactSpelling = true)]
        public static extern IntPtr SetFocus(this IntPtr hWnd);
        
        [DllImport(LibUser, SetLastError = true)]
        public static extern IntPtr GetClipboardData(uint uFormat);

        [DllImport(LibKernel, SetLastError = true)]
        public static extern IntPtr GlobalLock(IntPtr hMem);

        [DllImport(LibKernel, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GlobalUnlock(IntPtr hMem);

        [DllImport(LibUser, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool OpenClipboard(IntPtr hWndNewOwner);

        [DllImport(LibUser, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CloseClipboard();

        [DllImport(LibUser, SetLastError = true)]
        public static extern IntPtr SetClipboardData(uint uFormat, IntPtr data);

        [DllImport(LibUser)]
        public static extern bool EmptyClipboard();

        [DllImport(LibKernel, SetLastError = true)]
        public static extern int GlobalSize(IntPtr hMem);
        
        //Window
        [DllImport(LibUser, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetWindowPlacement(this IntPtr hWnd, ref WindowPlacement winpl);
        
        [DllImport(LibUser)]
        public static extern IntPtr MonitorFromWindow(this IntPtr hWnd, uint dwFlags);
        
        [DllImport(LibUser, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetWindowPlacement(this IntPtr hWnd, [In] ref WindowPlacement winpl);
        
        [DllImport(LibUser, SetLastError = false)]
        public static extern IntPtr GetDesktopWindow();
    
        [DllImport(LibUser, CharSet = CharSet.Auto)]
        public static extern bool GetMonitorInfo(IntPtr hMonitor, ref MonitorInfo mi);
        
        public static IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex)
        {
            if (IntPtr.Size == 8)
                return GetWindowLongPtr64(hWnd, nIndex);
            else
                return GetWindowLongPtr32(hWnd, nIndex);
        }
        [DllImport(LibUser, EntryPoint="GetWindowLong")]
        public static extern IntPtr GetWindowLongPtr32(IntPtr hWnd, int nIndex);
        [DllImport(LibUser, EntryPoint="GetWindowLongPtr")]
        public static extern IntPtr GetWindowLongPtr64(IntPtr hWnd, int nIndex);
        
        public static IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong)
        {
            return IntPtr.Size == 8 
                ? SetWindowLongPtr64(hWnd, nIndex, dwNewLong) 
                : new IntPtr(SetWindowLong32(hWnd, nIndex, dwNewLong.ToInt32()));
        }
        [DllImport(LibUser, EntryPoint="SetWindowLong")]
        public static extern int SetWindowLong32(IntPtr hWnd, int nIndex, int dwNewLong);
        [DllImport(LibUser, EntryPoint="SetWindowLongPtr")]
        public static extern IntPtr SetWindowLongPtr64(IntPtr hWnd, int nIndex, IntPtr dwNewLong);
        
        [DllImport(LibUser, SetLastError = true)]
        public static extern int GetWindowLongA(IntPtr hWnd, int nIndex);
        [DllImport(LibUser, SetLastError = true)]
        public static extern int SetWindowLongA(IntPtr hWnd, int nIndex, int dwNewLong);
        
        [DllImport(LibUser)]
        public static extern bool SetLayeredWindowAttributes(IntPtr hWnd, uint crKey, byte bAlpha, uint dwFlags);        
        
        [DllImport(LibUser)]
        public static extern int GetSystemMetrics(SystemMetric smIndex);

        [DllImport(LibGdi)]
        public static extern int GetDeviceCaps(IntPtr hdc, int nIndex);

        [DllImport(LibUser)]
        public static extern IntPtr GetDC(IntPtr hWnd);

        [DllImport(LibUser)]
        public static extern bool ReleaseDC(IntPtr hWnd, IntPtr hDC);
        
        [DllImport(LibUser)]
        public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport(LibUser)]
        public static extern bool ShowWindow(this IntPtr hWnd, int nCmdShow);
        
        [DllImport(LibUser, SetLastError=true)]
        public static extern int CloseWindow (this IntPtr hWnd);        
        
        [DllImport(LibUser)]
        public static extern bool DestroyWindow(this IntPtr hWnd);        

        [DllImport(LibUser)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetWindowPos(this IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, SetWindowPosFlags uFlags);

        [DllImport(LibUser, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetWindowRect(this IntPtr hWnd, out Rectangle lpRect);
        
        [DllImport(LibUser)]
        private static extern IntPtr GetForegroundWindow();
        
        [DllImport(LibUser, SetLastError = true)]
        public static extern bool MoveWindow(this IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);
        
        [DllImport(LibUser)]
        public static extern bool GetClientRect(this IntPtr hWnd, out Rectangle lpRect);
        
        [DllImport(LibUser)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool ShowScrollBar(this IntPtr hWnd, int wBar, [MarshalAs(UnmanagedType.Bool)] bool bShow);
    }
}