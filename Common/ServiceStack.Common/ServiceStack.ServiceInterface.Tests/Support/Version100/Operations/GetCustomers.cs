using System.Collections.Generic;
using System.Runtime.Serialization;

namespace ServiceStack.ServiceInterface.Tests.Support.Version100.Operations
{
	[DataContract]
	public class GetCustomers 
	{
		[DataMember]
		public List<int> CustomerIds { get; set; }
	}
}