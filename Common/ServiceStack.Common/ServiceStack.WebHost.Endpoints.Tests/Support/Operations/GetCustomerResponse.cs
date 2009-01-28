using System.Runtime.Serialization;
using ServiceStack.WebHost.Endpoints.Tests.Support.Types;

namespace ServiceStack.WebHost.Endpoints.Tests.Support.Operations
{
	[DataContract]
	public class GetCustomerResponse
	{
		[DataMember]
		public Customer Customer { get; set; }
	}
}