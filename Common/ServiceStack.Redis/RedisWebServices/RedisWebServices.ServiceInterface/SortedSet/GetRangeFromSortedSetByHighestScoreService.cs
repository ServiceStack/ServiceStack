using RedisWebServices.ServiceModel.Operations.SortedSet;
using ServiceStack.ServiceInterface.ServiceModel;

namespace RedisWebServices.ServiceInterface.SortedSet
{
	public class GetRangeFromSortedSetByHighestScoreService
		: RedisServiceBase<GetRangeFromSortedSetByHighestScore>
	{
		protected override object Run(GetRangeFromSortedSetByHighestScore request)
		{
			return new GetRangeFromSortedSetByHighestScoreResponse
	       	{
				Items = new ArrayOfString
				(
					RedisExec(r => r.GetRangeFromSortedSetByHighestScore(
						request.Id, request.FromScore, request.ToScore, request.Skip, request.Take))	
				)
	       	};
		}
	}
}