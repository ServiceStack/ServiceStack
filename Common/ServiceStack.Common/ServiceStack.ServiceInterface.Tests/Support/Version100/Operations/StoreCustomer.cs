using System.Runtime.Serialization;
using ServiceStack.ServiceInterface.Tests.Support.Version100.Types;

namespace ServiceStack.ServiceInterface.Tests.Support.Version100.Operations
{
	[DataContract]
	public class StoreCustomer
	{
		[DataMember]
		public Customer Customer { get; set; }
	}
}