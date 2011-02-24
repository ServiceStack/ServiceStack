using System;
using System.ComponentModel;
using System.Runtime.Serialization;
using ServiceStack.Configuration;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceInterface;
using ServiceStack.ServiceInterface.ServiceModel;
using ServiceStack.WebHost.Endpoints;

namespace handler.servicestack.wildcard40
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
	public class HelloResponse : IHasResponseStatus
	{
		[DataMember]
		public string Result { get; set; }

		[DataMember]
		public ResponseStatus ResponseStatus { get; set; }		 
	}

	public class HelloService : ServiceBase<Hello>
	{
		protected override object Run(Hello request)
		{
			return new HelloResponse { Result = "Hello, " + request.Name };
		}
	}

	public class AppHost
		: AppHostBase
	{
		static readonly ConfigurationResourceManager AppSettings = new ConfigurationResourceManager();

		public AppHost()
			: base(AppSettings.GetString("ServiceName") ?? "Host Tests", typeof(HelloService).Assembly) { }

		public override void Configure(Funq.Container container) { }
	}

	public class Global : System.Web.HttpApplication
	{
		protected void Application_Start(object sender, EventArgs e)
		{
			new AppHost().Init();
		}
	}

}
