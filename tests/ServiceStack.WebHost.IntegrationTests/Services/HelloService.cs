using System.ComponentModel;
using System.Runtime.Serialization;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceInterface;

namespace ServiceStack.WebHost.IntegrationTests.Services
{
	[DataContract]
	[Description("ServiceStack's Hello World web service.")]
	[Route("/hello")]
	[Route("/hello/{Name}")]
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

	public class TestFilterAttribute : ResponseFilterAttribute
	{
		public override void Execute(IHttpRequest req, IHttpResponse res, object requestDto)
		{
		}
	}

	[Route("/hello2")]
	[Route("/hello2/{Name}")]
	public class Hello2
	{
		[DataMember]
		public string Name { get; set; }
	}

	[DataContract]
	public class Hello2Response
	{
		[DataMember]
		public string Result { get; set; }
	}

	[TestFilter]
	public class Hello2Service : IService<Hello2>
	{
		public object Execute(Hello2 request)
		{
			return request.Name;
		}
	}


}