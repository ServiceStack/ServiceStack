using System;
using Funq;
using ServiceStack.Configuration;
using ServiceStack.Logging;
using ServiceStack.Logging.Support.Logging;
using ServiceStack.OrmLite;
using ServiceStack.OrmLite.Sqlite;
using ServiceStack.ServiceClient.Web;

namespace ServiceStack.WebHost.Endpoints.Tests.IntegrationTests
{
	public class IntegrationTestBase
		: AppHostHttpListenerBase
	{
		private const string BaseUrl = "http://localhost:8080/";
		
		//Fiddler can debug local HTTP requests when using the hostname
		//private const string BaseUrl = "http://io:8080/";

		private static ILog log;

		public IntegrationTestBase()
			: base("ServiceStack Examples", typeof(MovieRestService).Assembly)
		{
			LogManager.LogFactory = new DebugLogFactory();
			log = LogManager.GetLogger(GetType());
			Instance = null;

			Init();
			try
			{
				Start(BaseUrl);
			}
			catch (Exception ex)
			{
				Console.WriteLine("Error trying to run ConsoleHost: " + ex.Message);
			}
		}

		public override void Configure(Container container)
		{
			container.Register<IResourceManager>(new ConfigurationResourceManager());

			container.Register(c => new ExampleConfig(c.Resolve<IResourceManager>()));
			var appConfig = container.Resolve<ExampleConfig>();

			container.Register<IDbConnectionFactory>(c =>
				 new OrmLiteConnectionFactory(
					":memory:",			//Use an in-memory database instead
					false,				//keep the same in-memory db connection open
					SqliteOrmLiteDialectProvider.Instance));

			ConfigureDatabase.Init(container.Resolve<IDbConnectionFactory>());
		}

		public void SendToEachEndpoint<TRes>(object request, Action<TRes> validate)
		{
			SendToEachEndpoint(request, null, validate);
		}

		/// <summary>
		/// Run the request against each Endpoint
		/// </summary>
		/// <typeparam name="TRes"></typeparam>
		/// <param name="request"></param>
		/// <param name="validate"></param>
		/// <param name="httpMethod"></param>
		public void SendToEachEndpoint<TRes>(object request, string httpMethod, Action<TRes> validate)
		{
			using (var xmlClient = new XmlServiceClient(BaseUrl))
			using (var jsonClient = new JsonServiceClient(BaseUrl))
			using (var jsvClient = new JsvServiceClient(BaseUrl))
			{
				xmlClient.HttpMethod = httpMethod;
				jsonClient.HttpMethod = httpMethod;
				jsvClient.HttpMethod = httpMethod;

				var xmlResponse = xmlClient.Send<TRes>(request);
				if (validate != null) validate(xmlResponse);

				var jsonResponse = jsonClient.Send<TRes>(request);
				if (validate != null) validate(jsonResponse);

				var jsvResponse = jsvClient.Send<TRes>(request);
				if (validate != null) validate(jsvResponse);
			}
		}

	}
}