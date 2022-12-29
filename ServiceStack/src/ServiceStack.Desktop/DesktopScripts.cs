using System;
using System.Collections.Generic;
using PInvoke;
using ServiceStack.Script;
// ReSharper disable InconsistentNaming

namespace ServiceStack.Desktop
{
    public class DesktopScripts : ScriptMethods
    {
        static string resolveUrl(ScriptScopeContext scope, string url)
        {
            var resolvedUrl = scope.ResolveUrl(url);
            return resolvedUrl.IndexOf("://", StringComparison.Ordinal) >= 0
                ? resolvedUrl
                : DesktopState.StartUrl.CombineWith(resolvedUrl);
        }
        public bool openUrl(ScriptScopeContext scope, string url) =>
            scope.DoWindow(w => NativeWin.Open(new Uri(resolveUrl(scope,url)).ToString()));
        public bool start(ScriptScopeContext scope, string cmd) =>
            scope.DoWindow(w => NativeWin.Open(cmd));
        public Dictionary<string, string> desktopInfo(ScriptScopeContext scope) => 
            scope.DoWindow(w => NativeWin.GetDesktopInfo());
        public Dictionary<string, object> deviceScreenResolution(ScriptScopeContext scope) =>
            scope.DoWindow(w => NativeWin.ToObject(NativeWin.GetScreenResolution()));
        public long findWindowByName(ScriptScopeContext scope, string name) =>
            scope.DoWindow(w => (long) User32.FindWindow(null, name));
        public string clipboard(ScriptScopeContext scope) =>
            scope.DoWindow(w => NativeWin.GetClipboardAsString());
        public bool setClipboard(ScriptScopeContext scope, string data) => 
            scope.DoWindow(w => NativeWin.SetStringInClipboard(data));
        public int messageBox(ScriptScopeContext scope, string text, string caption, uint type) => 
            scope.DoWindow(w => (int)User32.MessageBox(default, text, caption, (User32.MessageBoxOptions)type));
        public string expandEnvVars(ScriptScopeContext scope, string path) => 
            scope.DoWindow(w => NativeWin.ExpandEnvVars(path));
        public string knownFolder(ScriptScopeContext scope, string folderName) => 
            scope.DoWindow(w => KnownFolders.GetPath(folderName));

        public Dictionary<string, object> primaryMonitorInfo(ScriptScopeContext scope) =>
            scope.DoWindow(w => w.GetPrimaryMonitorInfo(out var mi) ? NativeWin.ToObject(mi) : null);
        public bool windowSendToForeground(ScriptScopeContext scope) =>
            scope.DoWindow(User32.SetForegroundWindow);
        public bool windowCenterToScreen(ScriptScopeContext scope) => 
            scope.DoWindow(w => w.CenterToScreen());
        public bool windowCenterToScreen(ScriptScopeContext scope, bool useWorkArea) => 
            scope.DoWindow(w => w.CenterToScreen(useWorkArea));
        public bool windowSetFullScreen(ScriptScopeContext scope) => 
            scope.DoWindow(w => w.SetWindowFullScreen());
        public bool windowSetFocus(ScriptScopeContext scope) => 
            scope.DoWindow(w => { w.SetFocus(); });
        public bool windowShowScrollBar(ScriptScopeContext scope, bool show) => 
            scope.DoWindow(w => w.ShowScrollBar(show));
        public bool windowSetPosition(ScriptScopeContext scope, int x, int y, int width, int height) =>
            scope.DoWindow(w => w.SetPosition(x,y,width,height));
        public bool windowSetPosition(ScriptScopeContext scope, int x, int y) =>
            scope.DoWindow(w => w.SetPosition(x,y));
        public bool windowSetSize(ScriptScopeContext scope, int width, int height) =>
            scope.DoWindow(w => w.SetSize(width, height));
        public bool windowRedrawFrame(ScriptScopeContext scope) => 
            scope.DoWindow(w => w.RedrawFrame());
        public bool windowIsVisible(ScriptScopeContext scope) => 
            scope.DoWindow(User32.IsWindowVisible);
        public bool windowIsEnabled(ScriptScopeContext scope) => 
            scope.DoWindow(w => w.IsWindowEnabled());
        public bool windowShow(ScriptScopeContext scope) => 
            scope.DoWindow(w => User32.ShowWindow(w, User32.WindowShowStyle.SW_SHOW));
        public bool windowHide(ScriptScopeContext scope) => 
            scope.DoWindow(w => User32.ShowWindow(w, User32.WindowShowStyle.SW_HIDE));
        public string windowText(ScriptScopeContext scope) => 
            scope.DoWindow(w => w.GetText());
        public bool windowSetText(ScriptScopeContext scope, string text) => 
            scope.DoWindow(w => w.SetText(text));
        public bool windowSetState(ScriptScopeContext scope, int state) =>
            scope.DoWindow(w => User32.ShowWindow(w, (User32.WindowShowStyle) state));
        
        public Dictionary<string, object> windowSize(ScriptScopeContext scope) =>
            scope.DoWindow(w => NativeWin.ToObject(w.GetWindowSize()));
        public Dictionary<string, object> windowClientSize(ScriptScopeContext scope) => 
            scope.DoWindow(w => NativeWin.ToObject(w.GetClientSize()));
        public Dictionary<string, object> windowClientRect(ScriptScopeContext scope) => 
            scope.DoWindow(w => NativeWin.ToObject(w.GetClientRect()));

        public DialogResult openFile(ScriptScopeContext scope, Dictionary<string, object> options) => 
            scope.DoWindow(w => w.OpenFile(options));
        public DialogResult openFolder(ScriptScopeContext scope, Dictionary<string, object> options) => 
            scope.DoWindow(w => w.OpenFolder(options));
    }
    
}