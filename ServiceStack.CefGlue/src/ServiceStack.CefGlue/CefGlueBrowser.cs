using System;
using System.Runtime.InteropServices;
using Xilium.CefGlue;

namespace ServiceStack.CefGlue
{
    public class CefGlueBrowser
    {
        private IntPtr browserWindowHandle = IntPtr.Zero;
        public WebClient Client { get; }
        public CefConfig Config { get; }
        public CefApp App { get; }
        public WebBrowser WebBrowser { get; }
        public CefBrowser CefBrowser { get; private set; }
        public IntPtr ParentHandle { get; }
        public IntPtr BrowserWindowHandle => browserWindowHandle;

        public CefGlueBrowser(IntPtr parentHandle, CefApp app, CefConfig config)
        {
            this.ParentHandle = parentHandle;
            this.App = app;
            this.Config = config;

            var windowInfo = CefWindowInfo.Create();
            windowInfo.SetAsChild(parentHandle, new CefRectangle(0, 0, config.Width, config.Height));

            this.WebBrowser = new WebBrowser(this);
            this.WebBrowser.Created += WebBrowser_Created;
            this.Client = new WebClient(this.WebBrowser);

            CefBrowserHost.CreateBrowser(windowInfo, Client, config.CefBrowserSettings, config.StartUrl);
        }

        public string Title { get; private set; }
        public string Address { get; private set; }
        public event EventHandler BrowserCreated;
        public event EventHandler<TitleChangedEventArgs> TitleChanged;
        public event EventHandler<AddressChangedEventArgs> AddressChanged;
        public event EventHandler<StatusMessageEventArgs> StatusMessage;
        public event EventHandler<ConsoleMessageEventArgs> ConsoleMessage;
        public event EventHandler<LoadingStateChangeEventArgs> LoadingStateChange;
        public event EventHandler<TooltipEventArgs> Tooltip;
        public event EventHandler BeforeClose;
        public event EventHandler<BeforePopupEventArgs> BeforePopup;
        public event EventHandler<LoadEndEventArgs> LoadEnd;
        public event EventHandler<LoadErrorEventArgs> LoadError;
        public event EventHandler<LoadStartEventArgs> LoadStarted;
        public event EventHandler<PluginCrashedEventArgs> PluginCrashed;
        public event EventHandler<RenderProcessTerminatedEventArgs> RenderProcessTerminated;

        private void WebBrowser_Created(object sender, EventArgs e)
        {
            if (browserWindowHandle == IntPtr.Zero) // Main Window on Startup
            {
                this.CefBrowser = this.WebBrowser.CefBrowser;
                this.browserWindowHandle = CefBrowser.GetHost().GetWindowHandle();

                var offsetWidth = Config.Width - 22; //For some reason it's 22px too long when first started
                CefPlatform.Instance.ResizeWindow(browserWindowHandle, offsetWidth, Config.Height);
            }

            this.BrowserCreated?.Invoke(this, EventArgs.Empty);
        }
        public virtual void OnTitleChanged(TitleChangedEventArgs eventArgs)
        {
            this.Title = eventArgs.Title;
            this.TitleChanged?.Invoke(this, eventArgs);
        }
        public virtual void OnAddressChanged(AddressChangedEventArgs eventArgs)
        {
            this.Address = eventArgs.Address;
            this.AddressChanged?.Invoke(this, eventArgs);
        }
        public virtual void OnStatusMessage(StatusMessageEventArgs eventArgs)
        {
            this.StatusMessage?.Invoke(this, eventArgs);
        }
        public virtual void OnConsoleMessage(ConsoleMessageEventArgs eventArgs)
        {
            this.ConsoleMessage?.Invoke(this, eventArgs);
        }
        public virtual void OnLoadingStateChange(LoadingStateChangeEventArgs eventArgs)
        {
            this.LoadingStateChange?.Invoke(this, eventArgs);
        }
        public virtual void OnTooltip(TooltipEventArgs eventArgs)
        {
            this.Tooltip?.Invoke(this, eventArgs);
        }
        public virtual void OnBeforeClose()
        {
            this.browserWindowHandle = IntPtr.Zero;
            this.BeforeClose?.Invoke(this, EventArgs.Empty);
        }
        public virtual void OnBeforePopup(BeforePopupEventArgs eventArgs)
        {
            this.BeforePopup?.Invoke(this, eventArgs);
        }
        public virtual void OnLoadEnd(LoadEndEventArgs eventArgs)
        {
            this.LoadEnd?.Invoke(this, eventArgs);
        }
        public virtual void OnLoadError(LoadErrorEventArgs eventArgs)
        {
            this.LoadError?.Invoke(this, eventArgs);
        }
        public virtual void OnLoadStart(LoadStartEventArgs eventArgs)
        {
            this.LoadStarted?.Invoke(this, eventArgs);
        }
        public virtual void OnPluginCrashed(PluginCrashedEventArgs eventArgs)
        {
            this.PluginCrashed?.Invoke(this, eventArgs);
        }
        public virtual void OnRenderProcessTerminated(RenderProcessTerminatedEventArgs eventArgs)
        {
            this.RenderProcessTerminated?.Invoke(this, eventArgs);
        }

