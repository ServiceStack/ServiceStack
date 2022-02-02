using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using WinApi.User32;
using WinApi.Windows;

namespace ServiceStack.CefGlue.Win64
{
    internal class WinapiHostFactory : WindowFactory
    {
        public WinapiHostFactory(string name, WindowClassStyles styles, IntPtr hInstance, IntPtr hIcon, IntPtr hCursor, IntPtr hBgBrush, WindowProc wndProc)
            : base(name, styles, hInstance, hIcon, hCursor, hBgBrush, wndProc) { }

        public static WindowFactory Init(string iconFullPath = null)
        {
            IntPtr? hIcon = LoadIconFromFile(iconFullPath);
            return Create(null, WindowClassStyles.CS_VREDRAW | WindowClassStyles.CS_HREDRAW, null, hIcon, null, null);
        }

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern IntPtr LoadImage(IntPtr hinst, string lpszName, uint uType, int cxDesired, int cyDesired, uint fuLoad);

        public static IntPtr? LoadIconFromFile(string iconFullPath)
        {
            if (string.IsNullOrEmpty(iconFullPath))
            {
                return null;
            }

            if (!File.Exists(iconFullPath))
            {
                return null;
            }

            return LoadImage(                                       // returns a HANDLE so we have to cast to HICON
                IntPtr.Zero,                                        // hInstance must be NULL when loading from a file
                iconFullPath,                                       // the icon file name
                (uint)ResourceImageType.IMAGE_ICON,                 // specifies that the file is an icon
                0,                                                  // width of the image (we'll specify default later on)
                0,                                                  // height of the image
                (uint)LoadResourceFlags.LR_LOADFROMFILE |           // we want to load a file (as opposed to a resource)
                (uint)LoadResourceFlags.LR_DEFAULTSIZE |            // default metrics based on the type (IMAGE_ICON, 32x32)
                (uint)LoadResourceFlags.LR_SHARED                   // let the system release the handle when it's no longer used
            );
        }
    }
}