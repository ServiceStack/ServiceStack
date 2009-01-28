using System.Runtime.Serialization;
using ServiceStack.ServiceInterface.Tests.Support.Version100.Types;

namespace ServiceStack.ServiceInterface.Tests.Support.Version100.Operations
{
	[DataContract]
	public class GetCustomerResponse
	{
		[DataMember]
		public Customer Customer { get; set; }
	}
}