        public virtual void ResizeWindow(int width, int height)
        {
            CefPlatform.Instance.ResizeWindow(this.browserWindowHandle, width, height);
        }

        public void DisposeCefBrowser()
        {
            CefBrowser.Dispose();
            CefBrowser = null;
        }

        protected void Dispose(bool disposing)
        {
            if (this.CefBrowser != null && disposing)
            {
                var host = this.CefBrowser.GetHost();
                if (host != null)
                {
                    host.CloseBrowser();
                    host.Dispose();
                }

                this.CefBrowser.Dispose();
                this.CefBrowser = null;
                this.browserWindowHandle = IntPtr.Zero;
            }
        }
        private bool isDisposed;
        ~CefGlueBrowser()
        {
            this.Dispose(false);
        }
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }
    }

    public sealed class StatusMessageEventArgs : EventArgs
    {
        public StatusMessageEventArgs(string value) => Value = value;
        public string Value { get; }
    }
    public class ConsoleMessageEventArgs : EventArgs
    {
        public ConsoleMessageEventArgs(CefLogSeverity level, string message, string source, int line)
        {
            Level = level;
            Message = message;
            Source = source;
            Line = line;
        }
        public CefLogSeverity Level { get; private set; }
        public string Message { get; private set; }
        public string Source { get; private set; }
        public int Line { get; private set; }
        public bool Handled { get; set; }
    }
    public class LoadingStateChangeEventArgs : EventArgs
    {
        public LoadingStateChangeEventArgs(bool isLoading, bool canGoBack, bool canGoForward)
        {
            IsLoading = isLoading;
            CanGoBack = canGoBack;
            CanGoForward = canGoForward;
        }

        public bool IsLoading { get; private set; }
        public bool CanGoBack { get; private set; }
        public bool CanGoForward { get; private set; }
    }
    public class TooltipEventArgs : EventArgs
    {
        public TooltipEventArgs(string text) => Text = text;
        public string Text { get; private set; }
        public bool Handled { get; set; }
    }
    public class BeforePopupEventArgs : EventArgs
    {
        public BeforePopupEventArgs(
            CefFrame frame,
            string targetUrl,
            string targetFrameName,
            CefPopupFeatures popupFeatures,
            CefWindowInfo windowInfo,
            CefClient client,
            CefBrowserSettings settings,
            bool noJavascriptAccess)
        {
            Frame = frame;
            TargetUrl = targetUrl;
            TargetFrameName = targetFrameName;
            PopupFeatures = popupFeatures;
            WindowInfo = windowInfo;
            Client = client;
            Settings = settings;
            NoJavascriptAccess = noJavascriptAccess;
        }
        public bool NoJavascriptAccess { get; set; }
        public CefBrowserSettings Settings { get; private set; }
        public CefClient Client { get; set; }
        public CefWindowInfo WindowInfo { get; private set; }
        public CefPopupFeatures PopupFeatures { get; private set; }
        public string TargetFrameName { get; private set; }
        public string TargetUrl { get; private set; }
        public CefFrame Frame { get; private set; }
        public bool Handled { get; set; }
    }
    public class LoadEndEventArgs : EventArgs
    {
        public LoadEndEventArgs(CefFrame frame, int httpStatusCode)
        {
            Frame = frame;
            HttpStatusCode = httpStatusCode;
        }
        public int HttpStatusCode { get; private set; }
        public CefFrame Frame { get; private set; }
    }
    public class LoadErrorEventArgs : EventArgs
    {
        public LoadErrorEventArgs(CefFrame frame, CefErrorCode errorCode, string errorText, string failedUrl)
        {
            Frame = frame;
            ErrorCode = errorCode;
            ErrorText = errorText;
            FailedUrl = failedUrl;
        }
        public string FailedUrl { get; private set; }
        public string ErrorText { get; private set; }
        public CefErrorCode ErrorCode { get; private set; }
        public CefFrame Frame { get; private set; }
    }
    public class LoadStartEventArgs : EventArgs
    {
        public LoadStartEventArgs(CefFrame frame) => Frame = frame;
        public CefFrame Frame { get; private set; }
    }
    public class PluginCrashedEventArgs : EventArgs
    {
        public PluginCrashedEventArgs(string pluginPath) => PluginPath = pluginPath;
        public string PluginPath { get; private set; }
    }
    public class RenderProcessTerminatedEventArgs : EventArgs
    {
        public RenderProcessTerminatedEventArgs(CefTerminationStatus status) => Status = status;
        public CefTerminationStatus Status { get; private set; }
    }
}
