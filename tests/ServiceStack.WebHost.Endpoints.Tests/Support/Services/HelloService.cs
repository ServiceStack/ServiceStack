using System.Runtime.Serialization;
using ServiceStack.ServiceHost;

namespace ServiceStack.WebHost.Endpoints.Tests.Support.Services
{
	/// Create the name of your Web Service (i.e. the Request DTO)
	[DataContract]
	[Route("/hello")] //Optional: Define an alternate REST-ful url for this service
	[Route("/hello/{Name}")]
	public class Hello
	{
		[DataMember]
		public string Name { get; set; }
	}

	/// Define your Web Service response (i.e. Response DTO)
	[DataContract]
	public class HelloResponse
	{
		[DataMember]
		public string Result { get; set; }
	}

	/// Create your Web Service implementation 
	public class HelloService : IService<Hello>
	{
		public object Execute(Hello request)
		{
			return new HelloResponse { Result = "Hello, " + request.Name };
		}
	}

}