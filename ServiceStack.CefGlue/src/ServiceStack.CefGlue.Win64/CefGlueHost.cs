using System;
using System.Runtime.InteropServices;
using WinApi.Windows;
using Xilium.CefGlue;

namespace ServiceStack.CefGlue.Win64
{
    internal class CefGlueHost : EventedWindowCore
    {
        private readonly CefConfig context;
        public CefGlueHost(CefConfig context) => this.context = context;

        public CefGlueBrowser browser;

        protected bool MultiThreadedMessageLoop => context.CefSettings.MultiThreadedMessageLoop;

        protected override void OnCreate(ref CreateWindowPacket packet)
        {
            CefRuntime.Load();
            CefRuntime.EnableHighDpiSupport();

            var argv = context.Args;
            if (CefRuntime.Platform != CefRuntimePlatform.Windows)
            {
                argv = new string[context.Args.Length + 1];
                Array.Copy(context.Args, 0, argv, 1, context.Args.Length);
                argv[0] = "-";
            }

            var mainArgs = new CefMainArgs(argv);

            var app = new WebCefApp(context);

            var exitCode = CefRuntime.ExecuteProcess(mainArgs, app, IntPtr.Zero);
            if (exitCode != -1)
                return;

            CefRuntime.Initialize(mainArgs, context.CefSettings, app, IntPtr.Zero);

            this.browser = new CefGlueBrowser(this.Handle, app, context);

            base.OnCreate(ref packet);
        }

        protected override void OnSize(ref SizePacket packet)
        {
            base.OnSize(ref packet);
            var size = packet.Size;
            this.browser.ResizeWindow(size.Width, size.Height);
        }
        
        protected override void OnDestroy(ref Packet packet)
        {
            this.browser.Dispose();
            CefRuntime.Shutdown();
            base.OnDestroy(ref packet);
        }
        private void PostTask(CefThreadId threadId, Action action)
        {
            CefRuntime.PostTask(threadId, new ActionTask(action));
        }
        private class ActionTask : CefTask
        {
            private Action mAction;
            public ActionTask(Action action) => this.mAction = action;
            protected override void Execute()
            {
                this.mAction();
                this.mAction = null;
            }
        }

        internal sealed class WebCefApp : CefApp
        {
            private CefConfig context;
            private string[] args;

            public WebCefApp(CefConfig context)
            {
                this.context = context;
                this.args = context.Args;
            }

            protected override void OnRegisterCustomSchemes(CefSchemeRegistrar registrar)
            {                
                context.OnRegisterCustomSchemes?.Invoke(registrar);
            }

            protected override void OnBeforeCommandLineProcessing(string processType, CefCommandLine commandLine)
            {
                context.OnBeforeCommandLineProcessing?.Invoke(processType, commandLine);
            }

            protected override CefRenderProcessHandler GetRenderProcessHandler()
            {
                return base.GetRenderProcessHandler();
            }

            protected override CefBrowserProcessHandler GetBrowserProcessHandler()
            {
                return base.GetBrowserProcessHandler();
            }
        }
    }
}