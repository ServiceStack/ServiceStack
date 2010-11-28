using System.Runtime.Serialization;

namespace ServiceStack.WebHost.Endpoints.Tests.Support.Types
{
	[DataContract]
	public class Customer
	{
		[DataMember]
		public long Id { get; set; }
	}
}