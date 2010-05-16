using System.Runtime.Serialization;
using ServiceStack.DesignPatterns.Model;
using ServiceStack.ServiceInterface.ServiceModel;

namespace RedisWebServices.ServiceModel.Operations.SortedSet
{
	[DataContract]
	public class GetRangeFromSortedSetByHighestScore
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
	public class GetRangeFromSortedSetByHighestScoreResponse
	{
		public GetRangeFromSortedSetByHighestScoreResponse()
		{
			this.ResponseStatus = new ResponseStatus();

			this.Items = new ArrayOfString();
		}

		[DataMember]
		public ArrayOfString Items { get; set; }

		[DataMember]
		public ResponseStatus ResponseStatus { get; set; }
	}
}