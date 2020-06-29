using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using PInvoke;
using ServiceStack.Text.Pools;
using Win32Exception = System.ComponentModel.Win32Exception;

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

        public static Dictionary<string, object> ToObject(RECT rect) => new Dictionary<string, object> {
            ["top"] = rect.top,
            ["left"] = rect.left,
            ["bottom"] = rect.bottom,
            ["right"] = rect.right,
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

        public static string ExpandEnvVars(string path) => string.IsNullOrEmpty(path) || path.IndexOf('%') == -1
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

        public static DialogResult OpenFolder(this IntPtr hWnd, Dictionary<string, object> options)
        {
            var flags = (int) BrowseInfos.NewDialogStyle;

            var initialDir = options.TryGetValue("initialDir", out var oInitialDir)
                ? ExpandEnvVars(oInitialDir as string)
                : Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

            var dlgArgs = new BROWSEINFO {
                pidlRoot = IntPtr.Zero,
                hwndOwner = hWnd,
                pszDisplayName = new string(new char[256]),
                lpszTitle = options.TryGetValue("title", out var oTitle)
                    ? oTitle as string
                    : "Select a Folder",
                ulFlags = flags,
                lParam = IntPtr.Zero,
                iImage = 0,
                lpfn = (hWnd, msg, lParam, data) => {
                    switch (msg)
                    {
                        case 1: //BFFM_INITIALIZED
                            const int BFFM_SETSELECTIONW = 0x400 + 103;
                            SendMessage(hWnd, BFFM_SETSELECTIONW, 1, initialDir);
                            break;
                        case 2: //BFFM_SELCHANGED
                            // Indicates the selection has changed. The lpData parameter points to the item identifier list for the newly selected item.
                            IntPtr selectedPidl = lParam;
                            if (selectedPidl != IntPtr.Zero)
                            {
                                const int BFFM_ENABLEOK = 0x400 + 101;
                                IntPtr pszSelectedPath =
                                    Marshal.AllocHGlobal((MAX_PATH + 1) * Marshal.SystemDefaultCharSize);
                                // Try to retrieve the path from the IDList
                                bool isFileSystemFolder = SHGetPathFromIDListLongPath(selectedPidl, ref pszSelectedPath);
                                Marshal.FreeHGlobal(pszSelectedPath);
                                SendMessage(hWnd, BFFM_ENABLEOK, 0, isFileSystemFolder ? 1 : 0);
                            }

                            break;
                    }

                    return 0;
                },
            };

            var pidlRet = SHBrowseForFolder(dlgArgs);
            if (pidlRet != IntPtr.Zero)
            {
                var filePath = Shell32.SHGetPathFromIDList(pidlRet);
                return new DialogResult {
                    File = filePath,
                    Ok = true,
                };
            }

            return new DialogResult();
        }

        public static void CenterToScreen(this IntPtr hWnd, bool useWorkArea = true)
        {
            if (GetNearestMonitorInfo(hWnd, out var mi))
            {
                var rectangle = useWorkArea ? mi.WorkArea : mi.Monitor;
                var num1 = rectangle.Width() / 2;
                var num2 = rectangle.Height() / 2;
                var windowSize = GetWindowSize(hWnd);
                SetPosition(hWnd, num1 - windowSize.Width / 2, num2 - windowSize.Height / 2);
            }
        }
        
        public static bool SetSize(this IntPtr hWnd, int width, int height)
        {
            if (hWnd == IntPtr.Zero) return default;
            return User32.SetWindowPos(hWnd, IntPtr.Zero, -1, -1, width, height, 
                User32.SetWindowPosFlags.SWP_NOACTIVATE | User32.SetWindowPosFlags.SWP_NOMOVE | User32.SetWindowPosFlags.SWP_NOZORDER);
        }

        public static bool SetPosition(this IntPtr hWnd, int x, int y)
        {
            if (hWnd == IntPtr.Zero) return default;
            return User32.SetWindowPos(hWnd, IntPtr.Zero, x, y, -1, -1, 
                User32.SetWindowPosFlags.SWP_NOACTIVATE | User32.SetWindowPosFlags.SWP_NOSIZE | User32.SetWindowPosFlags.SWP_NOZORDER);
        }

        public static bool SetPosition(this IntPtr hWnd, int x, int y, int width, int height)
        {
            if (hWnd == IntPtr.Zero) return default;
            return User32.SetWindowPos(hWnd, IntPtr.Zero, x, y, width, height, 
                User32.SetWindowPosFlags.SWP_NOACTIVATE | User32.SetWindowPosFlags.SWP_NOZORDER);
        }

        public static bool SetPosition(this IntPtr hWnd, int x, int y, int width, int height, User32.SetWindowPosFlags flags)
        {
            if (hWnd == IntPtr.Zero) return default;
            return User32.SetWindowPos(hWnd, IntPtr.Zero, x, y, width, height, flags);
        }
        public static bool SetPosition(this IntPtr hWnd, ref RECT rect) => SetPosition(hWnd, 
            rect.left, rect.top, rect.Width(), rect.Height(), User32.SetWindowPosFlags.SWP_NOACTIVATE | User32.SetWindowPosFlags.SWP_NOZORDER);
        public static bool SetPosition(this IntPtr hWnd, RECT rect) => SetPosition(hWnd, ref rect);
        public static bool SetPosition(this IntPtr hWnd, RECT rect, User32.SetWindowPosFlags flags) => SetPosition(hWnd, ref rect, flags);
        public static bool SetPosition(this IntPtr hWnd, ref RECT rect, User32.SetWindowPosFlags flags) => 
            SetPosition(hWnd, rect.left, rect.top, rect.Width(), rect.Height(), flags);

        public static void RedrawFrame(this IntPtr hWnd) => SetPosition(hWnd, new RECT(),
            User32.SetWindowPosFlags.SWP_FRAMECHANGED | User32.SetWindowPosFlags.SWP_NOMOVE | User32.SetWindowPosFlags.SWP_NOSIZE);

        public static float GetScalingFactor(this User32.SafeDCHandle hdc)
        {
            int logicalScreenHeight = Gdi32.GetDeviceCaps(hdc, Gdi32.DeviceCap.VERTRES);
            int physicalScreenHeight = Gdi32.GetDeviceCaps(hdc, Gdi32.DeviceCap.DESKTOPVERTRES);
            return physicalScreenHeight / (float) logicalScreenHeight;
        }

        public static System.Drawing.Size GetScreenResolution()
        {
            using var hdc = User32.GetDC(IntPtr.Zero);
            var scalingFactor = GetScalingFactor(hdc);
            return new System.Drawing.Size(
                (int) (User32.GetSystemMetrics(User32.SystemMetric.SM_CXSCREEN) * scalingFactor),
                (int) (User32.GetSystemMetrics(User32.SystemMetric.SM_CYSCREEN) * scalingFactor)
            );
        }

        public static MonitorInfo? SetKioskMode(this IntPtr hWnd)
        {
            if (hWnd == IntPtr.Zero) return null;
            //https://devblogs.microsoft.com/oldnewthing/20100412-00/?p=14353
            var dwStyle = GetWindowLongPtr(hWnd, (int) User32.WindowLongIndexFlags.GWL_STYLE);
            if (GetPrimaryMonitorInfo(hWnd, out var mi))
            {
                SetWindowLongPtr(hWnd, (int) User32.WindowLongIndexFlags.GWL_STYLE,
                    new IntPtr((uint) dwStyle & (uint)~User32.WindowStyles.WS_OVERLAPPEDWINDOW));

                var mr = mi.Monitor;
                User32.SetWindowPos(hWnd, User32.SpecialWindowHandles.HWND_TOPMOST,
                    mr.left, mr.top,
                    mr.right - mr.left,
                    mr.bottom - mr.top,
                    User32.SetWindowPosFlags.SWP_NOZORDER | User32.SetWindowPosFlags.SWP_FRAMECHANGED);
                return mi;
            }
            return null;
        }
        
        public static void SetWindowFullScreen(this IntPtr hWnd)
        {
            if (hWnd == IntPtr.Zero) return;
            var res = GetScreenResolution();
            User32.SetWindowPos(hWnd, IntPtr.Zero,
                0, 0,
                res.Width,
                res.Height,
                User32.SetWindowPosFlags.SWP_SHOWWINDOW);
        }

        public static void ResizeWindow(this IntPtr hWnd, int width, int height)
        {
            if (hWnd == IntPtr.Zero) return;
            User32.SetWindowPos(hWnd, IntPtr.Zero,
                0, 0, width, height,
                User32.SetWindowPosFlags.SWP_NOZORDER
            );
        }
        
        public static RECT GetClientRect(this IntPtr hWnd)
        {
            if (hWnd == IntPtr.Zero) return default;
            User32.GetClientRect(hWnd, out var lpRect);
            return lpRect;
        }

        public static System.Drawing.Size GetClientSize(this IntPtr hWnd)
        {
            if (hWnd == IntPtr.Zero) return default;
            User32.GetClientRect(hWnd, out var rectangle);
            return new System.Drawing.Size
            {
                Width = rectangle.Width(),
                Height = rectangle.Height()
            };
        }

        public static System.Drawing.Size GetWindowSize(this IntPtr hWnd)
        {
            if (hWnd == IntPtr.Zero) return default;
            User32.GetWindowRect(hWnd, out var rectangle);
            return new System.Drawing.Size
            {
                Width = rectangle.Width(),
                Height = rectangle.Height()
            };
        }

        public static bool GetPrimaryMonitorInfo(this IntPtr hWnd, out MonitorInfo monitorInfo)
        {
            if (hWnd == IntPtr.Zero) { monitorInfo = default; return default; }
            var mi = new MonitorInfo();
            mi.Size = Marshal.SizeOf(mi);
            var ret = GetMonitorInfo(MonitorFromWindow(hWnd, User32.MonitorOptions.MONITOR_DEFAULTTOPRIMARY), ref mi);
            monitorInfo = mi;
            return ret;
        }

        public static bool GetNearestMonitorInfo(this IntPtr hWnd, out MonitorInfo monitorInfo)
        {
            if (hWnd == IntPtr.Zero) { monitorInfo = default; return default; }
            var mi = new MonitorInfo();
            mi.Size = Marshal.SizeOf(mi);
            var ret = GetMonitorInfo(MonitorFromWindow(hWnd, User32.MonitorOptions.MONITOR_DEFAULTTONEAREST), ref mi);
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
            return User32.SetWindowText(hWnd, text);
        }

        public static string GetText(this IntPtr hWnd)
        {
            if (hWnd == IntPtr.Zero) return default;
            var size = User32.GetWindowTextLength(hWnd);
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
                if (User32.OpenClipboard(default))
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

                pointer = Kernel32.GlobalLock(handle);
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
                    Kernel32.GlobalUnlock(handle);
                User32.CloseClipboard();
            }
        }

        public static bool SetStringInClipboard(string text)
        {
            TryOpenClipboard();
            
            IntPtr hGlobal = default;
            try
            {
                User32.EmptyClipboard();

                var bytes = (text.Length + 1) * 2;
                hGlobal = Marshal.AllocHGlobal(bytes);

                if (hGlobal == default)
                    ThrowWin32();

                var target = Kernel32.GlobalLock(hGlobal);

                if (target == default)
                    ThrowWin32();

                try
                {
                    Marshal.Copy(text.ToCharArray(), 0, target, text.Length);
                }
                finally
                {
                    Kernel32.GlobalUnlock(target);
                }

                if (SetClipboardData(cfUnicodeText, hGlobal) == default)
                    ThrowWin32();

                hGlobal = default;
            }
            finally
            {
                if (hGlobal != default)
                    Marshal.FreeHGlobal(hGlobal);

                User32.CloseClipboard();
            }
            return true;
        }

        static void ThrowWin32() => throw new Win32Exception(Marshal.GetLastWin32Error());

        public static void ApplyTransparency(IntPtr hWnd, byte transparency)
        {
            const int GWL_EXSTYLE = (int) User32.WindowLongIndexFlags.GWL_EXSTYLE;
            const int WS_EX_LAYERED = (int)User32.WindowStylesEx.WS_EX_LAYERED;
            SetWindowLongPtr(hWnd, GWL_EXSTYLE, new IntPtr(GetWindowLongPtr(hWnd, GWL_EXSTYLE).ToInt32() | WS_EX_LAYERED));
            SetLayeredWindowAttributes(hWnd, 0, transparency, LWA_ALPHA);
        }
    }

    public static class RectEx
    {
        public static int Top(this RECT rect) => rect.top;
        public static int Bottom(this RECT rect) => rect.bottom;
        public static int Left(this RECT rect) => rect.left;
        public static int Right(this RECT rect) => rect.right;
        public static int Width(this RECT rect) => rect.right - rect.left;
        public static int Height(this RECT rect) => rect.bottom - rect.top;
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

    public class DialogResult
    {
        public string File { get; set; }
        public string FileTitle { get; set; }
        public bool Ok { get; set; }
    }
    
    [Flags]
    public enum BrowseInfos
    {
        NewDialogStyle      = 0x0040,   // Use the new dialog layout with the ability to resize
        HideNewFolderButton = 0x0200    // Don't display the 'New Folder' button
    }
 
    [StructLayout(LayoutKind.Sequential, CharSet=CharSet.Auto)]
    public class BROWSEINFO 
    {
        public IntPtr hwndOwner;
        public IntPtr pidlRoot; //LPCITEMIDLIST pidlRoot; // Root ITEMIDLIST
    
        // For interop purposes, send over a buffer of MAX_PATH size. 
        public string pszDisplayName; //LPWSTR pszDisplayName; // Return display name of item selected.
    
        public string lpszTitle; //LPCWSTR lpszTitle; // text to go in the banner over the tree.
        public int ulFlags; //UINT ulFlags; // Flags that control the return stuff
        public BrowseCallbackProc lpfn; //BFFCALLBACK lpfn; // Call back pointer
        public IntPtr lParam; //LPARAM lParam; // extra info that's passed back in callbacks
        public int iImage; //int iImage; // output var: where to return the Image index.
    }
    
    public delegate int BrowseCallbackProc(IntPtr hwnd, int msg, IntPtr lParam, IntPtr lpData);
    
    // Using User32.MONITORINFO doesn't materialize properly
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    public struct MonitorInfo
    {
        public const int CCHDEVICENAME = 32; // size of a device name string

        public int Size;
        public RECT Monitor;
        public RECT WorkArea;
        public uint Flags;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = CCHDEVICENAME)]
        public string DeviceName;
        public void Init()
        {
            this.Size = 40 + 2 * CCHDEVICENAME;
            this.DeviceName = string.Empty;
        }
    }    
    
    public static class KnownFolders
    {
        public static Guid Contacts => Shell32.KNOWNFOLDERID.FOLDERID_Contacts;
        public static Guid Desktop => Shell32.KNOWNFOLDERID.FOLDERID_Desktop;
        public static Guid Documents => Shell32.KNOWNFOLDERID.FOLDERID_Documents;
        public static Guid Downloads => Shell32.KNOWNFOLDERID.FOLDERID_Downloads;
        public static Guid Favorites => Shell32.KNOWNFOLDERID.FOLDERID_Favorites;
        public static Guid Links => Shell32.KNOWNFOLDERID.FOLDERID_Links;
        public static Guid Music => Shell32.KNOWNFOLDERID.FOLDERID_Music;
        public static Guid Pictures => Shell32.KNOWNFOLDERID.FOLDERID_Pictures;
        public static Guid SavedGames => Shell32.KNOWNFOLDERID.FOLDERID_SavedGames;
        public static Guid SavedSearches => Shell32.KNOWNFOLDERID.FOLDERID_SavedSearches;
        public static Guid Videos => Shell32.KNOWNFOLDERID.FOLDERID_Videos;
        
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
            Shell32.KNOWN_FOLDER_FLAG flags = Shell32.KNOWN_FOLDER_FLAG.KF_FLAG_DONT_VERIFY, bool defaultUser = false) =>
            Map.TryGetValue(knownFolder, out var knownFolderId)
                ? GetPath(knownFolderId, flags, defaultUser)
                : ThrowUnknownFolder();
        
        public static string GetPath(Guid knownFolderId, 
            Shell32.KNOWN_FOLDER_FLAG flags=Shell32.KNOWN_FOLDER_FLAG.KF_FLAG_DONT_VERIFY, bool defaultUser=false)
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
        public const int MAX_UNICODESTRING_LEN = short.MaxValue;

        public const int SB_HORZ = 0;
        public const int SB_VERT = 1;
        public const int SB_CTL = 2;
        public const int SB_BOTH = 3;

        public const int LWA_ALPHA = 0x2;
        public const int LWA_COLORKEY = 0x1;

        public static int ScreenX => User32.GetSystemMetrics(User32.SystemMetric.SM_CXSCREEN);
        public static int ScreenY => User32.GetSystemMetrics(User32.SystemMetric.SM_CYSCREEN);
        
        public const uint cfUnicodeText = 13;
        
        public const string LibUser = "user32.dll";
        public const string LibCommonDlg = "comdlg32.dll";
        public const string LibKernel = "kernel32.dll";
        public const string LibShell = "shell32.dll";
        
        //Message Box
        [DllImport(LibUser, SetLastError = true, CharSet= CharSet.Auto)]
        public static extern int MessageBox(int hWnd, string text, string caption, uint type);        
        
        //Window Operations
        [DllImport(LibUser, CharSet = CharSet.Auto)]
        public static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport(LibUser, ExactSpelling = true)]
        public static extern bool IsWindowEnabled(this IntPtr hWnd);
        
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

        [DllImport(LibUser, SetLastError = true)]
        public static extern IntPtr SetClipboardData(uint uFormat, IntPtr data);
        
        [DllImport(LibKernel, SetLastError = true)]
        public static extern int GlobalSize(IntPtr hMem);
        
        //Window
        
        [DllImport(LibUser)]
        public static extern IntPtr MonitorFromWindow(this IntPtr hWnd, User32.MonitorOptions dwFlags);
    
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
        
        [DllImport(LibUser, SetLastError = true)]
        public static extern bool MoveWindow(this IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);
        
        [DllImport(LibUser)]
        public static extern bool GetClientRect(this IntPtr hWnd, out RECT lpRect);
        
        [DllImport(LibUser)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool ShowScrollBar(this IntPtr hWnd, int wBar, [MarshalAs(UnmanagedType.Bool)] bool bShow);
        
        [DllImport(LibUser, CharSet=CharSet.Auto)]
        public static extern IntPtr SendMessage(IntPtr hWnd, int msg, int wParam, string lParam);
        [DllImport(LibUser, CharSet=CharSet.Auto)]
        public static extern IntPtr SendMessage(IntPtr hWnd, int msg, int wParam, int lParam);        

        [DllImport(LibShell, CharSet=CharSet.Auto)]
        public static extern IntPtr SHBrowseForFolder([In] BROWSEINFO lpbi);
        
        [DllImport(LibShell, CharSet=CharSet.Auto)]
        private static extern bool SHGetPathFromIDListEx(IntPtr pidl, IntPtr pszPath, int cchPath, int flags);
        public static bool SHGetPathFromIDListLongPath(IntPtr pidl, ref IntPtr pszPath)
        {
            int noOfTimes = 1;
            // This is how size was allocated in the calling method.
            int bufferSize = MAX_PATH * Marshal.SystemDefaultCharSize;
            int length = MAX_PATH;
            bool result = false;
 
            // SHGetPathFromIDListEx returns false in case of insufficient buffer.
            // This method does not distinguish between insufficient memory and an error. Until we get a proper solution,
            // this logic would work. In the worst case scenario, loop exits when length reaches unicode string length.
            while ((result = SHGetPathFromIDListEx(pidl, pszPath, length, 0)) == false 
                   && length < MAX_UNICODESTRING_LEN)
            {
                string path = Marshal.PtrToStringAuto(pszPath);
 
                if (path.Length != 0 && path.Length < length)
                    break;
 
                noOfTimes += 2; //520 chars capacity increase in each iteration.
                length = noOfTimes * length >= MAX_UNICODESTRING_LEN 
                    ? MAX_UNICODESTRING_LEN :  length;
                pszPath = Marshal.ReAllocHGlobal(pszPath, (IntPtr)((length + 1) * Marshal.SystemDefaultCharSize));
            }
 
            return result;
        }
    }
}