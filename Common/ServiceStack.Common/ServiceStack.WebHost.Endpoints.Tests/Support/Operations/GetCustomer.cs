using System.Runtime.Serialization;

namespace ServiceStack.WebHost.Endpoints.Tests.Support.Operations
{
	[DataContract]
	public class GetCustomer
	{
		[DataMember]
		public long CustomerId { get; set; }
	}
}
