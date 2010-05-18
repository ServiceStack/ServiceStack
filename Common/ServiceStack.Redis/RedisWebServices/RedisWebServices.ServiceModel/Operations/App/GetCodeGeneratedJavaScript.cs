using System.Runtime.Serialization;
using ServiceStack.ServiceInterface.ServiceModel;

namespace RedisWebServices.ServiceModel.Operations.App
{
	[DataContract]
	public class GetCodeGeneratedJavaScript
	{
		[DataMember]
		public string Text { get; set; }
	}

	[DataContract]
	public class GetCodeGeneratedJavaScriptResponse
	{
		public GetCodeGeneratedJavaScriptResponse()
		{
			this.ResponseStatus = new ResponseStatus();
		}

		[DataMember]
		public string JavaScript { get; set; }

		[DataMember]
		public ResponseStatus ResponseStatus { get; set; }
	}
}