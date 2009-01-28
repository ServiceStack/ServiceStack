using System.Runtime.Serialization;

namespace ServiceStack.ServiceInterface.Tests.Support.Version100.Types
{
	[DataContract]
	public class Customer
	{
		[DataMember]
		public long Id { get; set; }
	}
}