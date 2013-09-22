using System.Runtime.Serialization;
using ServiceStack.Server;

namespace ServiceStack.ServiceHost.Tests.Support
{
	[DataContract]
	public class BasicRequest { }

	[DataContract]
	public class BasicRequestResponse { }

	public class BasicService : IService<BasicRequest>
	{
		public object Execute(BasicRequest request)
		{
			return new BasicRequestResponse();
		}
	}
}