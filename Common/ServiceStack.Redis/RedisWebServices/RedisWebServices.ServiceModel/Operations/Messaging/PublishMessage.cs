using System.Runtime.Serialization;
using ServiceStack.ServiceInterface.ServiceModel;

namespace RedisWebServices.ServiceModel.Operations.Messaging
{
	[DataContract]
	public class PublishMessage
	{
		[DataMember]
		public string ToChannel { get; set; }

		[DataMember]
		public string Message { get; set; }
	}

	[DataContract]
	public class PublishMessageResponse
	{
		public PublishMessageResponse()
		{
			this.ResponseStatus = new ResponseStatus();
		}

		[DataMember]
		public ResponseStatus ResponseStatus { get; set; }
	}
}