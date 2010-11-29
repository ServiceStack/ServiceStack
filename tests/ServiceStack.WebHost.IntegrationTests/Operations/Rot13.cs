using System.Runtime.Serialization;

namespace ServiceStack.WebHost.IntegrationTests.Operations
{
	[DataContract]
	public class Rot13
	{
		[DataMember]
		public string Value { get; set; }
	}

	[DataContract]
	public class Rot13Response
	{
		[DataMember]
		public string Result { get; set; }
	}
}