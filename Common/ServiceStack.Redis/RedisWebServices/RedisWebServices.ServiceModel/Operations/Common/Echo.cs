using System.Runtime.Serialization;
using ServiceStack.ServiceInterface.ServiceModel;

namespace RedisWebServices.ServiceModel.Operations.Common
{
	[DataContract]
	public class Echo
	{
		[DataMember]
		public string Text { get; set; }
	}

	[DataContract]
	public class EchoResponse
	{
		public EchoResponse()
		{
			this.ResponseStatus = new ResponseStatus();
		}

		[DataMember]
		public string Text { get; set; }

		[DataMember]
		public ResponseStatus ResponseStatus { get; set; }
	}
}