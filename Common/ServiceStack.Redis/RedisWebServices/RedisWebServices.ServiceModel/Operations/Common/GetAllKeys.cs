using System.Runtime.Serialization;
using ServiceStack.ServiceInterface.ServiceModel;

namespace RedisWebServices.ServiceModel.Operations.Common
{
	[DataContract]
	public class GetAllKeys
	{
	}

	[DataContract]
	public class GetAllKeysResponse
	{
		public GetAllKeysResponse()
		{
			this.ResponseStatus = new ResponseStatus();
			this.Keys = new ArrayOfString();
		}

		[DataMember] 
		public ArrayOfString Keys { get; set; }

		[DataMember]
		public ResponseStatus ResponseStatus { get; set; }
	}
}