using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Funq;
using ServiceStack.Logging;
using ServiceStack.ServiceHost;
using ServiceStack.Text;

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
		private readonly DateTime startTime;

		public static AppHostBase Instance { get; protected set; }

		protected AppHostBase()
		{
			this.startTime = DateTime.Now;
			log.Info("Begin Initializing Application...");
		}

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
			if (serviceManager != null)
			{
				//Required for adhoc services added in Configure()
				serviceManager.ReloadServiceOperations();
				EndpointHost.SetOperationTypes(
					serviceManager.ServiceOperations,
					serviceManager.AllServiceOperations
				);
			}

			EndpointHost.AfterInit();

			var elapsed = DateTime.Now - this.startTime;
			log.InfoFormat("Initializing Application took {0}ms", elapsed.TotalMilliseconds);
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
			var autoWire = new AutoWireContainer(this.Container);
			autoWire.RegisterAs<T, TAs>();
		}

		public void Register<T>(T instance)
		{
			this.Container.Register(instance);
		}

		public T TryResolve<T>()
		{
			return this.Container.TryResolve<T>();
		}

		public IContentTypeFilter ContentTypeFilters
		{
			get
			{
				return EndpointHost.ContentTypeFilter;
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

		public List<StreamSerializerResolverDelegate> HtmlProviders
		{
			get
			{
				return EndpointHost.HtmlProviders;
			}
		}

		public List<HttpHandlerResolverDelegate> CatchAllHandlers
		{
			get { return EndpointHost.CatchAllHandlers; }
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

		public EndpointHostConfig Config
		{
			get { return EndpointHost.Config; }
		}

		public void RegisterService(Type serviceType, params string[] atRestPaths)
		{
			var genericService = EndpointHost.Config.ServiceManager.RegisterService(serviceType);
			var requestType = genericService.GetGenericArguments()[0];
			foreach (var atRestPath in atRestPaths)
			{
				this.Routes.Add(requestType, atRestPath, null, null);
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
