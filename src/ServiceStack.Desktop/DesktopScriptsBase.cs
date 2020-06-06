using System;
using ServiceStack.Script;

namespace ServiceStack.Desktop
{
    public abstract class DesktopScriptsBase : ScriptMethods
    {
        public Func<ScriptScopeContext, IntPtr> WindowFactory { get; set; } = DesktopConfig.RequestWindowFactory;

        protected bool DoWindow(ScriptScopeContext scope, Action<IntPtr> fn)
        {
            var hWnd = WindowFactory(scope);
            if (hWnd != IntPtr.Zero)
            {
                fn(hWnd);
                return true;
            }

            return false;
        }

        protected T DoWindow<T>(ScriptScopeContext scope, Func<IntPtr, T> fn)
        {
            var hWnd = WindowFactory(scope);
            return hWnd != IntPtr.Zero ? fn(hWnd) : default;
        }
    }
}