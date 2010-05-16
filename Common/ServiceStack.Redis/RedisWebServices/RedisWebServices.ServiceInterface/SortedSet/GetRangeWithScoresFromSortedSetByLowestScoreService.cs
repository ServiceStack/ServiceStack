using RedisWebServices.ServiceModel.Operations.SortedSet;
using RedisWebServices.ServiceModel.Types;
using ServiceStack.Common.Extensions;

namespace RedisWebServices.ServiceInterface.SortedSet
{
	public class GetRangeWithScoresFromSortedSetByLowestScoreService
		: RedisServiceBase<GetRangeWithScoresFromSortedSetByLowestScore>
	{
		protected override object Run(GetRangeWithScoresFromSortedSetByLowestScore request)
		{
			var itemsScoreMap = request.FromStringScore.IsNullOrEmpty()
        		? RedisExec(r => r.GetRangeWithScoresFromSortedSetByLowestScore(
        				request.Id, request.FromScore, request.ToScore, request.Skip, request.Take))
        		: RedisExec(r => r.GetRangeWithScoresFromSortedSetByLowestScore(
						request.Id, request.FromStringScore, request.ToStringScore, request.Skip, request.Take));

			return new GetRangeWithScoresFromSortedSetByLowestScoreResponse
	       	{
				ItemsWithScores = new ArrayOfItemWithScore(itemsScoreMap.ToItemsWithScores())
	       	};
		}
	}
}