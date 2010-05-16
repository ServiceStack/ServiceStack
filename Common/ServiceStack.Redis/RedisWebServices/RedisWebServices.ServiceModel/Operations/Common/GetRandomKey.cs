using System.Runtime.Serialization;
using ServiceStack.ServiceInterface.ServiceModel;

namespace RedisWebServices.ServiceModel.Operations.Common
{
	[DataContract]
	public class GetRandomKey
	{
	}

	[DataContract]
	public class GetRandomKeyResponse
	{
		public GetRandomKeyResponse()
		{
			this.ResponseStatus = new ResponseStatus();
		}

		[DataMember] 
		public string Key { get; set; }

		[DataMember]
		public ResponseStatus ResponseStatus { get; set; }
	}
}