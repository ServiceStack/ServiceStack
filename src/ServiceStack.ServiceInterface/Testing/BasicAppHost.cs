using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Funq;
using ServiceStack.Html;
using ServiceStack.IO;
using ServiceStack.VirtualPath;
using ServiceStack.ServiceHost;
using ServiceStack.WebHost.Endpoints;

namespace ServiceStack.ServiceInterface.Testing
{
    public class BasicAppHost : IAppHost, IHasContainer
    {
        public BasicAppHost()
        {
            this.Container = new Container();
            this.PreRequestFilters = new List<Action<IHttpRequest, IHttpResponse>>();
            this.RequestFilters = new List<Action<IHttpRequest, IHttpResponse, object>>();
            this.ResponseFilters = new List<Action<IHttpRequest, IHttpResponse, object>>();
            this.ViewEngines = new List<IViewEngine>();
            this.CatchAllHandlers = new List<HttpHandlerResolverDelegate>();
            VirtualPathProvider = new FileSystemVirtualPathProvider(this, "~".MapServerPath());
        }

        public void RegisterAs<T, TAs>() where T : TAs
        {
            this.Container.RegisterAs<T, TAs>();
        }

        public virtual void Release(object instance) { }
        
        public void OnEndRequest() {}
        
        public IServiceRoutes Routes { get; private set; }

        public void Register<T>(T instance)
        {
            this.Container.Register(instance);
        }

        public T TryResolve<T>()
        {
            return this.Container.TryResolve<T>();
        }

        public Container Container { get; set; }

        public IContentTypeFilter ContentTypeFilters { get; set; }

        public List<Action<IHttpRequest, IHttpResponse>> PreRequestFilters { get; set; }

        public List<Action<IHttpRequest, IHttpResponse, object>> RequestFilters { get; set; }

        public List<Action<IHttpRequest, IHttpResponse, object>> ResponseFilters { get; set; }

        public List<IViewEngine> ViewEngines { get; set; }

        public HandleUncaughtExceptionDelegate ExceptionHandler { get; set; }

        public HandleServiceExceptionDelegate ServiceExceptionHandler { get; set; }

        public List<HttpHandlerResolverDelegate> CatchAllHandlers { get; set; }

        public Dictionary<Type, Func<IHttpRequest, object>> RequestBinders
        {
            get { throw new NotImplementedException(); }
        }

        public EndpointHostConfig Config { get; set; }

        public void RegisterService(Type serviceType, params string[] atRestPaths)
        {
            if (Config == null)
                Config = new EndpointHostConfig("BasicAppHost", new ServiceManager(Container, Assembly.GetExecutingAssembly()));

            Config.ServiceManager.RegisterService(serviceType);
        }

        public List<IPlugin> Plugins { get; private set; }

        public void LoadPlugin(params IPlugin[] plugins)
        {
            plugins.ToList().ForEach(x => x.Register(this));
        }
	
		public IVirtualPathProvider VirtualPathProvider { get; set; }
        
        public IServiceRunner<TRequest> CreateServiceRunner<TRequest>(ActionContext actionContext)
        {
            throw new NotImplementedException();
        }

        public void Init()
        {
            EndpointHost.ConfigureHost(this, GetType().Name, Config.ServiceManager);
        }
    }
}