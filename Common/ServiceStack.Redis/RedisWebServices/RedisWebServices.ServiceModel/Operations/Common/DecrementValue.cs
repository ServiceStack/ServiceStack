using System.Runtime.Serialization;
using ServiceStack.ServiceInterface.ServiceModel;

namespace RedisWebServices.ServiceModel.Operations.Common
{
	[DataContract]
	public class DecrementValue
	{
		[DataMember]
		public string Key { get; set; }

		[DataMember]
		public int? DecrementBy { get; set; }

	}

	[DataContract]
	public class DecrementValueResponse
	{
		public DecrementValueResponse()
		{
			this.ResponseStatus = new ResponseStatus();
		}

		[DataMember]
		public int Value { get; set; }

		[DataMember]
		public ResponseStatus ResponseStatus { get; set; }
	}
}