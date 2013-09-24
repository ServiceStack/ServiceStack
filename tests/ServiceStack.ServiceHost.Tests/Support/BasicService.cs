using System.Runtime.Serialization;

namespace ServiceStack.ServiceHost.Tests.Support
{
	[DataContract]
	public class BasicRequest { }

	[DataContract]
	public class BasicRequestResponse { }

	public class BasicService : IService
	{
		public object Any(BasicRequest request)
		{
			return new BasicRequestResponse();
		}
	}
}