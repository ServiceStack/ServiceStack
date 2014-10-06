using System;
using System.Collections.Generic;
using System.Reflection;
using ServiceStack.DependencyInjection;
using ServiceStack.Logging;
using ServiceStack.Text;

namespace ServiceStack.ServiceHost
{
	public class ServiceManager
		: IDisposable
	{
		private static readonly ILog Log = LogManager.GetLogger(typeof(ServiceManager));

		public DependencyService DependencyService { get; private set; }
		public ServiceController ServiceController { get; private set; }
        public ServiceMetadata Metadata { get; internal set; }

        //public ServiceOperations ServiceOperations { get; set; }
        //public ServiceOperations AllServiceOperations { get; set; }

		public ServiceManager(params Assembly[] assembliesWithServices)
		{
			if (assembliesWithServices == null || assembliesWithServices.Length == 0)
				throw new ArgumentException(
					"No Assemblies provided in your AppHost's base constructor.\n"
					+ "To register your services, please provide the assemblies where your web services are defined.");

		    this.DependencyService = new DependencyService();
            this.Metadata = new ServiceMetadata();
            this.ServiceController = new ServiceController(() => GetAssemblyTypes(assembliesWithServices), this.Metadata);
		}

        public ServiceManager(DependencyService dependencyService, params Assembly[] assembliesWithServices)
            : this(assembliesWithServices)
        {
            this.DependencyService = dependencyService ?? new DependencyService();
        }

        /// <summary>
        /// Inject alternative dependencyInjector and strategy for resolving Service Types
        /// </summary>
        public ServiceManager(DependencyService dependencyService, ServiceController serviceController)
        {
            if (serviceController == null)
                throw new ArgumentNullException("serviceController");

            this.DependencyService = dependencyService ?? new DependencyService();
            this.Metadata = serviceController.Metadata; //always share the same metadata
            this.ServiceController = serviceController;
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

        private DependencyInjectorResolveCache typeFactory;

		public ServiceManager Init()
		{

            typeFactory = new DependencyInjectorResolveCache(this.DependencyService);

		    this.ServiceController.Register(typeFactory);

			this.DependencyService.RegisterAutoWiredTypes(this.Metadata.ServiceTypes);

		    return this;
		}

		public void RegisterService<T>()
		{
			if (!typeof(T).IsGenericType
				|| typeof(T).GetGenericTypeDefinition() != typeof(IService<>))
				throw new ArgumentException("Type {0} is not a Web Service that inherits IService<>".Fmt(typeof(T).FullName));

			this.ServiceController.RegisterGService(typeFactory, typeof(T));
			this.DependencyService.RegisterAutoWired<T>();
		}

		public Type RegisterService(Type serviceType)
		{
            var genericServiceType = serviceType.GetTypeWithGenericTypeDefinitionOf(typeof(IService<>));
            try
			{
                if (genericServiceType != null)
                {
                    this.ServiceController.RegisterGService(typeFactory, serviceType);
                    this.DependencyService.RegisterAutoWiredType(serviceType);
                    return genericServiceType;
                }

                var isNService = typeof(IService).IsAssignableFrom(serviceType);
                if (isNService)
                {
                    this.ServiceController.RegisterNService(typeFactory, serviceType);
                    this.DependencyService.RegisterAutoWiredType(serviceType);
                    return null;
                }

                throw new ArgumentException("Type {0} is not a Web Service that inherits IService<> or IService".Fmt(serviceType.FullName));
            }
			catch (Exception ex)
			{
				Log.Error(ex);
			    return genericServiceType;
			}
		}

		public object Execute(object dto)
		{
			return this.ServiceController.Execute(dto, null);
		}

		public void Dispose()
		{
		}

		public void AfterInit()
		{
			this.ServiceController.AfterInit();
		}
	}

}
