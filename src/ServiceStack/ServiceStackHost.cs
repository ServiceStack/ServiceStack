// Copyright (c) Service Stack LLC. All Rights Reserved.
// License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt


using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Web;
using Funq;
using ServiceStack.Configuration;
using ServiceStack.Host;
using ServiceStack.Html;
using ServiceStack.IO;
using ServiceStack.Logging;
using ServiceStack.Web;

namespace ServiceStack
{
    public abstract class ServiceStackHost
        : IAppHost, IFunqlet, IHasContainer, IDisposable
    {
        private readonly ILog log = LogManager.GetLogger(typeof(ServiceStackHost));
        public static ServiceStackHost Instance { get; protected set; }

        protected ServiceStackHost(string serviceName, params Assembly[] assembliesWithServices)
        {
            EndpointHost.ConfigureHost(this, serviceName, CreateServiceManager(assembliesWithServices));
        }

        public abstract void Configure(Container container);

        protected virtual ServiceManager CreateServiceManager(params Assembly[] assembliesWithServices)
        {
            return new ServiceManager(assembliesWithServices);
            //Alternative way to inject Container + Service Resolver strategy
            //return new ServiceManager(new Container(),
            //    new ServiceController(() => assembliesWithServices.ToList().SelectMany(x => x.GetTypes())));
        }

        public void SetConfig(AppHostConfig config)
        {
            if (config.ServiceName == null)
                config.ServiceName = AppHostConfig.Instance.ServiceName;

            if (config.ServiceManager == null)
                config.ServiceManager = AppHostConfig.Instance.ServiceManager;

            config.ServiceManager.ServiceController.EnableAccessRestrictions = config.EnableAccessRestrictions;

            EndpointHost.Config = config;
        }

        public void Init()
        {
            if (Instance != null)
            {
                throw new InvalidDataException("ServiceStackHost.Instance has already been set");
            }

            Instance = this;

            if (ServiceManager != null)
            {
                ServiceManager.Init();
                Configure(Container);
            }
            else
            {
                Configure(null);
            }

            EndpointHost.AfterInit();
            OnAfterInit();
        }

        public virtual void OnAfterInit() {}

        public ServiceManager ServiceManager { get; internal set; }

        public ServiceMetadata Metadata { get { return ServiceManager.Metadata; } }
        
        public IServiceController ServiceController { get { return ServiceManager.ServiceController; } }
        
        public Container Container { get { return ServiceManager.Container; } }
        
        public IServiceRoutes Routes { get { return ServiceController.Routes; } }

        public Dictionary<Type, Func<IHttpRequest, object>> RequestBinders
        {
            get { return ServiceManager.ServiceController.RequestTypeFactoryMap; }
        }

        public IContentTypes ContentTypes { get; set; }

        public List<Action<IHttpRequest, IHttpResponse>> PreRequestFilters { get; set; }

        public List<Action<IHttpRequest, IHttpResponse, object>> GlobalRequestFilters { get; set; }

        public List<Action<IHttpRequest, IHttpResponse, object>> GlobalResponseFilters { get; set; }

        public List<IViewEngine> ViewEngines { get; set; }

        public HandleUncaughtExceptionDelegate ExceptionHandler { get; set; }

        public HandleServiceExceptionDelegate ServiceExceptionHandler { get; set; }

        public List<HttpHandlerResolverDelegate> CatchAllHandlers { get; set; }

        public AppHostConfig Config { get; set; }

        public List<IPlugin> Plugins { get; set; }

        public IVirtualPathProvider VirtualPathProvider { get; set; }

        public virtual void RegisterAs<T, TAs>() where T : TAs
        {
            this.Container.RegisterAutoWiredAs<T, TAs>();
        }

        public virtual void Release(object instance)
        {
            try
            {
                var iocAdapterReleases = Container.Adapter as IRelease;
                if (iocAdapterReleases != null)
                {
                    iocAdapterReleases.Release(instance);
                }
                else
                {
                    var disposable = instance as IDisposable;
                    if (disposable != null)
                        disposable.Dispose();
                }
            }
            catch { /*ignore*/ }
        }

        public virtual void OnEndRequest()
        {
            foreach (var item in HostContext.Instance.Items.Values)
            {
                Release(item);
            }

            HostContext.Instance.EndRequest();
        }

        public void Register<T>(T instance)
        {
            this.Container.Register(instance);
        }

        public T TryResolve<T>()
        {
            return this.Container.TryResolve<T>();
        }

        /// <summary>
        /// Resolves from IoC container a specified type instance.
        /// </summary>
        public static T Resolve<T>()
        {
            if (Instance == null) throw new InvalidOperationException("ServiceStackHost is not initialized.");
            return Instance.Container.Resolve<T>();
        }

        /// <summary>
        /// Resolves and auto-wires a ServiceStack Service
        /// </summary>
        public static T ResolveService<T>(HttpContext httpCtx) where T : class, IRequiresRequestContext
        {
            if (Instance == null) throw new InvalidOperationException("ServiceStackHost is not initialized.");
            var service = Instance.Container.Resolve<T>();
            if (service == null) return null;
            service.RequestContext = httpCtx.ToRequestContext();
            return service;
        }

        public virtual IServiceRunner<TRequest> CreateServiceRunner<TRequest>(ActionContext actionContext)
        {
            //cached per service action
            return new ServiceRunner<TRequest>(this, actionContext);
        }

        public virtual string ResolveAbsoluteUrl(string virtualPath, IHttpRequest httpReq)
        {
            return httpReq.GetAbsoluteUrl(virtualPath); //Http Listener, TODO: ASP.NET overrides
        }

        public virtual void LoadPlugin(params IPlugin[] plugins)
        {
            foreach (var plugin in plugins)
            {
                try
                {
                    plugin.Register(this);
                }
                catch (Exception ex)
                {
                    log.Warn("Error loading plugin " + plugin.GetType().Name, ex);
                }
            }
        }

        public virtual object ExecuteService(object requestDto)
        {
            return ExecuteService(requestDto, RequestAttributes.None);
        }

        public virtual object ExecuteService(object requestDto, RequestAttributes requestAttributes)
        {
            return ServiceController.Execute(requestDto, new HttpRequestContext(requestDto, requestAttributes));
        }

        public virtual void RegisterService(Type serviceType, params string[] atRestPaths)
        {
            ServiceManager.RegisterService(serviceType);
            var reqAttr = serviceType.GetCustomAttributes(true).OfType<DefaultRequestAttribute>().FirstOrDefault();
            if (reqAttr != null)
            {
                foreach (var atRestPath in atRestPaths)
                {
                    this.Routes.Add(reqAttr.RequestType, atRestPath, null);
                }
            }
        }

        public virtual void Dispose()
        {
            if (ServiceManager != null)
            {
                ServiceManager.Dispose();
            }
        }
    }
}