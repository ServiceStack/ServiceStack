using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Funq;
using ServiceStack.Common.Web;
using ServiceStack.Logging;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceModel.Serialization;
using ServiceStack.Text;
using ServiceStack.WebHost.Endpoints.Formats;

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
			: this()
		{
			SetConfig(new EndpointHostConfig
			{
				ServiceName = serviceName,
				ServiceManager = new ServiceManager(assembliesWithServices),
			});
		}

		protected IServiceController ServiceController
		{
			get
			{
				return EndpointHost.Config.ServiceController;
			}
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
			EndpointHost.ConfigureHost(this);

			var serviceManager = EndpointHost.Config.ServiceManager;
			if (serviceManager != null)
			{
				serviceManager.Init();
				Configure(EndpointHost.Config.ServiceManager.Container);

				EndpointHost.SetOperationTypes(
					serviceManager.ServiceOperations,
					serviceManager.AllServiceOperations
				);
			}
			else
			{
				Configure(null);
			}

			var elapsed = DateTime.Now - this.startTime;
			log.InfoFormat("Initializing Application took {0}ms", elapsed.TotalMilliseconds);
		}

		public abstract void Configure(Container container);

		public void SetConfig(EndpointHostConfig config)
		{
			if (config.ServiceName == null)
				config.ServiceName = EndpointHost.Config.ServiceName;

			if (config.ServiceManager == null)
				config.ServiceManager = EndpointHost.Config.ServiceManager;

			config.ServiceManager.ServiceController.EnableAccessRestrictions = config.EnableAccessRestrictions;

			EndpointHost.Config = config;

			JsonDataContractSerializer.Instance.UseBcl = config.UseBclJsonSerializers;
			JsonDataContractDeserializer.Instance.UseBcl = config.UseBclJsonSerializers;			
		}

		public T TryResolve<T>()
		{
			return this.Container.TryResolve<T>();
		}

		public IContentTypeFilter ContentTypeFilters
		{
			get
			{
				return EndpointHost.Config.ContentTypeFilter;
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

		public virtual void Dispose()
		{
			if (EndpointHost.Config.ServiceManager != null)
			{
				EndpointHost.Config.ServiceManager.Dispose();
			}
		}
	}
}