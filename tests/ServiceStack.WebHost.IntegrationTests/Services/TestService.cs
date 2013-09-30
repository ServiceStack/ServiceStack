using System.ComponentModel;
using System.Runtime.Serialization;

namespace ServiceStack.WebHost.IntegrationTests.Services
{
	[DataContract]
	[Description("ServiceStack's Test World web service.")]
	[Route("/test")]
	[Route("/test/{Name}")]
	public class Test
	{
		[DataMember]
		public string Name { get; set; }
	}

	[DataContract]
	public class TestResponse
	{
		[DataMember]
		public string Result { get; set; }
	}

	public class TestService : IService
	{
		public object Any(Test request)
		{
			var client = new Soap12ServiceClient("http://localhost/ServiceStack.WebHost.IntegrationTests/api/");
			var response = client.Send<HelloResponse>(new Hello { Name = request.Name });
			return new TestResponse { Result = response.Result };
		}
	}

}