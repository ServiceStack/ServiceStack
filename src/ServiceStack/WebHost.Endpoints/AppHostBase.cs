using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Funq;
using ServiceStack.Common;
using ServiceStack.Configuration;
using ServiceStack.Html;
using ServiceStack.Logging;
using ServiceStack.ServiceHost;
using ServiceStack.VirtualPath;

namespace ServiceStack.WebHost.Endpoints
{
	/// <summary>
	/// Inherit from this class if you want to host your web services inside an
	/// ASP.NET application.
	/// </summary>
	public abstract class AppHostBase
		: IFunqlet, IDisposable, IAppHost
	{
		private readonly ILog log = LogManager.GetLogger(typeof(AppHostBase));

		public static AppHostBase Instance { get; protected set; }

		protected AppHostBase(string serviceName, params Assembly[] assembliesWithServices)
		{
			EndpointHost.ConfigureHost(this, serviceName, CreateServiceManager(assembliesWithServices));
		}

		protected virtual ServiceManager CreateServiceManager(params Assembly[] assembliesWithServices)
		{		
			return new ServiceManager(assembliesWithServices);
			//Alternative way to inject Container + Service Resolver strategy
			//return new ServiceManager(new Container(),
			//    new ServiceController(() => assembliesWithServices.ToList().SelectMany(x => x.GetTypes())));
		}

		protected IServiceController ServiceController
		{
			get
			{
				return EndpointHost.Config.ServiceController;
			}
		}
		
		public IServiceRoutes Routes
		{
			get { return EndpointHost.Config.ServiceController.Routes; }
		}

		public Container Container
		{
			get
			{
				return EndpointHost.Config.ServiceManager != null
					? EndpointHost.Config.ServiceManager.Container : null;
			}
		}

		public void Init()
		{
			if (Instance != null)
			{
				throw new InvalidDataException("AppHostBase.Instance has already been set");
			}

			Instance = this;

		    EndpointHost.Config.DebugMode = GetType().Assembly.IsDebugBuild(); 
			var serviceManager = EndpointHost.Config.ServiceManager;
			if (serviceManager != null)
			{
				serviceManager.Init();
				Configure(EndpointHost.Config.ServiceManager.Container);
			}
			else
			{
				Configure(null);
			}

			EndpointHost.AfterInit();

			if (serviceManager != null)
			{
				//Required for adhoc services added in Configure()
				EndpointHost.SetOperationTypes(
					serviceManager.ServiceOperations,
					serviceManager.AllServiceOperations
				);
			}
		}

		public abstract void Configure(Container container);

		public void SetConfig(EndpointHostConfig config)
		{
			if (config.ServiceName == null)
				config.ServiceName = EndpointHostConfig.Instance.ServiceName;

			if (config.ServiceManager == null)
				config.ServiceManager = EndpointHostConfig.Instance.ServiceManager;

			config.ServiceManager.ServiceController.EnableAccessRestrictions = config.EnableAccessRestrictions;

			EndpointHost.Config = config;
		}

		public void RegisterAs<T, TAs>() where T : TAs
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
            catch {/*ignore*/}
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

		public Dictionary<Type, Func<IHttpRequest, object>> RequestBinders
		{
			get { return EndpointHost.ServiceManager.ServiceController.RequestTypeFactoryMap; }
		}

		public IContentTypeFilter ContentTypeFilters
		{
			get
			{
				return EndpointHost.ContentTypeFilter;
			}
		}

        public List<Action<IHttpRequest, IHttpResponse>> PreRequestFilters
		{
			get
			{
				return EndpointHost.RawRequestFilters;
			}
		}

		public List<Action<IHttpRequest, IHttpResponse, object>> RequestFilters
		{
			get
			{
				return EndpointHost.RequestFilters;
			}
		}

		public List<Action<IHttpRequest, IHttpResponse, object>> ResponseFilters
		{
			get
			{
				return EndpointHost.ResponseFilters;
			}
		}

        public List<IViewEngine> ViewEngines
		{
			get
			{
				return EndpointHost.ViewEngines;
			}
		}

	    public Action<IHttpRequest, IHttpResponse, string, Exception> ExceptionHandler
	    {
	        get { return EndpointHost.ExceptionHandler; }
           set { EndpointHost.ExceptionHandler = value; }
        }

		public List<HttpHandlerResolverDelegate> CatchAllHandlers
		{
			get { return EndpointHost.CatchAllHandlers; }
		}

		public EndpointHostConfig Config
		{
			get { return EndpointHost.Config; }
		}

		public List<IPlugin> Plugins
		{
			get { return EndpointHost.Plugins; }
		}
		
		public IVirtualPathProvider VirtualPathProvider
		{
			get { return EndpointHost.VirtualPathProvider; }
			set { EndpointHost.VirtualPathProvider = value; }
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
			return ExecuteService(requestDto, EndpointAttributes.None);
		}

		public object ExecuteService(object requestDto, EndpointAttributes endpointAttributes)
		{
			return EndpointHost.Config.ServiceController.Execute(requestDto,
				new HttpRequestContext(requestDto, endpointAttributes));
		}

		public void RegisterService(Type serviceType, params string[] atRestPaths)
		{
			var genericService = EndpointHost.Config.ServiceManager.RegisterService(serviceType);
			var requestType = genericService.GetGenericArguments()[0];
			foreach (var atRestPath in atRestPaths)
			{
				this.Routes.Add(requestType, atRestPath, null);
			}
		}
		
		public virtual void Dispose()
		{
			if (EndpointHost.Config.ServiceManager != null)
			{
				EndpointHost.Config.ServiceManager.Dispose();
			}
		}
	}
}
