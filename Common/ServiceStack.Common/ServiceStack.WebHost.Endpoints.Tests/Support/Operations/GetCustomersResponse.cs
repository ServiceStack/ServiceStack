using System.Collections.Generic;
using System.Runtime.Serialization;
using ServiceStack.WebHost.Endpoints.Tests.Support.Types;

namespace ServiceStack.WebHost.Endpoints.Tests.Support.Operations
{
	[DataContract]
	public class GetCustomersResponse
	{
		[DataMember]
		public List<Customer> Customers { get; set; }
	}
}