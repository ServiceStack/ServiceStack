using System;
using Funq;
using NUnit.Framework;
using ServiceStack.Configuration;
using ServiceStack.Data;
using ServiceStack.Logging;
using ServiceStack.OrmLite;
using ServiceStack.OrmLite.Sqlite;
using ServiceStack.WebHost.Endpoints.Tests.Support.Host;

namespace ServiceStack.WebHost.Endpoints.Tests.IntegrationTests
{
	public class IntegrationTestBase
	{
		protected const string BaseUrl = "http://localhost:1337/";

        private readonly IntegrationTestAppHost appHost;
	    public IntegrationTestBase()
	    {
            appHost = new IntegrationTestAppHost();
	        appHost.Init();
            appHost.Start(BaseUrl);
        }

        [OneTimeTearDown]
        public void TestFixtureTearDown()
        {
            appHost.Dispose();
        }

	    //Fiddler can debug local HTTP requests when using the hostname
		//private const string BaseUrl = "http://io:8081/";

		public class IntegrationTestAppHost : AppHostHttpListenerBase 
		{
            public IntegrationTestAppHost()
                : base("ServiceStack Examples", typeof(RestMovieService).Assembly)
            {
                LogManager.LogFactory = new DebugLogFactory();
            }

            public override void Configure(Container container)
            {
#if NETFRAMEWORK
                Plugins.Add(new SoapFormat());
#endif
                container.Register<IAppSettings>(new AppSettings());

                container.Register(c => new ExampleConfig(c.Resolve<IAppSettings>()));
                //var appConfig = container.Resolve<ExampleConfig>();

                container.Register<IDbConnectionFactory>(c =>
                     new OrmLiteConnectionFactory(":memory:", SqliteDialect.Provider));

                Routes.Add<Movies>("/custom-movies", "GET")
                      .Add<Movies>("/custom-movies/genres/{Genre}")
                      .Add<Movie>("/custom-movies", "POST,PUT")
                      .Add<Movie>("/custom-movies/{Id}");

                ConfigureDatabase.Init(container.Resolve<IDbConnectionFactory>());
            }
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

		public void DeleteOnEachEndpoint<TRes>(string relativePathOrAbsoluteUri, Action<TRes> validate)
		{
			using (var xmlClient = new XmlServiceClient(BaseUrl))
			using (var jsonClient = new JsonServiceClient(BaseUrl))
			using (var jsvClient = new JsvServiceClient(BaseUrl))
			{
				var xmlResponse = xmlClient.Delete<TRes>(relativePathOrAbsoluteUri);
				if (validate != null) validate(xmlResponse);

				var jsonResponse = jsonClient.Delete<TRes>(relativePathOrAbsoluteUri);
				if (validate != null) validate(jsonResponse);

				var jsvResponse = jsvClient.Delete<TRes>(relativePathOrAbsoluteUri);
				if (validate != null) validate(jsvResponse);
			}
		}
	}
}
