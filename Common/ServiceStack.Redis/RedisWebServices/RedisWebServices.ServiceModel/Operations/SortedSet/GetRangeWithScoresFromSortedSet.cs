using System.Runtime.Serialization;
using RedisWebServices.ServiceModel.Types;
using ServiceStack.DesignPatterns.Model;
using ServiceStack.ServiceInterface.ServiceModel;

namespace RedisWebServices.ServiceModel.Operations.SortedSet
{
	[DataContract]
	public class GetRangeWithScoresFromSortedSet
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
	public class GetRangeWithScoresFromSortedSetResponse
	{
		public GetRangeWithScoresFromSortedSetResponse()
		{
			this.ResponseStatus = new ResponseStatus();

			this.ItemsWithScores = new ArrayOfItemWithScore();
		}

		[DataMember]
		public ArrayOfItemWithScore ItemsWithScores { get; set; }

		[DataMember]
		public ResponseStatus ResponseStatus { get; set; }
	}
}