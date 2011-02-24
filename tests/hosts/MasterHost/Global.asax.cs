using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;
using ServiceStack.Common.Utils;
using ServiceStack.Configuration;
using ServiceStack.OrmLite;
using ServiceStack.OrmLite.Sqlite;
using ServiceStack.ServiceHost;
using ServiceStack.WebHost.Endpoints;
using ServiceStack.WebHost.Endpoints.Formats;

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
		public AppConfig(IResourceManager appSettings)
		{
			this.PortsHostEnvironment = appSettings.GetString("PortsHostEnvironment");
			this.PathsHostEnvironment = appSettings.GetString("PathsHostEnvironment");
			this.RunOnBaseUrl = appSettings.GetString("RunOnBaseUrl");
			this.TestPathComponents = appSettings.GetList("TestPathComponents");
			this.RunOnPorts = appSettings.GetList("RunOnPorts");
			this.RunOnPaths = appSettings.GetList("RunOnPaths");
		}

		public string PortsHostEnvironment { get; set; }
		public string PathsHostEnvironment { get; set; }
		public string RunOnBaseUrl { get; set; }
		public IList<string> TestPathComponents { get; set; }
		public IList<string> RunOnPorts { get; set; }
		public IList<string> RunOnPaths { get; set; }
	}

	public class AppHost
		: AppHostBase
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
				dbCmd.CreateTable<Report>(false));
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

