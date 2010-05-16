using System.Runtime.Serialization;
using ServiceStack.ServiceInterface.ServiceModel;

namespace RedisWebServices.ServiceModel.Operations.Common
{
	[DataContract]
	public class GetSubstring
	{
		[DataMember]
		public string Key { get; set; }

		[DataMember]
		public int FromIndex { get; set; }

		[DataMember]
		public int ToIndex { get; set; }
	}

	[DataContract]
	public class GetSubstringResponse
	{
		public GetSubstringResponse()
		{
			this.ResponseStatus = new ResponseStatus();
		}

		[DataMember] 
		public string Value { get; set; }

		[DataMember]
		public ResponseStatus ResponseStatus { get; set; }
	}
}