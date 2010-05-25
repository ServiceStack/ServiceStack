using RedisWebServices.ServiceModel.Operations.SortedSet;
using RedisWebServices.ServiceModel.Types;

namespace RedisWebServices.ServiceInterface.SortedSet
{
	public class GetRangeWithScoresFromSortedSetByLowestScoreService
		: RedisServiceBase<GetRangeWithScoresFromSortedSetByLowestScore>
	{
		protected override object Run(GetRangeWithScoresFromSortedSetByLowestScore request)
		{
			var itemsScoreMap = RedisExec(r => r.GetRangeWithScoresFromSortedSetByLowestScore(
				request.Id, request.FromScore, request.ToScore, request.Skip, request.Take));

			return new GetRangeWithScoresFromSortedSetByLowestScoreResponse
	       	{
				ItemsWithScores = new ArrayOfItemWithScore(itemsScoreMap.ToItemsWithScores())
	       	};
		}
	}
}