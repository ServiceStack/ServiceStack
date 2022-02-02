using System;
using System.Drawing;
using System.Runtime.InteropServices;

namespace ServiceStack.CefGlue
{
    /// <summary>
    /// Copy of ServiceStack.Desktop used in Cef
    /// </summary>
    internal static class NativeWin
    {
        public const int CCHDEVICENAME = 32; // size of a device name string
        public const int MONITOR_DEFAULTTONEAREST = 2;

        public const string LibUser = "user32.dll";

        [DllImport(LibUser, EntryPoint = "SetWindowLongPtr")]
        public static extern IntPtr SetWindowLongPtr64(this IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        [DllImport(LibUser, ExactSpelling = true)]
        public static extern bool ShowWindow(this IntPtr hWnd, ShowWindowCommands nCmdShow);

        [DllImport(LibUser)]
        public static extern IntPtr MonitorFromWindow(this IntPtr hWnd, uint dwFlags);

        [DllImport(LibUser, CharSet = CharSet.Auto)]
        public static extern bool GetMonitorInfo(IntPtr hMonitor, ref MonitorInfo mi);

        [DllImport(LibUser)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetWindowPos(this IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy,
            SetWindowPosFlags uFlags);

        public static bool SetPosition(this IntPtr hWnd, int x, int y, int width, int height)
        {
            if (hWnd == IntPtr.Zero) return default;
            return SetWindowPos(hWnd, IntPtr.Zero, x, y, width, height,
                SetWindowPosFlags.NoActivate | SetWindowPosFlags.NoZOrder);
        }

        public static bool GetNearestMonitorInfo(this IntPtr hWnd, out MonitorInfo monitorInfo)
        {
            if (hWnd == IntPtr.Zero)
            {
                monitorInfo = default;
                return default;
            }

            var mi = new MonitorInfo();
            mi.Size = Marshal.SizeOf(mi);
            var ret = GetMonitorInfo(MonitorFromWindow(hWnd, MONITOR_DEFAULTTONEAREST), ref mi);
            monitorInfo = mi;
            return ret;
        }
    }
    
    [Flags]
    internal enum WindowStyles
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
    internal enum WindowLongFlags
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
    internal enum ShowWindowCommands
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
}