using System.Runtime.Serialization;

namespace ServiceStack.WebHost.IntegrationTests.Operations
{
	[DataContract]
	public class Reverse
	{
		[DataMember]
		public string Value { get; set; }
	}

	[DataContract]
	public class ReverseResponse
	{
		[DataMember]
		public string Result { get; set; }
	}
}