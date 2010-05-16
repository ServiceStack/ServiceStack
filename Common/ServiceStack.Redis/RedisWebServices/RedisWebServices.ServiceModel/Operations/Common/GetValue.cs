using System.Runtime.Serialization;
using ServiceStack.ServiceInterface.ServiceModel;

namespace RedisWebServices.ServiceModel.Operations.Common
{
	[DataContract]
	public class GetValue
	{
		[DataMember] 
		public string Key { get; set; }
	}

	[DataContract]
	public class GetValueResponse
	{
		public GetValueResponse()
		{
			this.ResponseStatus = new ResponseStatus();
		}

		[DataMember] 
		public string Value { get; set; }

		[DataMember]
		public ResponseStatus ResponseStatus { get; set; }
	}
}