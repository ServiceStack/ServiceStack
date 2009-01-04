using System.Collections.Generic;
using NUnit.Framework;
using Sakila.ServiceModel.Version100.Types;
using ServiceStack.Configuration;
using ServiceStack.DataAccess;
using ServiceStack.DataAccess.Db4oProvider;
using ServiceStack.SakilaDb4o.ServiceInterface;
using ServiceStack.ServiceInterface;

namespace ServiceStack.SakilaDb4o.Tests.Integration.Support
{
	public class IntegrationTestBase
	{
		public IntegrationTestBase()
		{
			this.ServiceController = new ServiceController(new ServiceResolver());
			this.Config = new AppConfig();
			this.ProviderManager = new Db4oFileProviderManager(this.Config.ConnectionString);
			this.AppContext = new OperationContext {
				Resources = new ConfigurationResourceManager(),
				Factory = new FactoryProvider(null, this.ProviderManager)
			};
		}

		public ushort CustomerId { get { return 1; } }

		protected List<Customer> Customers { get; set; }

		protected List<int> CustomerIds
		{
			get { return this.Customers.ConvertAll(x => x.Id); }
		}

		protected AppConfig Config { get; private set; }
		protected OperationContext AppContext { get; private set; }
		protected IPersistenceProviderManager ProviderManager { get; private set; }
		protected ServiceController ServiceController { get; private set; }

		protected object ExecuteService(object requestDto)
		{
			var context = new CallContext(this.AppContext, new RequestContext(requestDto, new FactoryProvider(null)));
			return this.ServiceController.Execute(context);
		}

	}
}