using RedisWebServices.ServiceModel.Operations.SortedSet;
using RedisWebServices.ServiceModel.Types;

namespace RedisWebServices.ServiceInterface.SortedSet
{
	public class GetRangeWithScoresFromSortedSetService
		: RedisServiceBase<GetRangeWithScoresFromSortedSet>
	{
		protected override object Run(GetRangeWithScoresFromSortedSet request)
		{
			var itemsScoreMap = RedisExec(r => r.GetRangeWithScoresFromSortedSet(request.Id, request.FromRank, request.ToRank));

			return new GetRangeWithScoresFromSortedSetResponse
	       	{
				ItemsWithScores = new ArrayOfItemWithScore(itemsScoreMap.ToItemsWithScores())
	       	};
		}
	}
}