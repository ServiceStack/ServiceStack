using System.Collections.Generic;
using System.Runtime.Serialization;

namespace ServiceStack.WebHost.Endpoints.Tests.Support.Operations
{
	[DataContract]
	public class GetCustomers
	{
		[DataMember]
		public List<long> CustomerIds { get; set; }
	}
}