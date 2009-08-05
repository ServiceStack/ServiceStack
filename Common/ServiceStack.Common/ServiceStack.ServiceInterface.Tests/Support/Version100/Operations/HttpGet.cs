using System.Runtime.Serialization;

namespace ServiceStack.ServiceInterface.Tests.Support.Version100.Operations
{
	[DataContract]
	public class HttpGet
	{
		[DataMember]
		public bool Value { get; set; }
	}
}