using System;
using System.Collections.Generic;
using System.IO;
using ServiceStack.IO;
using ServiceStack.Logging;
using ServiceStack.Razor.Managers;
using ServiceStack.ServiceHost;
using ServiceStack.Text;
using ServiceStack.VirtualPath;
using ServiceStack.WebHost.Endpoints;

namespace ServiceStack.Razor
{
    public class RazorFormat : IPlugin, IRazorPlugin, IRazorConfig
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(RazorFormat));
        public static RazorFormat Instance;

        public RazorFormat()
        {
            this.RazorFileExtension = ".cshtml";
            this.DefaultPageName = "default.cshtml";
            this.PageBaseType = typeof(ViewPage);
            this.LiveReloadFactory = CreateLiveReload;

            Deny = new List<Predicate<string>> {
                DenyPathsWithLeading_,
            };
        }

        //configs
        public string RazorFileExtension { get; set; }
        public Type PageBaseType { get; set; }
        public string DefaultPageName { get; set; }
        public string WebHostUrl { get; set; }
        public string ScanRootPath { get; set; }
        public bool? EnableLiveReload { get; set; }
        public List<Predicate<string>> Deny { get; set; }
        public IVirtualPathProvider VirtualPathProvider { get; set; }
        public ILiveReload LiveReload { get; set; }
        public Func<ViewManager, ILiveReload> LiveReloadFactory { get; set; }
        public RenderPartialDelegate RenderPartialFn { get; set; }

        static bool DenyPathsWithLeading_(string path)
        {
            return Path.GetFileName(path).StartsWith("_");
        }

        public bool WatchForModifiedPages { get; set; }

        //managers
        protected ViewManager ViewManager;
        protected PageResolver PageResolver;

        public void Register(IAppHost appHost)
        {
            this.ScanRootPath = this.ScanRootPath ?? appHost.Config.WebHostPhysicalPath;
            this.VirtualPathProvider = VirtualPathProvider ?? appHost.VirtualPathProvider;
            this.WebHostUrl = WebHostUrl ?? appHost.Config.WebHostUrl;
            this.EnableLiveReload = this.EnableLiveReload ?? appHost.Config.DebugMode;

            Init();

            BindToAppHost(appHost);
        }

        private void BindToAppHost(IAppHost appHost)
        {
            appHost.CatchAllHandlers.Add(this.PageResolver.CatchAllHandler);
            appHost.ViewEngines.Add(this.PageResolver);

            if (this.RenderPartialFn == null)
            {
                this.RenderPartialFn = (pageName, model, renderHtml, writer, htmlHelper, httpReq) => {
                    foreach (var viewEngine in appHost.ViewEngines) 
                    {
                        if (viewEngine == PageResolver || !viewEngine.HasView(pageName, httpReq)) continue;
                        return viewEngine.RenderPartial(pageName, model, renderHtml, writer, htmlHelper);
                    }
                    writer.Write("<!--{0} not found-->".Fmt(pageName));
                    return null;
                };
            }
            this.PageResolver.RenderPartialFn = this.RenderPartialFn;
        }

        public RazorFormat Init()
        {
            if (Instance != null)
            {
                Log.Warn("RazorFormat plugin should only be initialized once");
                return this;
            }

            Instance = this;

            this.ViewManager = new ViewManager(this, VirtualPathProvider);
            this.PageResolver = new PageResolver(this, this.ViewManager);

            this.ViewManager.Init();

            if (EnableLiveReload.GetValueOrDefault())
            {
                this.LiveReload = LiveReloadFactory(this.ViewManager);
                this.LiveReload.StartWatching(this.ScanRootPath);
            }
            return this;
        }

        static ILiveReload CreateLiveReload(ViewManager viewManager)
        {
            return new FileSystemWatcherLiveReload(viewManager);
        }

        public RazorPage FindByPathInfo(string pathInfo)
        {
            return ViewManager.GetRazorViewByPathInfo(pathInfo);
        }

        public void ProcessRazorPage(IHttpRequest httpReq, RazorPage contentPage, object model, IHttpResponse httpRes)
        {
            PageResolver.ResolveAndExecuteRazorPage(httpReq, httpRes, model, contentPage);
        }

        public void ProcessRequest(IHttpRequest httpReq, IHttpResponse httpRes, object dto)
        {
            PageResolver.ProcessRequest(httpReq, httpRes, dto);
        }

        public RazorPage AddPage(string filePath)
        {
            return ViewManager.AddRazorPage(filePath);
        }

        public RazorPage CreatePage(string razorContents)
        {
            if (this.VirtualPathProvider == null)
                throw new ArgumentNullException("VirtualPathProvider");

            var writableFileProvider = this.VirtualPathProvider as IWriteableVirtualPathProvider;
            if (writableFileProvider == null)
                throw new InvalidOperationException("VirtualPathProvider is not IWriteableVirtualPathProvider");

            var tmpPath = "/__tmp/{0}.cshtml".Fmt(Guid.NewGuid().ToString("N"));
            writableFileProvider.AddFile(tmpPath, razorContents);

            return ViewManager.AddRazorPage(tmpPath);
        }

        public string RenderToHtml(string filePath, object dto = null)
        {
            var razorView = ViewManager.GetRazorView(filePath);
            if (razorView == null)
                throw new FileNotFoundException("Razor file not found", filePath);

            return RenderToHtml(razorView, dto);
        }

        public string RenderToHtml(RazorPage razorPage, object dto = null)
        {
            if (razorPage == null)
                throw new ArgumentNullException("razorPage");

            var mqContext = new MqRequestContext();
            var httpRes = new MqResponse(mqContext);
            PageResolver.ResolveAndExecuteRazorPage(
                httpReq: new MqRequest(mqContext),
                httpRes: httpRes,
                dto: dto,
                razorPage: razorPage);

            var ms = (MemoryStream)httpRes.OutputStream;
            return ms.ToArray().FromUtf8Bytes();
        }
    }

    public interface IRazorConfig
    {
        string RazorFileExtension { get; }
        Type PageBaseType { get; }
        string DefaultPageName { get; }
        string ScanRootPath { get; }
        string WebHostUrl { get; }
        List<Predicate<string>> Deny { get; }
    }

}