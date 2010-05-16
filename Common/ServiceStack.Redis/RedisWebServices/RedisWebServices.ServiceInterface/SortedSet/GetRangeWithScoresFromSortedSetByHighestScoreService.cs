using RedisWebServices.ServiceModel.Operations.SortedSet;
using RedisWebServices.ServiceModel.Types;
using ServiceStack.Common.Extensions;

namespace RedisWebServices.ServiceInterface.SortedSet
{
	public class GetRangeWithScoresFromSortedSetByHighestScoreService
		: RedisServiceBase<GetRangeWithScoresFromSortedSetByHighestScore>
	{
		protected override object Run(GetRangeWithScoresFromSortedSetByHighestScore request)
		{
			var itemsScoreMap = request.FromStringScore.IsNullOrEmpty()
        		? RedisExec(r => r.GetRangeWithScoresFromSortedSetByHighestScore(
        				request.Id, request.FromScore, request.ToScore, request.Skip, request.Take))
        		: RedisExec(r => r.GetRangeWithScoresFromSortedSetByHighestScore(
						request.Id, request.FromStringScore, request.ToStringScore, request.Skip, request.Take));

			return new GetRangeWithScoresFromSortedSetByHighestScoreResponse
	       	{
				ItemsWithScores = new ArrayOfItemWithScore(itemsScoreMap.ToItemsWithScores())
	       	};
		}
	}
}