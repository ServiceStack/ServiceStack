using System;
using ServiceStack.Script;

namespace ServiceStack.Desktop
{
    public static class DesktopUtils
    {
        public static bool DoWindow(this ScriptScopeContext scope, Action<IntPtr> fn)
        {
            var hWnd = DesktopConfig.WindowFactory(scope);
            if (hWnd != IntPtr.Zero)
            {
                fn(hWnd);
                return true;
            }

            return false;
        }

        public static T DoWindow<T>(this ScriptScopeContext scope, Func<IntPtr, T> fn)
        {
            var hWnd = DesktopConfig.WindowFactory(scope);
            return hWnd != IntPtr.Zero ? fn(hWnd) : default;
        }
    }
}