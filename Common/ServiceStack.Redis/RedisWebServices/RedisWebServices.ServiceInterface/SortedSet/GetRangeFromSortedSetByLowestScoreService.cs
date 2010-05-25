using RedisWebServices.ServiceModel.Operations.SortedSet;
using ServiceStack.ServiceInterface.ServiceModel;

namespace RedisWebServices.ServiceInterface.SortedSet
{
	public class GetRangeFromSortedSetByLowestScoreService
		: RedisServiceBase<GetRangeFromSortedSetByLowestScore>
	{
		protected override object Run(GetRangeFromSortedSetByLowestScore request)
		{
			return new GetRangeFromSortedSetByLowestScoreResponse
	       	{
				Items = new ArrayOfString
				(
					RedisExec(r => r.GetRangeFromSortedSetByLowestScore(
						request.Id, request.FromScore, request.ToScore, request.Skip, request.Take))
				)
	       	};
		}
	}
}