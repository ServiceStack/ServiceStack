using System.Runtime.Serialization;

namespace ServiceStack.ServiceInterface.Tests.Support.Version100.Operations
{
	[DataContract]
	public class GetCustomer
	{
		[DataMember]
		public long CustomerId { get; set; }
	}
}