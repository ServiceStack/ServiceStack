using System.Runtime.Serialization;

namespace ServiceStack.ServiceHost.Tests.Support
{
	[DataContract]
	public class BasicRequest { }

	public class BasicService : IService<BasicRequest>
	{
		public object Execute(BasicRequest request)
		{
			return new BasicRequest();
		}
	}
}