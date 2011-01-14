using System.Runtime.Serialization;
using Funq;
using ServiceStack.Configuration;
using ServiceStack.Logging;
using ServiceStack.Logging.Support.Logging;
using ServiceStack.OrmLite;
using ServiceStack.OrmLite.Sqlite;
using ServiceStack.ServiceHost;
using ServiceStack.WebHost.Endpoints.Tests.IntegrationTests;

namespace ServiceStack.WebHost.Endpoints.Tests.Support.Host
{

	[DataContract]
	public class GetFactorial
	{
		[DataMember]
		public long ForNumber { get; set; }
	}

	[DataContract]
	public class GetFactorialResponse
	{
		[DataMember]
		public long Result { get; set; }
	}

	public class GetFactorialService
		: IService<GetFactorial>
	{
		public object Execute(GetFactorial request)
		{
			return new GetFactorialResponse { Result = GetFactorial(request.ForNumber) };
		}

		public static long GetFactorial(long n)
		{
			return n > 1 ? n * GetFactorial(n - 1) : 1;
		}
	}


	public class ExampleAppHost
		: AppHostHttpListenerBase
	{
		private static ILog log;

		public ExampleAppHost()
			: base("ServiceStack Examples", typeof(GetFactorialService).Assembly)
		{
			LogManager.LogFactory = new DebugLogFactory();
			log = LogManager.GetLogger(typeof(ExampleAppHost));
		}

		public override void Configure(Container container)
		{
			//Signal advanced web browsers what HTTP Methods you accept
			base.SetConfig(new EndpointHostConfig
		   {
			   GlobalResponseHeaders =
	           {
				   { "Access-Control-Allow-Origin", "*" },
				   { "Access-Control-Allow-Methods", "GET, POST, PUT, DELETE, OPTIONS" },
               },
			   WsdlServiceNamespace = "http://www.servicestack.net/types",
			   WsdlServiceTypesNamespace = "http://www.servicestack.net/types",
		   });

			container.Register<IResourceManager>(new ConfigurationResourceManager());

			var appSettings = container.Resolve<IResourceManager>();

			container.Register(c => new ExampleConfig(c.Resolve<IResourceManager>()));
			var appConfig = container.Resolve<ExampleConfig>();

			container.Register<IDbConnectionFactory>(c =>
				new OrmLiteConnectionFactory(
					":memory:",
					SqliteOrmLiteDialectProvider.Instance));
		}
	}


}