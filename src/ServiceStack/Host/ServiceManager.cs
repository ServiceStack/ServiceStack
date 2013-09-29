using System;
using System.Collections.Generic;
using System.Reflection;
using ServiceStack.Logging;
using Funq;
using ServiceStack.Text;

namespace ServiceStack.Host
{
	public class ServiceManager : IDisposable
	{
		private static readonly ILog Log = LogManager.GetLogger(typeof(ServiceManager));

		public Container Container { get; private set; }
		public ServiceController ServiceController { get; private set; }
        public ServiceMetadata Metadata { get; internal set; }

        public ServiceManager(Container container, params Assembly[] assembliesWithServices)
		{
			if (assembliesWithServices == null || assembliesWithServices.Length == 0)
				throw new ArgumentException(
					"No Assemblies provided in your AppHost's base constructor.\n"
					+ "To register your services, please provide the assemblies where your web services are defined.");

			this.Container = container ?? new Container();
            this.Container.DefaultOwner = Owner.External;
            this.Metadata = new ServiceMetadata();
            this.ServiceController = new ServiceController(() => GetAssemblyTypes(assembliesWithServices), this.Metadata);
		}

        /// <summary>
        /// Inject alternative container and strategy for resolving Service Types
        /// </summary>
        public ServiceManager(Container container, ServiceController serviceController)
        {
            if (serviceController == null)
                throw new ArgumentNullException("serviceController");

            this.Container = container ?? new Container();
            this.Container.DefaultOwner = Owner.External;
            this.Metadata = serviceController.Metadata; //always share the same metadata
            this.ServiceController = serviceController;

            typeFactory = new ContainerResolveCache(this.Container);
        }

		private List<Type> GetAssemblyTypes(Assembly[] assembliesWithServices)
		{
			var results = new List<Type>();
			string assemblyName = null;
			string typeName = null;

			try
			{
				foreach (var assembly in assembliesWithServices)
				{
					assemblyName = assembly.FullName;
					foreach (var type in assembly.GetTypes())
					{
						typeName = type.Name;
						results.Add(type);
					}
				}
				return results;
			}
			catch (Exception ex)
			{
				var msg = string.Format("Failed loading types, last assembly '{0}', type: '{1}'", assemblyName, typeName);
				Log.Error(msg, ex);
				throw new Exception(msg, ex);
			}
		}

		private ContainerResolveCache typeFactory;

		public ServiceManager Init()
		{
			typeFactory = new ContainerResolveCache(this.Container);

			this.ServiceController.Register(typeFactory);

			this.Container.RegisterAutoWiredTypes(this.Metadata.ServiceTypes);

		    return this;
		}

		public void RegisterService(Type serviceType)
		{
            try
			{
                var isNService = typeof(IService).IsAssignableFrom(serviceType);
                if (isNService)
                {
                    this.ServiceController.RegisterService(typeFactory, serviceType);
                    this.Container.RegisterAutoWiredType(serviceType);
                }

                throw new ArgumentException("Type {0} is not a Web Service that inherits IService".Fmt(serviceType.FullName));
            }
			catch (Exception ex)
			{
				Log.Error(ex);
			}
		}

		public object Execute(object dto)
		{
			return this.ServiceController.Execute(dto, new BasicRequestContext());
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
