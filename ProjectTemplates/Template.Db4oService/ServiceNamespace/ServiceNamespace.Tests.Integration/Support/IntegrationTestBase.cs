using System.Collections.Generic;
using NUnit.Framework;
using @ServiceModelNamespace@.Version100.Types;
using ServiceStack.Configuration;
using ServiceStack.DataAccess;
using ServiceStack.DataAccess.Db4oProvider;
using @ServiceNamespace@.ServiceInterface;
using ServiceStack.ServiceInterface;

namespace @ServiceNamespace@.Tests.Integration.Support
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

		public ushort @ModelName@Id { get { return 1; } }

		protected List<@ModelName@> @ModelName@s { get; set; }

		protected List<int> @ModelName@Ids
		{
			get { return this.@ModelName@s.ConvertAll(x => (int)x.Id); }
		}

		protected AppConfig Config { get; private set; }
		protected OperationContext AppContext { get; private set; }
		protected IPersistenceProviderManager ProviderManager { get; private set; }
		protected ServiceController ServiceController { get; private set; }

		[TestFixtureTearDown]
		public void FixtureTearDown()
		{
		}

		protected object ExecuteService(object requestDto)
		{
			var context = new CallContext(this.AppContext, new RequestContext(requestDto, new FactoryProvider(null)));
			return this.ServiceController.Execute(context);
		}

	}
}