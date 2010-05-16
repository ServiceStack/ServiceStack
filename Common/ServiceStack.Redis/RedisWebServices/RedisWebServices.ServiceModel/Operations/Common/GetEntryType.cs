using System.Runtime.Serialization;
using RedisWebServices.ServiceModel.Types;
using ServiceStack.DataAnnotations;
using ServiceStack.ServiceInterface.ServiceModel;

namespace RedisWebServices.ServiceModel.Operations.Common
{
	[DataContract]
	public class GetEntryType
	{
		[DataMember] 
		public string Key { get; set; }
	}

	[DataContract]
	public class GetEntryTypeResponse
	{
		public GetEntryTypeResponse()
		{
			this.ResponseStatus = new ResponseStatus();
		}

		[References(typeof(KeyType))]
		[DataMember] 
		public string KeyType { get; set; }

		[DataMember]
		public ResponseStatus ResponseStatus { get; set; }
	}
}