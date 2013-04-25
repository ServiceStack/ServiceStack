using System;
using ServiceStack.Logging;
using ServiceStack.Razor2.Managers;
using ServiceStack.ServiceHost;
using ServiceStack.WebHost.Endpoints;

namespace ServiceStack.Razor2
{
    public class RazorFormat : IPlugin, IRazor2Plugin
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(RazorFormat));

        public static RazorFormat Instance;

        protected IAppHost AppHost { get; private set; }

        public RazorFormat()
        {
            this.BuildConfig = new BuildConfig();
            this.ViewConfig = new ViewConfig();
            this.ResolveConfig = new PageResolverConfig();
        }

        //configs
        public PageResolverConfig ResolveConfig { get; protected set; }
        public ViewConfig ViewConfig { get; protected set; }
        public BuildConfig BuildConfig { get; protected set; }

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
            this.ViewManager = new ViewManager(appHost, this.ViewConfig);
            this.BuildManager = new BuildManager(appHost, this.BuildConfig);
            this.PageResolver = new PageResolver(appHost, this.ResolveConfig, this.ViewManager, this.BuildManager);
        }

        public ViewPage FindByPathInfo(string pathInfo)
        {
            throw new NotImplementedException();
        }

        public void ProcessRazorPage(IHttpRequest httpReq, ViewPage contentPage, object model, IHttpResponse httpRes)
        {
            throw new NotImplementedException();
        }

        public void ReloadIfNeeeded(ViewPage contentPage)
        {
            throw new NotImplementedException();
        }
    }

}