using System.Runtime.Serialization;
using Funq;
using ServiceStack.ServiceHost;

namespace ServiceStack.WebHost.Endpoints.Tests.Support.Host
{
	[DataContract]
	[Route("/login/{UserName}/{Password}")]
	public class BclDto
	{
		[DataMember(Name = "uname")]
		public string UserName { get; set; }

		[DataMember(Name = "pwd")]
		public string Password { get; set; }
	}

	[DataContract]
	public class BclDtoResponse
	{
		[DataMember(Name = "uname")]
		public string UserName { get; set; }

		[DataMember(Name = "pwd")]
		public string Password { get; set; }
	}

	public class BclDtoService : ServiceInterface.Service
	{
	    public object Any(BclDto request)
		{
			return new BclDtoResponse
			{
				UserName = request.UserName,
				Password = request.Password
			};
		}
	}

	public class TestConfigAppHostHttpListener
		: AppHostHttpListenerBase
	{
		public TestConfigAppHostHttpListener()
			: base("TestConfigAppHost Service", typeof(BclDto).Assembly)
		{
		}

		public override void Configure(Container container)
		{
			SetConfig(new EndpointHostConfig
			{
				UseBclJsonSerializers = true,
			});
		}
	}
}