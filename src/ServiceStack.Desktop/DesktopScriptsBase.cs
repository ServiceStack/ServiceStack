using System;
using System.Collections.Generic;
using ServiceStack.Script;
using ServiceStack.Web;

namespace ServiceStack.Desktop
{
    public abstract class DesktopScriptsBase : ScriptMethods
    {
        public static Func<ScriptScopeContext, IntPtr> WindowFactory => scope => 
        {
            if (scope.TryGetValue(ScriptConstants.Request, out var oRequest) && oRequest is IRequest req)
            {
                var info = req.GetHeader("X-Desktop-Info");
                if (info != null)
                    NativeWin.SetDesktopInfo(info.FromJsv<Dictionary<string, string>>());
                var handle = req.GetHeader("X-Window-Handle");
                if (handle != null && long.TryParse(handle, out var lHandle))
                    return (IntPtr)lHandle;
            }
            return IntPtr.Zero;
        };

        private readonly Func<ScriptScopeContext, IntPtr> windowFactory;

        protected DesktopScriptsBase(Func<ScriptScopeContext, IntPtr> windowFactory=null) =>
            this.windowFactory = windowFactory ?? WindowFactory;

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