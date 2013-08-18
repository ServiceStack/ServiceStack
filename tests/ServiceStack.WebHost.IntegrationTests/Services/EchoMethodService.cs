using System.Runtime.Serialization;
using ServiceStack.Common.Web;
using ServiceStack.ServiceHost;

namespace ServiceStack.WebHost.IntegrationTests.Services
{
	[Route("/echomethod")]
	public class EchoMethod
	{
	}

	[DataContract]
	public class EchoMethodResponse
	{
		[DataMember]
		public string Result { get; set; }
	}

    [DefaultRequest(typeof(EchoMethod))]
	public class EchoMethodService : ServiceInterface.Service
	{
		public object Get(EchoMethod request)
		{
			return new EchoMethodResponse { Result = HttpMethods.Get };
		}

		public object Post(EchoMethod request)
		{
			return new EchoMethodResponse { Result = HttpMethods.Post };
		}

		public object Put(EchoMethod request)
		{
			return new EchoMethodResponse { Result = HttpMethods.Put };
		}

		public object Delete(EchoMethod request)
		{
			return new EchoMethodResponse { Result = HttpMethods.Delete };
		}

		public object Patch(EchoMethod request)
		{
			return new EchoMethodResponse { Result = HttpMethods.Patch };
		}
	}
}