using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;
using ServiceStack.Common.Utils;
using ServiceStack.Configuration;
using ServiceStack.OrmLite;
using ServiceStack.OrmLite.Sqlite;
using ServiceStack.ServiceHost;
using ServiceStack.WebHost.Endpoints;
using ServiceStack.WebHost.Endpoints.Support;

namespace MasterHost
{
	[DataContract]
	[Description("ServiceStack's Hello World web service.")]
	[RestService("/hello")]
	[RestService("/hello/{Name}")]
	[RestService("/hi/{Name}")]
	public class Hello
	{
		[DataMember]
		public string Name { get; set; }
	}

	[DataContract]
	public class HelloResponse
	{
		[DataMember]
		public string Result { get; set; }
	}

	public class HelloService : IService<Hello>
	{
		public object Execute(Hello request)
		{
			return new HelloResponse { Result = "Hello, " + request.Name };
		}
	}

	public class AppConfig
	{
		public AppConfig() {}

		public AppConfig(IResourceManager appSettings)
		{
			this.RunOnBaseUrl = appSettings.GetString("RunOnBaseUrl");
			this.TestPaths = appSettings.GetList("TestPaths");

			this.HandlerHosts = appSettings.GetList("HandlerHosts");
			this.HandlerHostNames = appSettings.GetList("HandlerHostNames");
		}

		public string RunOnBaseUrl { get; set; }
		public IList<string> TestPaths { get; set; }

		public IList<string> HandlerHosts { get; set; }
		public IList<string> HandlerHostNames { get; set; }
	}

	public class AppHost : AppHostBase
	{
		public AppHost()
			: base("Master Test Host", typeof(HelloService).Assembly) { }

		public override void Configure(Funq.Container container)
		{
			SetConfig(new EndpointHostConfig { DebugMode = true });

			container.Register<IDbConnectionFactory>(c =>
				new OrmLiteConnectionFactory(
					"~/reports.sqlite".MapHostAbsolutePath(),
					SqliteOrmLiteDialectProvider.Instance));

			container.Register(new AppConfig(new ConfigurationResourceManager()));

			//Create Report table if not exists
			container.Resolve<IDbConnectionFactory>().Exec(dbCmd =>
			{
				dbCmd.CreateTable<Report>(false);
				dbCmd.CreateTable<RequestInfoResponse>(false);
			});
		}
	}

	public class Global : System.Web.HttpApplication
	{
		protected void Application_Start(object sender, EventArgs e)
		{
			new AppHost().Init();
		}
	}
}

