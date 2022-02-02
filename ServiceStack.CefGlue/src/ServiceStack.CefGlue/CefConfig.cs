using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Reflection;
using Xilium.CefGlue;

namespace ServiceStack.CefGlue
{
    public class CefConfig
    {
        public CefConfig(bool debug=false) : this(Path.GetDirectoryName(new Uri(Assembly.GetExecutingAssembly().CodeBase).LocalPath), debug) {}
        
        public CefConfig(string cefPath, bool debug=false)
        {
            CefPath = cefPath;
            var processPath = File.Exists(Path.Combine(cefPath, "cefclient.exe"))
                ? Path.Combine(cefPath, "cefclient.exe")
                : File.Exists(Path.Combine(cefPath, "webapp.exe"))
                    ? Path.Combine(cefPath, "webapp.exe")
                    : null;
            CefSettings = new CefSettings
            {
                LocalesDirPath = Path.Combine(cefPath, "locales"),
                Locale = "en-US",
                LogFile = debug ? "cef.log" : null,
                MultiThreadedMessageLoop = CefRuntime.Platform == CefRuntimePlatform.Windows,
                LogSeverity = CefLogSeverity.Info,
                ResourcesDirPath = cefPath,
                BrowserSubprocessPath = processPath,
                RemoteDebuggingPort = debug ? 20480 : 0,
                NoSandbox = false,
            };
            CefBrowserSettings = new CefBrowserSettings
            {
                DefaultEncoding = "UTF-8",
                FileAccessFromFileUrls = CefState.Enabled,
                UniversalAccessFromFileUrls = CefState.Enabled,
                WebSecurity = CefState.Enabled,                
            };

            WindowTitle = "Web App";
            Icon = "favicon.ico";
            Args = new string[0];
            StartUrl = "about:blank";
            CenterToScreen = true;
            HideConsoleWindow = true;
            EnableNavigationKeys = true;
            EnableReload = debug;
            EnableToggleFullScreen = true;
            Debug = true;
        }

        public string WindowTitle { get; set; }
        public string Icon { get; set; }
        public string CefPath { get; set; }
        public string[] Args { get; set; }
        public CefSettings CefSettings { get; set; }
        public CefBrowserSettings CefBrowserSettings { get; set; }
        public bool EnableCorsScheme { get; set; } = true;
        public List<ProxyScheme> Schemes { get; set; } = new List<ProxyScheme>();
        public List<SchemeFactory> SchemeFactories { get; set; } = new List<SchemeFactory>();
        public string StartUrl { get; set; }
        public int? X { get; set; }
        public int? Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public bool CenterToScreen { get; set; }
        public bool HideConsoleWindow { get; set; }
        public bool EnableNavigationKeys { get; set; }    // ALT+Left + ALT+Right
        public bool EnableReload { get; set; }            // F5
        public bool EnableToggleFullScreen { get; set; }  // F11 
        public bool FullScreen { get; set; }
        public bool Kiosk { get; set; }
        public bool Debug { get; set; }
        
        public Dictionary<string,string> Meta { get; set; }

        public bool DevTools
        {
            get => CefSettings.RemoteDebuggingPort != 0;
            set => CefSettings.RemoteDebuggingPort = value ? 20480 : 0;
        }

        public bool Verbose
        {
            get => CefSettings.LogSeverity == CefLogSeverity.Verbose;
            set => CefSettings.LogSeverity = value ? CefLogSeverity.Verbose : CefLogSeverity.Info;
        }
        
        //WebBrowser
        public Action<WebBrowser> OnCreated { get; set; }
        public Action<WebBrowser,string> OnTitleChanged { get; set; }
        public Action<WebBrowser,string> OnAddressChanged { get; set; }
        public Action<WebBrowser,string> OnTargetUrlChanged { get; set; }
        public Action<WebBrowser,LoadingStateChangedEventArgs> OnLoadingStateChanged { get; set; }
        public Action<WebBrowser,string> OnLog { get; set; }
        public Func<WebClient,CefBrowser,CefProcessId,CefProcessMessage,bool?> OnProcessMessageReceived { get; set; }
        
        //WebLifeSpanHandler
        public Action<CefBrowser> OnLifeSpanAfterCreated { get; set; }
        public Action<CefBrowser> OnLifeSpanBeforeClose { get; set; }
        public Action<CefBrowser> OnLifeSpanDoClose { get; set; }
        
        //WebDisplayHandler
        public Action<CefBrowser,string> OnDisplayTitleChange { get; set; }
        public Action<CefBrowser,CefFrame,string> OnDisplayAddressChange { get; set; }
        public Action<CefBrowser,string> OnDisplayStatusMessage { get; set; }
        public Func<CefBrowser,string,bool?> OnDisplayTooltip { get; set; }
        public Action<CefBrowser,bool> OnFullscreenModeChange { get; set; }
        
        //WebKeyboardHandler
        public Func<CefBrowser,CefKeyEvent,IntPtr,bool?> OnKeyboardPreKeyEvent { get; set; }
        public Func<CefBrowser,CefKeyEvent,IntPtr,bool?> OnKeyEvent { get; set; }


        //CefApp
        public Action<CefSchemeRegistrar> OnRegisterCustomSchemes { get; set; }
        public Action<string, CefCommandLine> OnBeforeCommandLineProcessing { get; set; }
    }
    
    public class ProxyScheme
    {
        public string Scheme { get; set; } = "https";
        public string TargetScheme { get; set; }
        public string Domain { get; set; }
        public bool AllowCors { get; set; }
        public List<string> IgnoreHeaders { get; set; } = new List<string>();
        public Dictionary<string,string> AddHeaders { get; set; } = new Dictionary<string, string>();
        public CefSchemeOptions? SchemeOptions { get; set; }
        public Func<string,string> ResolveUrl { get; set; }
        
        public Action<NameValueCollection> OnResponseHeaders { get; set; }
    }

    public class SchemeFactory
    {
        public SchemeFactory() { }
        public SchemeFactory(string domain, CefSchemeHandlerFactory factory)
        {
            Domain = domain;
            Factory = factory;
        }
        public string Scheme { get; set; } = "https";
        public string Domain { get; set; }
        public CefSchemeHandlerFactory Factory { get; set; }
        public bool AddCrossOriginWhitelistEntry { get; set; } = true;
        public CefSchemeOptions? SchemeOptions { get; set; }
    }
}