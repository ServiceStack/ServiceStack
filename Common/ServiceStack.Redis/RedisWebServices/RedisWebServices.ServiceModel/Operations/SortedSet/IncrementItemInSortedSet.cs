using System.Runtime.Serialization;
using ServiceStack.DesignPatterns.Model;
using ServiceStack.ServiceInterface.ServiceModel;

namespace RedisWebServices.ServiceModel.Operations.SortedSet
{
	[DataContract]
	public class IncrementItemInSortedSet
		: IHasStringId
	{
		[DataMember]
		public string Id { get; set; }

		[DataMember]
		public string Item { get; set; }

		[DataMember]
		public double? IncrementBy { get; set; }
	}

	[DataContract]
	public class IncrementItemInSortedSetResponse
	{
		public IncrementItemInSortedSetResponse()
		{
			this.ResponseStatus = new ResponseStatus();
		}

		[DataMember]
		public double Score { get; set; }

		[DataMember]
		public ResponseStatus ResponseStatus { get; set; }
	}
}