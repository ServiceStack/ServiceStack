using System.Collections.Generic;
using System.Runtime.Serialization;
using ServiceStack.ServiceInterface.Tests.Support.Version100.Types;

namespace ServiceStack.ServiceInterface.Tests.Support.Version100.Operations
{
	[DataContract]
	public class GetCustomersResponse
	{
		[DataMember]
		public List<Customer> Customers { get; set; }
	}
}