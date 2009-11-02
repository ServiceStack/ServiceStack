using System;
using System.Collections.Generic;
using System.Reflection;
using Funq;
using ServiceStack.Logging;

namespace ServiceStack.ServiceHost
{
	public class ServiceManager
		: IServiceResolver, IDisposable
	{
		private static readonly ILog Log = LogManager.GetLogger(typeof (ServiceManager));

		public Container Container { get; private set; }
		public ServiceController ServiceController { get; private set; }
		private readonly Assembly[] assembliesWithServices;

		public ServiceManager(params Assembly[] assembliesWithServices)
		{
			this.assembliesWithServices = assembliesWithServices;
			this.ServiceController = new ServiceController();
		}

		public void Init()
		{
			var typeFactory = new ExpressionFunqlet(this.ServiceController.ServiceTypes);

			this.ServiceController.Register(typeFactory, assembliesWithServices);

			this.Container = new Container();
			typeFactory.Configure(this.Container);
		}

		public void Dispose()
		{
			if (this.Container != null)
			{
				this.Container.Dispose();
			}
		}
	}

}
