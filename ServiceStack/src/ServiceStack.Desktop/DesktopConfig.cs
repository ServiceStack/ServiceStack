using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using ServiceStack.Script;
using ServiceStack.Web;

namespace ServiceStack.Desktop
{
    public class DesktopConfig
    {
        public static DesktopConfig Instance { get; } = new();
        public List<ProxyConfig> ProxyConfigs { get; set; } = new();
        public string AppName { get; set; }
        public List<string> ImportParams { get; set; } = new();
        public string MinToolVersion { get; set; }
        public Action OnExit { get; set; }
        public Action<Exception> OnError { get; set; }

        public static Func<ScriptScopeContext, IntPtr> WindowFactory { get; set; } = RequestWindowFactory;
        
        public static IntPtr RequestWindowFactory(ScriptScopeContext scope) 
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
        }
    }

    public class ProxyConfig
    {
        public string Scheme { get; set; }
        public string TargetScheme { get; set; }
        public string Domain { get; set; }
        public bool AllowCors { get; set; }
        public List<string> IgnoreHeaders { get; set; } = new List<string>();
        public Dictionary<string,string> AddHeaders { get; set; } = new Dictionary<string, string>();
        public Action<NameValueCollection> OnResponseHeaders { get; set; }
    }

    public static class DesktopState
    {
        public static bool AppDebug { get; set; }
        public static IntPtr BrowserHandle { get; set; } 
        public static IntPtr ConsoleHandle { get; set; } 
        public static string Tool { get; set; }
        public static string ToolVersion { get; set; }
        public static string ChromeVersion { get; set; }
        public static bool FromScheme { get; set; }
        public static string StartUrl { get; set; }
        public static string[] OriginalCommandArgs { get; set; }
        public static string[] CommandArgs { get; set; }
    }
}