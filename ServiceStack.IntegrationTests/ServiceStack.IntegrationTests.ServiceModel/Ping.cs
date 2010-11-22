using System.Runtime.Serialization;

namespace ServiceStack.IntegrationTests.ServiceModel
{
	[DataContract]
	public class Ping
	{
		[DataMember]
		public string Text { get; set; }
	}

	[DataContract]
	public class PingResponse
	{
		[DataMember]
		public string Text { get; set; }
	}
}