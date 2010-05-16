using System.Runtime.Serialization;
using ServiceStack.DesignPatterns.Model;
using ServiceStack.ServiceInterface.ServiceModel;

namespace RedisWebServices.ServiceModel.Operations.SortedSet
{
	[DataContract]
	public class RemoveRangeFromSortedSet
		: IHasStringId
	{
		[DataMember]
		public string Id { get; set; }

		[DataMember]
		public int FromRank { get; set; }

		[DataMember]
		public int ToRank { get; set; }
	}

	[DataContract]
	public class RemoveRangeFromSortedSetResponse
	{
		public RemoveRangeFromSortedSetResponse()
		{
			this.ResponseStatus = new ResponseStatus();
		}

		[DataMember]
		public int ItemsRemovedCount { get; set; }

		[DataMember]
		public ResponseStatus ResponseStatus { get; set; }
	}
}