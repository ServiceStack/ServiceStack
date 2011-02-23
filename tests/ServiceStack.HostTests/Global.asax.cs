using System;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.Serialization;
using ServiceStack.ServiceHost;
using ServiceStack.WebHost.Endpoints;

namespace ServiceStack.HostTests
{
	[DataContract]
	[Description("ServiceStack's Hello World web service.")]
	[RestService("/hello/{Name}")]
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

	public class AppHost 
		: AppHostBase
	{
		public AppHost()
			: base("Host Tests", typeof(HelloService).Assembly) { }

		public override void Configure(Funq.Container container)
		{
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
