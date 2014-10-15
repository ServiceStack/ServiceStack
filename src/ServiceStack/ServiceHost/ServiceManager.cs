﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ServiceStack.Logging;
using Funq;
using ServiceStack.Text;

namespace ServiceStack.ServiceHost
{
	public class ServiceManager
		: IDisposable
	{
		private static readonly ILog Log = LogManager.GetLogger(typeof(ServiceManager));

		public Container Container { get; private set; }
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

			this.Container = new Container { DefaultOwner = Owner.External };
            this.Metadata = new ServiceMetadata();
            this.ServiceController = new ServiceController(() => GetAssemblyTypes(assembliesWithServices), this.Metadata);
		}

        public ServiceManager(Container container, params Assembly[] assembliesWithServices)
            : this(assembliesWithServices)
        {
            this.Container = container ?? new Container();
        }

        /// <summary>
        /// Inject alternative container and strategy for resolving Service Types
        /// </summary>
        public ServiceManager(Container container, ServiceController serviceController)
        {
            if (serviceController == null)
                throw new ArgumentNullException("serviceController");

            this.Container = container ?? new Container();
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

		private ContainerResolveCache typeFactory;

		public ServiceManager Init()
		{
			typeFactory = new ContainerResolveCache(this.Container);

			this.ServiceController.Register(typeFactory);

			this.Container.RegisterAutoWiredTypes(this.Metadata.ServiceTypes);

		    return this;
		}

		public void RegisterService<T>()
		{
			if (!typeof(T).IsGenericType
				|| typeof(T).GetGenericTypeDefinition() != typeof(IService<>))
				throw new ArgumentException("Type {0} is not a Web Service that inherits IService<>".Fmt(typeof(T).FullName));

			this.ServiceController.RegisterGService(typeFactory, typeof(T));
			this.Container.RegisterAutoWired<T>();
		}

		public Type RegisterService(Type serviceType)
		{
            var genericServiceType = serviceType.GetTypeWithGenericTypeDefinitionOf(typeof(IService<>));
            try
			{
                if (genericServiceType != null)
                {
                    this.ServiceController.RegisterGService(typeFactory, serviceType);
                    this.Container.RegisterAutoWiredType(serviceType);
                    return genericServiceType;
                }

                var isNService = typeof(IService).IsAssignableFrom(serviceType);
                if (isNService)
                {
                    this.ServiceController.RegisterNService(typeFactory, serviceType);
                    this.Container.RegisterAutoWiredType(serviceType);
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

        /// <summary>
        /// Registers the given service instance to be reused for each request that would route to 
        /// its service type.
        /// </summary>
        /// <remarks>
        /// This method should not be used in production.  Because it reuses a single instance for 
        /// each request, it can't really do dependency injection safely.  This method is only 
        /// useful by the functional testing harness, which self-hosts a ServiceStack with mock 
        /// implementations.
        /// </remarks>
        /// <param name="serviceImplementation">The singleton instance of a service to use.</param>
	    public void RegisterService(IService serviceImplementation)
        {
            var serviceType = serviceImplementation.GetType();

            this.ServiceController.RegisterNService(typeFactory, serviceType);
            
            //Container.Register<TService>(TService instance)
            var containerType = typeof(Container);
            var registrationMethodGeneric = containerType.GetMethods()
                .Where(x => x.Name == "Register")
                .Select(y => new { Method = y, Parameters = y.GetParameters() })
                .Single(z => z.Parameters.Length == 1 && !z.Parameters[0].ParameterType.IsGenericType);
            var registrationMethod = registrationMethodGeneric.Method.MakeGenericMethod(serviceType);
            registrationMethod.Invoke(this.Container, new object[] { serviceImplementation });
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
