using System.Runtime.Serialization;
using ServiceStack.ServiceInterface.ServiceModel;

namespace RedisWebServices.ServiceModel.Operations.Common
{
	[DataContract]
	public class SearchKeys
	{
		[DataMember] 
		public string Pattern { get; set; }
	}

	[DataContract]
	public class SearchKeysResponse
	{
		public SearchKeysResponse()
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