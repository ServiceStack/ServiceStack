using RedisWebServices.ServiceModel.Operations.SortedSet;
using ServiceStack.Common.Extensions;
using ServiceStack.ServiceInterface.ServiceModel;

namespace RedisWebServices.ServiceInterface.SortedSet
{
	public class GetRangeFromSortedSetByHighestScoreService
		: RedisServiceBase<GetRangeFromSortedSetByHighestScore>
	{
		protected override object Run(GetRangeFromSortedSetByHighestScore request)
		{
			var results = request.FromStringScore.IsNullOrEmpty()
          		? RedisExec(r => r.GetRangeFromSortedSetByHighestScore(request.Id, request.FromScore, request.ToScore, request.Skip, request.Take))
				: RedisExec(r => r.GetRangeFromSortedSetByHighestScore(request.Id, request.FromStringScore, request.ToStringScore, request.Skip, request.Take));

			return new GetRangeFromSortedSetByHighestScoreResponse
	       	{
				Items = new ArrayOfString(results)
	       	};
		}
	}
}