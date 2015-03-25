using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ServiceStack.Host;
using ServiceStack.Host.Handlers;
using ServiceStack.Html;
using ServiceStack.IO;
using ServiceStack.Logging;
using ServiceStack.Razor.Compilation;
using ServiceStack.Razor.Managers;
using ServiceStack.VirtualPath;
using ServiceStack.Web;

namespace ServiceStack.Razor
{
    using System.Reflection;

    public class RazorFormat : IPlugin, IRazorPlugin, IRazorConfig
    {
        public const string TemplatePlaceHolder = "@RenderBody()";

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
                DenyDirectAccessToViews
            };

            LoadFromAssemblies = new List<Assembly>();
            LoadUnloadedAssemblies = true;
        }

        //configs
        public string RazorFileExtension { get; set; }
        public Type PageBaseType { get; set; }
        public string DefaultPageName { get; set; }
        public string WebHostUrl { get; set; }
        public string ScanRootPath { get; set; }
        public List<Assembly> LoadFromAssemblies { get; set; }
        public List<Predicate<string>> Deny { get; set; }
        public bool? EnableLiveReload { get; set; }
        public bool? PrecompilePages { get; set; }
        public bool? WaitForPrecompilationOnStartup { get; set; }
        public bool MinifyHtml { get; set; }
        public bool UseAdvancedCompression { get; set; }
        public bool LoadUnloadedAssemblies { get; set; }
        public IVirtualPathProvider VirtualPathProvider { get; set; }
        public ILiveReload LiveReload { get; set; }
        public Func<RazorViewManager, ILiveReload> LiveReloadFactory { get; set; }
        public RenderPartialDelegate RenderPartialFn { get; set; }

        public Action<RenderingPage, string> OnWriteLiteral
        {
            set
            {
                if (value != null)
                    RenderingPage.WriteLiteralFn = value;
            }
        }

        public Action<RenderingPage, TextWriter, string> OnWriteLiteralTo
        {
            set
            {
                if (value != null)
                    RenderingPage.WriteLiteralToFn = value;
            }
        }

        static bool DenyPathsWithLeading_(string path)
        {
            return Path.GetFileName(path).StartsWith("_");
        }

        static bool DenyDirectAccessToViews(string path)
        {
            return path.StartsWithIgnoreCase("/views");
        }

        public bool WatchForModifiedPages
        {
            get { return EnableLiveReload.GetValueOrDefault(); }
            set { EnableLiveReload = value; }
        }

        //managers
        protected RazorViewManager ViewManager;
        protected RazorPageResolver PageResolver;

        public void Register(IAppHost appHost)
        {
            this.ScanRootPath = this.ScanRootPath ?? appHost.Config.WebHostPhysicalPath;
            this.VirtualPathProvider = VirtualPathProvider ?? appHost.VirtualPathProvider;
            this.WebHostUrl = WebHostUrl ?? appHost.Config.WebHostUrl;
            this.EnableLiveReload = this.EnableLiveReload ?? appHost.Config.DebugMode;
            this.PrecompilePages = this.PrecompilePages ?? !this.EnableLiveReload;
            this.WaitForPrecompilationOnStartup = this.WaitForPrecompilationOnStartup ?? !this.EnableLiveReload;

            if (LoadUnloadedAssemblies)
            {
                var loadedAssemblyNames = CompilerServices
                    .GetLoadedAssemblies()
                    .Where(x => !x.IsDynamic)
                    .Map(x => x.FullName.SplitOnFirst(',')[0]);

                foreach (var razorNamespace in appHost.Config.RazorNamespaces)
                {
                    try
                    {
                        if (razorNamespace.StartsWith("System") || 
                            razorNamespace.StartsWith("ServiceStack"))
                            continue;

                        if (!loadedAssemblyNames.Contains(razorNamespace))
                        {
                            Assembly.Load(razorNamespace);
                        }
                    }
                    catch { /*Ignore namespaces that don't map to assemblies*/ }
                }
            }

            try
            {
                Init();

                BindToAppHost(appHost);
            }
            catch (Exception ex)
            {
                appHost.NotifyStartupException(ex);
                throw;
            }
        }

        private void BindToAppHost(IAppHost appHost)
        {
            appHost.CatchAllHandlers.Add(this.PageResolver.CatchAllHandler);
            appHost.ViewEngines.Add(this.PageResolver);

            if (this.RenderPartialFn == null)
            {
                this.RenderPartialFn = (pageName, model, renderHtml, writer, htmlHelper, httpReq) =>
                {
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

        public virtual RazorFormat Init()
        {
            if (Instance != null)
            {
                Log.Warn("RazorFormat plugin should only be initialized once");

                if (ViewManager != null && PageResolver != null)
                    return this;

                Log.Warn("Incomplete initialization, RazorFormat.Instance set but ViewManager/PageResolver is null");
            }

            Instance = this;

            this.ViewManager = CreateViewManager();
            this.PageResolver = CreatePageResolver();

            this.ViewManager.Init();

            if (EnableLiveReload.GetValueOrDefault())
            {
                this.LiveReload = LiveReloadFactory(this.ViewManager);
                this.LiveReload.StartWatching(this.ScanRootPath);
            }
            return this;
        }

        public virtual RazorPageResolver CreatePageResolver()
        {
            return new RazorPageResolver(this, this.ViewManager)
            {
                MinifyHtml = MinifyHtml,
                UseAdvancedCompression = UseAdvancedCompression,
            };
        }

        public virtual RazorViewManager CreateViewManager()
        {
            return new RazorViewManager(this, VirtualPathProvider);
        }

        static ILiveReload CreateLiveReload(RazorViewManager viewManager)
        {
            return new FileSystemWatcherLiveReload(viewManager);
        }

        public void ProcessRazorPage(IRequest httpReq, RazorPage contentPage, object model, IResponse httpRes)
        {
            PageResolver.ExecuteRazorPage(httpReq, httpRes, model, contentPage);
        }

        public void ProcessRequest(IRequest httpReq, IResponse httpRes, object dto)
        {
            PageResolver.ProcessRequest(httpReq, httpRes, dto);
        }
        public void ProcessContentPageRequest(IRequest httpReq, IResponse httpRes)
        {
            ((IServiceStackHandler)PageResolver).ProcessRequest(httpReq, httpRes, httpReq.OperationName);
        }

        public RazorPage AddPage(string filePath)
        {
            return ViewManager.AddPage(filePath);
        }

        public RazorPage GetViewPage(string pageName)
        {
            return ViewManager.GetViewPage(pageName);
        }

        public RazorPage GetContentPage(string pathInfo)
        {
            return ViewManager.GetContentPage(pathInfo);
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

            return ViewManager.AddPage(tmpPath);
        }

        public string RenderToHtml(string filePath, object model = null, string layout = null)
        {
            var razorView = ViewManager.GetPage(filePath);
            if (razorView == null)
                throw new FileNotFoundException("Razor file not found", filePath);

            return RenderToHtml(razorView, model: model, layout: layout);
        }

        public string CreateAndRenderToHtml(string razorContents, object model = null, string layout = null)
        {
            var page = CreatePage(razorContents);
            return RenderToHtml(page, model: model, layout: layout);
        }

        public string RenderToHtml(RazorPage razorPage, object model = null, string layout = null)
        {
            IRazorView razorView;
            var result = RenderToHtml(razorPage, out razorView, model: model, layout: layout);
            using (razorView)
            {
                return result;
            }
        }

        public string RenderToHtml(RazorPage razorPage, out IRazorView razorView, object model = null, string layout = null)
        {
            if (razorPage == null)
                throw new ArgumentNullException("razorPage");

            var httpReq = new BasicRequest();
            if (layout != null)
            {
                httpReq.Items[RazorPageResolver.LayoutKey] = layout;
            }

            razorView = PageResolver.ExecuteRazorPage(httpReq, httpReq.Response, model, razorPage);

            var ms = (MemoryStream)httpReq.Response.OutputStream;
            return ms.ToArray().FromUtf8Bytes();
        }
    }

    public interface IRazorConfig
    {
        string RazorFileExtension { get; }
        Type PageBaseType { get; }
        string DefaultPageName { get; }
        string ScanRootPath { get; }
        List<Assembly> LoadFromAssemblies { get; }
        string WebHostUrl { get; }
        List<Predicate<string>> Deny { get; }
        bool? PrecompilePages { get; set; }
        bool? WaitForPrecompilationOnStartup { get; set; }
    }

}