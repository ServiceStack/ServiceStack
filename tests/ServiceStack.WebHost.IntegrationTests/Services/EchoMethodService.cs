using System;
using System.Runtime.Serialization;
using ServiceStack.Common.Web;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceInterface;

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

	public class EchoMethodService
		: RestServiceBase<EchoMethod>
	{
		public override object OnGet(EchoMethod request)
		{
			return new EchoMethodResponse { Result = HttpMethods.Get };
		}

		public override object OnPost(EchoMethod request)
		{
			return new EchoMethodResponse { Result = HttpMethods.Post };
		}

		public override object OnPut(EchoMethod request)
		{
			return new EchoMethodResponse { Result = HttpMethods.Put };
		}

		public override object OnDelete(EchoMethod request)
		{
			return new EchoMethodResponse { Result = HttpMethods.Delete };
		}

		public override object OnPatch(EchoMethod request)
		{
			return new EchoMethodResponse { Result = HttpMethods.Patch };
		}
	}
}