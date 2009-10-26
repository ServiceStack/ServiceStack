using System.Runtime.Serialization;
using ServiceStack.ServiceInterface.Tests.Support.Version100.Types;

namespace ServiceStack.ServiceInterface.Tests.Support.Version100.Operations
{
	[DataContract]
	public class GetCustomerGeneric
	{
		[DataMember]
		public long CustomerId { get; set; }
	}

	[DataContract]
	public class GetCustomerGenericResponse
	{
		[DataMember]
		public Customer Customer { get; set; }
	}
}