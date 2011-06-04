using System;
using System.Linq;
using System.Reflection;
using Funq;

namespace ServiceStack.ServiceHost
{
	public class ServiceManager
		: IDisposable
	{
		//private static readonly ILog Log = LogManager.GetLogger(typeof (ServiceManager));

		public Container Container { get; private set; }
		public ServiceController ServiceController { get; private set; }

		public ServiceOperations ServiceOperations { get; set; }
		public ServiceOperations AllServiceOperations { get; set; }

		public ServiceManager(params Assembly[] assembliesWithServices)
		{
			if (assembliesWithServices == null || assembliesWithServices.Length == 0)
				throw new ArgumentException(
					"No Assemblies provided in your AppHost's base constructor.\n"
					+ "To register your services, please provide the assemblies where your web services are defined.");

			this.Container = new Container();
			this.ServiceController = new ServiceController(
				() => assembliesWithServices.ToList().SelectMany(x => x.GetTypes()));
		}

		public ServiceManager(bool autoInitialize, params Assembly[] assembliesWithServices)
			: this(assembliesWithServices)
		{
			if (autoInitialize)
			{
				this.Init();
			}
		}

		/// <summary>
		/// Inject alternative container and strategy for resolving Service Types
		/// </summary>
		public ServiceManager(Container container, ServiceController serviceController)
		{
			if (serviceController == null)
				throw new ArgumentNullException("serviceController");

			this.Container = container ?? new Container();
			this.ServiceController = serviceController;
		}

		public void Init()
		{
			var typeFactory = new ExpressionTypeFunqContainer(this.Container);

			this.ServiceController.Register(typeFactory);

			this.ServiceOperations = new ServiceOperations(this.ServiceController.OperationTypes);
			this.AllServiceOperations = new ServiceOperations(this.ServiceController.AllOperationTypes);

			typeFactory.RegisterTypes(this.ServiceController.ServiceTypes);
		}
	
		public object Execute(object dto)
		{
			return this.ServiceController.Execute(dto, null);
		}

		public void Dispose()
		{
			if (this.Container != null)
			{
				this.Container.Dispose();
			}
		}

		public void AfterInit()
		{
			this.ServiceController.AfterInit();
		}
	}

}
