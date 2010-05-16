using System.Runtime.Serialization;
using RedisWebServices.ServiceModel.Types;
using ServiceStack.DesignPatterns.Model;
using ServiceStack.ServiceInterface.ServiceModel;

namespace RedisWebServices.ServiceModel.Operations.SortedSet
{
	[DataContract]
	public class GetRangeWithScoresFromSortedSetByLowestScore
		: IHasStringId
	{
		[DataMember]
		public string Id { get; set; }

		[DataMember]
		public double FromScore { get; set; }

		[DataMember]
		public double ToScore { get; set; }

		[DataMember]
		public string FromStringScore { get; set; }

		[DataMember]
		public string ToStringScore { get; set; }

		[DataMember]
		public int? Skip { get; set; }

		[DataMember]
		public int? Take { get; set; }
	}

	[DataContract]
	public class GetRangeWithScoresFromSortedSetByLowestScoreResponse
	{
		public GetRangeWithScoresFromSortedSetByLowestScoreResponse()
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