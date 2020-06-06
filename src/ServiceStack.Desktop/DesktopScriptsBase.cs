using System;
using System.Collections.Generic;
using ServiceStack.Script;
using ServiceStack.Web;

namespace ServiceStack.Desktop
{
    public abstract class DesktopScriptsBase : ScriptMethods
    {
        private readonly Func<ScriptScopeContext, IntPtr> windowFactory;

        protected DesktopScriptsBase(Func<ScriptScopeContext, IntPtr> windowFactory=null) =>
            this.windowFactory = windowFactory ?? DesktopConfig.RequestWindowFactory;

        protected bool DoWindow(ScriptScopeContext scope, Action<IntPtr> fn)
        {
            var hWnd = windowFactory(scope);
            if (hWnd != IntPtr.Zero)
            {
                fn(hWnd);
                return true;
            }

            return false;
        }

        protected T DoWindow<T>(ScriptScopeContext scope, Func<IntPtr, T> fn)
        {
            var hWnd = windowFactory(scope);
            return hWnd != IntPtr.Zero ? fn(hWnd) : default;
        }
    }
}