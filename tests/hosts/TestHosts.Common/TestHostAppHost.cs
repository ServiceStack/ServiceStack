using System.ComponentModel;
using System.Runtime.Serialization;
using ServiceStack.Configuration;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceInterface;
using ServiceStack.ServiceInterface.ServiceModel;
using ServiceStack.WebHost.Endpoints;

namespace TestHosts.Common
{
	[DataContract]
	[Description("ServiceStack's Hello World web service.")]
	[RestService("/hello/{Name*}")]
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

	public class TestHostAppHost
		: AppHostBase
	{
		static readonly ConfigurationResourceManager AppSettings = new ConfigurationResourceManager();

		public TestHostAppHost()
			: base(AppSettings.GetString("ServiceName") ?? "TestHosts", typeof(HelloService).Assembly)
		{
			//EndpointHostConfig.Instance.DebugAspNetHostEnvironment = "WebDev.WebServer";
			//EndpointHostConfig.Instance.DebugOnlyReturnRequestInfo = true;
		}

		public override void Configure(Funq.Container container) { }
	}
}
