using System;
using System.Collections.Generic;
using System.Reflection;
using Funq;
using ServiceStack.Logging;

namespace ServiceStack.ServiceHost
{
	public class ServiceManager
		: IDisposable
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
			this.Container = new Container();

			var typeFactory = new ExpressionTypeFunqContainer(this.Container);

			this.ServiceController.Register(typeFactory, assembliesWithServices);

			typeFactory.RegisterTypes(this.ServiceController.ServiceTypes);
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
