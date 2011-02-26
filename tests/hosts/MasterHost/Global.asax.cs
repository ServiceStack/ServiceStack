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

			this.RootPorts = appSettings.GetList("RootPorts");
			this.ApiPorts = appSettings.GetList("ApiPorts");
			this.ServiceStackPorts = appSettings.GetList("ServiceStackPorts");

			this.AllPaths = appSettings.GetList("AllPaths");

			this.RootUrlTests = appSettings.GetList("RootUrlTests");
			this.ApiUrlTests = appSettings.GetList("ApiUrlTests");
			this.ServiceStackUrlTests = appSettings.GetList("ServiceStackUrlTests");

			var allPorts = new List<string>();
			allPorts.AddRange(this.RootPorts);
			allPorts.AddRange(this.ApiPorts);
			allPorts.AddRange(this.ServiceStackPorts);
			this.AllPorts = allPorts;
		}

		public IList<string> GetPathsForPath(string virtualPath)
		{
			var vpath = virtualPath.ToLower();
			if (vpath.Contains("all"))
				return this.RootUrlTests;
			if (vpath.Contains("api"))
				return this.ApiUrlTests;
			if (vpath.Contains("servicestack"))
				return this.ServiceStackUrlTests;

			throw new ArgumentException("Unrecognized path: " + virtualPath);
		}

		public IList<string> GetPathsForPort(string portNo)
		{
			if (RootPorts.Contains(portNo))
				return this.RootUrlTests;
			if (ApiPorts.Contains(portNo))
				return this.ApiUrlTests;
			if (ServiceStackPorts.Contains(portNo))
				return this.ServiceStackUrlTests;

			throw new ArgumentException("Unrecognized port: " + portNo);
		}

		public IList<string> AllPaths { get; set; }

		public IList<string> AllPorts { get; set; }
		public IList<string> RootPorts { get; set; }
		public IList<string> ApiPorts { get; set; }
		public IList<string> ServiceStackPorts { get; set; }

		public string PortsHostEnvironment { get; set; }
		public string PathsHostEnvironment { get; set; }
		public string RunOnBaseUrl { get; set; }

		public IList<string> RootUrlTests { get; set; }
		public IList<string> ApiUrlTests { get; set; }
		public IList<string> ServiceStackUrlTests { get; set; }

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

