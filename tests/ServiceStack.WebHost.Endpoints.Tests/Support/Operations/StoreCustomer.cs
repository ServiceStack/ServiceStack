using System.Runtime.Serialization;
using ServiceStack.WebHost.Endpoints.Tests.Support.Types;

namespace ServiceStack.WebHost.Endpoints.Tests.Support.Operations
{
	[DataContract]
	public class StoreCustomer
	{
		[DataMember]
		public Customer Customer { get; set; }
	}
}