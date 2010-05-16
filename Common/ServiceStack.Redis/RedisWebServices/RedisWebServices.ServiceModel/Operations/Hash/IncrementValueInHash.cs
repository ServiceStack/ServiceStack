using System.Runtime.Serialization;
using ServiceStack.DesignPatterns.Model;
using ServiceStack.ServiceInterface.ServiceModel;

namespace RedisWebServices.ServiceModel.Operations.Hash
{
	[DataContract]
	public class IncrementValueInHash
		: IHasStringId
	{
		[DataMember]
		public string Id { get; set; }

		[DataMember]
		public string Key { get; set; }

		[DataMember]
		public int? IncrementBy { get; set; }
	}

	[DataContract]
	public class IncrementValueInHashResponse
	{
		public IncrementValueInHashResponse()
		{
			this.ResponseStatus = new ResponseStatus();
		}

		[DataMember]
		public int Value { get; set; }

		[DataMember]
		public ResponseStatus ResponseStatus { get; set; }
	}
}