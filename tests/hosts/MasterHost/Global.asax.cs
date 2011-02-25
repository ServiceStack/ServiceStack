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

			this.AllPaths = appSettings.GetList("AllPaths");
			this.ApiPaths = appSettings.GetList("ApiPaths");
			this.ServiceStackPaths = appSettings.GetList("ServiceStackPaths");

			this.AllPorts = appSettings.GetList("AllPorts");
			this.ApiPorts = appSettings.GetList("ApiPorts");
			this.ServiceStackPorts = appSettings.GetList("ServiceStackPorts");

			this.RunOnPorts = appSettings.GetList("RunOnPorts");
			this.RunOnPaths = appSettings.GetList("RunOnPaths");
		}

		public IList<string> GetPathsForPath(string virtualPath)
		{
			var vpath = virtualPath.ToLower();
			if (vpath.Contains("all"))
				return this.AllPaths;
			if (vpath.Contains("api"))
				return this.ApiPaths;
			if (vpath.Contains("servicestack"))
				return this.ServiceStackPaths;

			throw new ArgumentException("Unrecognized path: " + virtualPath);
		}

		public IList<string> GetPathsForPort(string portNo)
		{
			if (AllPorts.Contains(portNo))
				return this.AllPaths;
			if (ApiPorts.Contains(portNo))
				return this.ApiPaths;
			if (ServiceStackPorts.Contains(portNo))
				return this.ServiceStackPaths;

			throw new ArgumentException("Unrecognized port: " + portNo);
		}

		public IList<string> AllPorts { get; set; }
		public IList<string> ApiPorts { get; set; }
		public IList<string> ServiceStackPorts { get; set; }

		public string PortsHostEnvironment { get; set; }
		public string PathsHostEnvironment { get; set; }
		public string RunOnBaseUrl { get; set; }

		public IList<string> AllPaths { get; set; }
		public IList<string> ApiPaths { get; set; }
		public IList<string> ServiceStackPaths { get; set; }

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

