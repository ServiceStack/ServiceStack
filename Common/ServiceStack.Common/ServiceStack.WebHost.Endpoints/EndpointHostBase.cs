using System;
using System.IO;
using System.Reflection;
using Funq;
using ServiceStack.Logging;
using ServiceStack.LogicFacade;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceInterface;

namespace ServiceStack.WebHost.Endpoints
{
	public abstract class EndpointHostBase
		: IFunqlet, IDisposable
	{
		private readonly ILog log = LogManager.GetLogger(typeof(EndpointHostBase));
		private readonly DateTime startTime;
		private readonly ServiceManager serviceManager;

		public static EndpointHostBase Instance { get; protected set; }

		protected EndpointHostBase()
		{
			this.startTime = DateTime.Now;
			log.Info("Begin Initializing Application...");
		}

		protected EndpointHostBase(string serviceName, params Assembly[] assembliesWithServices)
			: this()
		{
			this.serviceManager = new ServiceManager(assembliesWithServices);

			SetConfig(new EndpointHostConfig {
				ServiceName = serviceName,
				ServiceController = serviceManager.ServiceController,
			});
		}

		public void Init()
		{
			if (Instance != null)
			{
				throw new InvalidDataException("EndpointHostBase.Instance has already been set");
			}

			Instance = this;

			if (this.serviceManager != null)
			{
				serviceManager.Init();
				Configure(serviceManager.Container);
			}
			else
			{
				Configure(null);
			}

			EndpointHost.SetOperationTypes(
				EndpointHost.Config.ServiceController.OperationTypes, 
				EndpointHost.Config.ServiceController.AllOperationTypes
			);

			var elapsed = DateTime.Now - this.startTime;
			log.InfoFormat("Initializing Application took {0}ms", elapsed.TotalMilliseconds);
		}

		public abstract void Configure(Container container);

		public void SetConfig(EndpointHostConfig config)
		{
			EndpointHost.Config = config;

			var contextController = EndpointHost.Config.ServiceController as ServiceControllerContext;
			if (contextController != null)
			{
				contextController.OperationContextFactory = this.CreateOperationContext;
			}
		}

		[Obsolete("Use IService<> instead")]
		protected virtual IOperationContext CreateOperationContext(object requestDto, EndpointAttributes endpointAttributes)
		{
			return new BasicOperationContext<IApplicationContext, RequestContext>(
				ApplicationContext.Instance,
				new RequestContext(requestDto, endpointAttributes));
		}

		public virtual object ExecuteService(object requestDto)
		{
			return ExecuteService(requestDto, EndpointAttributes.None);
		}

		public object ExecuteService(object requestDto, EndpointAttributes endpointAttributes)
		{
			return EndpointHost.Config.ServiceController.Execute(requestDto,
				new RequestContext(requestDto, endpointAttributes));
		}

		public virtual string ExecuteXmlService(string xml)
		{
			return ExecuteXmlService(xml, EndpointAttributes.None);
		}

		public string ExecuteXmlService(string xml, EndpointAttributes endpointAttributes)
		{
			return (string)EndpointHost.Config.ServiceController.ExecuteText(xml,
				new RequestContext(xml, endpointAttributes));
		}

		public virtual void Dispose()
		{
			if (this.serviceManager != null)
			{
				this.serviceManager.Dispose();
			}
		}
	}
}