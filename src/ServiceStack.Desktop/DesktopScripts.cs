using System;
using System.Collections.Generic;
using ServiceStack.Script;
// ReSharper disable InconsistentNaming

namespace ServiceStack.Desktop
{
    public class DesktopScripts : DesktopScriptsBase
    {
        public DesktopScripts(Func<ScriptScopeContext, IntPtr> windowFactory=null) : base(windowFactory) { }
        
        static string resolveUrl(ScriptScopeContext scope, string url)
        {
            var resolvedUrl = scope.ResolveUrl(url);
            return resolvedUrl.IndexOf("://", StringComparison.Ordinal) >= 0
                ? resolvedUrl
                : DesktopState.StartUrl.CombineWith(resolvedUrl);
        }
        public bool openUrl(ScriptScopeContext scope, string url) =>
            DoWindow(scope, w => NativeWin.Open(new Uri(resolveUrl(scope,url)).ToString()));
        public bool start(ScriptScopeContext scope, string cmd) =>
            DoWindow(scope, w => NativeWin.Open(cmd));
        public Dictionary<string, string> desktopInfo(ScriptScopeContext scope) => 
            DoWindow(scope, w => NativeWin.GetDesktopInfo());
        public Dictionary<string, object> deviceScreenResolution(ScriptScopeContext scope) =>
            DoWindow(scope, w => NativeWin.ToObject(NativeWin.GetScreenResolution()));
        public long findWindowByName(ScriptScopeContext scope, string name) =>
            DoWindow(scope, w => (long) NativeWin.FindWindowByName(name));
        public string clipboard(ScriptScopeContext scope) =>
            DoWindow(scope, w => NativeWin.GetClipboardAsString());
        public bool setClipboard(ScriptScopeContext scope, string data) => 
            DoWindow(scope, w => NativeWin.SetStringInClipboard(data));
        public int messageBox(ScriptScopeContext scope, string text, string caption, uint type) => 
            DoWindow(scope, w => NativeWin.MessageBox(0, text, caption, type));
        public string expandEnvVars(ScriptScopeContext scope, string path) => 
            DoWindow(scope, w => NativeWin.ExpandEnvVars(path));
        public string knownFolder(ScriptScopeContext scope, string folderName) => 
            DoWindow(scope, w => KnownFolders.GetPath(folderName));

        public Dictionary<string, object> primaryMonitorInfo(ScriptScopeContext scope) =>
            DoWindow(scope, w => w.GetPrimaryMonitorInfo(out var mi) ? NativeWin.ToObject(mi) : null);
        public bool windowSendToForeground(ScriptScopeContext scope) =>
            DoWindow(scope, w => w.SetForegroundWindow());
        public bool windowCenterToScreen(ScriptScopeContext scope) => 
            DoWindow(scope, w => w.CenterToScreen());
        public bool windowCenterToScreen(ScriptScopeContext scope, bool useWorkArea) => 
            DoWindow(scope, w => w.CenterToScreen(useWorkArea));
        public bool windowSetFullScreen(ScriptScopeContext scope) => 
            DoWindow(scope, w => w.SetWindowFullScreen());
        public bool windowSetFocus(ScriptScopeContext scope) => 
            DoWindow(scope, w => { w.SetFocus(); });
        public bool windowShowScrollBar(ScriptScopeContext scope, bool show) => 
            DoWindow(scope, w => w.ShowScrollBar(show));
        public bool windowSetPosition(ScriptScopeContext scope, int x, int y, int width, int height) =>
            DoWindow(scope, w => w.SetPosition(x,y,width,height));
        public bool windowSetPosition(ScriptScopeContext scope, int x, int y) =>
            DoWindow(scope, w => w.SetPosition(x,y));
        public bool windowSetSize(ScriptScopeContext scope, int width, int height) =>
            DoWindow(scope, w => w.SetSize(width, height));
        public bool windowRedrawFrame(ScriptScopeContext scope) => 
            DoWindow(scope, w => w.RedrawFrame());
        public bool windowIsVisible(ScriptScopeContext scope) => 
            DoWindow(scope, w => w.IsWindowVisible());
        public bool windowIsEnabled(ScriptScopeContext scope) => 
            DoWindow(scope, w => w.IsWindowEnabled());
        public bool windowShow(ScriptScopeContext scope) => 
            DoWindow(scope, w => w.ShowWindow(ShowWindowCommands.Show));
        public bool windowHide(ScriptScopeContext scope) => 
            DoWindow(scope, w => w.ShowWindow(ShowWindowCommands.Hide));
        public string windowText(ScriptScopeContext scope) => 
            DoWindow(scope, w => w.GetText());
        public bool windowSetText(ScriptScopeContext scope, string text) => 
            DoWindow(scope, w => w.SetText(text));
        public bool windowSetState(ScriptScopeContext scope, int state) => 
            DoWindow(scope, w => w.ShowWindow((ShowWindowCommands)state));
        
        public Dictionary<string, object> windowSize(ScriptScopeContext scope) =>
            DoWindow(scope, w => NativeWin.ToObject(w.GetWindowSize()));
        public Dictionary<string, object> windowClientSize(ScriptScopeContext scope) => 
            DoWindow(scope, w => NativeWin.ToObject(w.GetClientSize()));
        public Dictionary<string, object> windowClientRect(ScriptScopeContext scope) => 
            DoWindow(scope, w => NativeWin.ToObject(w.GetClientRect()));

        public DialogResult openFile(ScriptScopeContext scope, Dictionary<string, object> options) => 
            DoWindow(scope, w => w.OpenFile(options));
    }
    
}