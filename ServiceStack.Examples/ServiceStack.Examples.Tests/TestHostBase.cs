using System;
using Funq;
using Moq;
using NUnit.Framework;
using ServiceStack.Configuration;
using ServiceStack.DataAccess;
using ServiceStack.Examples.ServiceInterface;
using ServiceStack.Examples.ServiceInterface.Support;
using ServiceStack.Logging;
using ServiceStack.Logging.Support.Logging;
using ServiceStack.OrmLite;
using ServiceStack.OrmLite.Sqlite;
using ServiceStack.ServiceHost;
using ServiceStack.WebHost.Endpoints;

namespace ServiceStack.Examples.Tests
{
	[TestFixture]
	public class TestHostBase
		: AppHostBase
	{
		protected const string InMemoryDb = ":memory:";
		private static ILog log;

		public TestHostBase()
			: base("TestAppHost", typeof(GetFactorialService).Assembly)
		{
			LogManager.LogFactory = new ConsoleLogFactory();
			log = LogManager.GetLogger(GetType());

			Instance = null;
			Init();
		}

		protected IDbConnectionFactory ConnectionFactory
		{
			get
			{
				return Instance.Container.Resolve<IDbConnectionFactory>();
			}
		}		

		public override void Configure(Container container)
		{
			container.Register<IResourceManager>(c => new ConfigurationResourceManager());

			container.Register<IDbConnectionFactory>(c =>
				new OrmLiteConnectionFactory(
					InMemoryDb,
					false,
					SqliteOrmLiteDialectProvider.Instance));

			ConfigureDatabase.Init(container.Resolve<IDbConnectionFactory>());

			log.InfoFormat("TestAppHost Created: " + DateTime.Now);
		}

		protected IPersistenceProviderManager GetMockProviderManagerObject(Mock<IQueryablePersistenceProvider> mockPersistence)
		{
			var mockProviderManager = new Mock<IPersistenceProviderManager>();
			mockProviderManager.Expect(x => x.GetProvider()).Returns(mockPersistence.Object);
			return mockProviderManager.Object;
		}
		
		/// <summary>
		/// Process a webservice in-memory
		/// </summary>
		/// <typeparam name="TResponse"></typeparam>
		/// <param name="request"></param>
		/// <returns></returns>
		public TResponse Send<TResponse>(object request)
		{
			return Send<TResponse>(request, EndpointAttributes.None);
		}

		public TResponse Send<TResponse>(object request, EndpointAttributes endpointAttrs)
		{
			return (TResponse)this.ServiceController.Execute(request,
				new HttpRequestContext(request, endpointAttrs));
		}
	}
}