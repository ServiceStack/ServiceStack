using System.Runtime.Serialization;
using ServiceStack.DesignPatterns.Model;
using ServiceStack.ServiceInterface.ServiceModel;

namespace RedisWebServices.ServiceModel.Operations.SortedSet
{
	[DataContract]
	public class RemoveRangeFromSortedSetByScore
		: IHasStringId
	{
		[DataMember]
		public string Id { get; set; }

		[DataMember]
		public double FromScore { get; set; }

		[DataMember]
		public double ToScore { get; set; }
	}

	[DataContract]
	public class RemoveRangeFromSortedSetByScoreResponse
	{
		public RemoveRangeFromSortedSetByScoreResponse()
		{
			this.ResponseStatus = new ResponseStatus();
		}

		[DataMember]
		public int ItemsRemovedCount { get; set; }

		[DataMember]
		public ResponseStatus ResponseStatus { get; set; }
	}
}