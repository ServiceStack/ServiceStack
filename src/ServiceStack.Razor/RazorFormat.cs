using System;
using System.Collections.Generic;
using System.IO;
using ServiceStack.IO;
using ServiceStack.Logging;
using ServiceStack.Razor.Managers;
using ServiceStack.ServiceHost;
using ServiceStack.WebHost.Endpoints;

namespace ServiceStack.Razor
{
    public class RazorFormat : IPlugin, IRazorPlugin, IRazorConfig
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(RazorFormat));
        public static RazorFormat Instance;
        protected IAppHost AppHost { get; private set; }

        public RazorFormat()
        {
            this.RazorFileExtension = ".cshtml";
            this.DefaultPageName = "default.cshtml";
            this.PageBaseType = typeof(ViewPage);

            Deny = new List<Predicate<string>> {
                DenyPathsWithLeading_,
            };
        }

        //configs
        public string RazorFileExtension { get; set; }
        public Type PageBaseType { get; set; }
        public string DefaultPageName { get; set; }
        public List<Predicate<string>> Deny { get; set; }
        public IVirtualPathProvider VirtualPathProvider { get; set; }

        static bool DenyPathsWithLeading_(string path)
        {
            return Path.GetFileName(path).StartsWith("_");
        }

        public bool WatchForModifiedPages { get; set; }

        //managers
        protected ViewManager ViewManager;
        protected BuildManager BuildManager;
        protected PageResolver PageResolver;

        public void Register(IAppHost appHost)
        {
            if (Instance != null)
            {
                Log.Warn("RazorFormat plugin should only be registered once");
                return;
            }

            Instance = this;

            this.AppHost = appHost;
            var virtualPathProvider = VirtualPathProvider ?? appHost.VirtualPathProvider;
            this.ViewManager = new ViewManager(appHost, this, virtualPathProvider);
            this.BuildManager = new BuildManager(appHost, this);
            this.PageResolver = new PageResolver(appHost, this, this.ViewManager, this.BuildManager);
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
    }

    public interface IRazorConfig
    {
        string RazorFileExtension { get; set; }
        Type PageBaseType { get; set; }
        string DefaultPageName { get; set; }
        List<Predicate<string>> Deny { get; set; }
    }

}