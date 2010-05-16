using RedisWebServices.ServiceModel.Operations.SortedSet;

namespace RedisWebServices.ServiceInterface.SortedSet
{
	public class RemoveRangeFromSortedSetByScoreService
		: RedisServiceBase<RemoveRangeFromSortedSetByScore>
	{
		protected override object Run(RemoveRangeFromSortedSetByScore request)
		{
			return new RemoveRangeFromSortedSetByScoreResponse
	       	{
				ItemsRemovedCount = RedisExec(r => r.RemoveRangeFromSortedSetByScore(
					request.Id, request.FromScore, request.ToScore))
	       	};
		}
	}
}