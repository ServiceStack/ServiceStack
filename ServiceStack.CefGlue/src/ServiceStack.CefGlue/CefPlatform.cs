using System;
using System.Drawing;
using Xilium.CefGlue;

namespace ServiceStack.CefGlue
{
    public abstract class CefPlatform
    {
        public static CefPlatform Instance { get; protected set; }

        public abstract CefSize GetScreenResolution();

        public abstract void HideConsoleWindow();

        public abstract void ResizeWindow(IntPtr handle, int width, int height);

        public abstract Rectangle GetClientRectangle(IntPtr handle);

        public abstract void SetWindowFullScreen(IntPtr handle);

        public abstract void ShowScrollBar(IntPtr handle, bool show);
    }
}