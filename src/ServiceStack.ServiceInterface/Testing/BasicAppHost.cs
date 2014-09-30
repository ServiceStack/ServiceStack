/*
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
using ServiceStack.WebHost.Endpoints.Extensions;

namespace ServiceStack.ServiceInterface.Testing
{
    public class BasicAppHost : IAppHost, IHasContainer, IDisposable
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

        private EndpointHostConfig config;
        public EndpointHostConfig Config
        {
            get
            {
                return config ?? (new EndpointHostConfig("BasicAppHost", new ServiceManager(Container, Assembly.GetExecutingAssembly())));
            }
            set { config = value; }
        }

        public void RegisterService(Type serviceType, params string[] atRestPaths)
        {
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

        public virtual string ResolveAbsoluteUrl(string virtualPath, IHttpRequest httpReq)
        {
            return httpReq.GetAbsoluteUrl(virtualPath);
        }

        public BasicAppHost Init()
        {
            EndpointHost.ConfigureHost(this, GetType().Name, Config.ServiceManager);
            return this;
        }


        private bool disposed;
        protected virtual void Dispose(bool disposing)
        {
            if (disposed) return;

            lock (this)
            {
                if (disposed) return;

                if (disposing)
                {
                    if (EndpointHost.Config != null && EndpointHost.Config.ServiceManager != null)
                    {
                        EndpointHost.Config.ServiceManager.Dispose();
                    }

                    EndpointHost.Dispose();
                }

                //release unmanaged resources here...
                disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
*/
