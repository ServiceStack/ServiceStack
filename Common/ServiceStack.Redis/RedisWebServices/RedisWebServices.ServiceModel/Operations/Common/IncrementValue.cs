using System.Runtime.Serialization;
using ServiceStack.ServiceInterface.ServiceModel;

namespace RedisWebServices.ServiceModel.Operations.Common
{
	[DataContract]
	public class IncrementValue
	{
		[DataMember]
		public string Key { get; set; }

		[DataMember]
		public int? IncrementBy { get; set; }

	}

	[DataContract]
	public class IncrementValueResponse
	{
		public IncrementValueResponse()
		{
			this.ResponseStatus = new ResponseStatus();
		}

		[DataMember] 
		public int Value { get; set; }

		[DataMember]
		public ResponseStatus ResponseStatus { get; set; }
	}
}