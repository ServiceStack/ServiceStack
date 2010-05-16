using RedisWebServices.ServiceModel.Operations.SortedSet;
using ServiceStack.Common.Extensions;
using ServiceStack.ServiceInterface.ServiceModel;

namespace RedisWebServices.ServiceInterface.SortedSet
{
	public class GetRangeFromSortedSetByLowestScoreService
		: RedisServiceBase<GetRangeFromSortedSetByLowestScore>
	{
		protected override object Run(GetRangeFromSortedSetByLowestScore request)
		{
			var results = request.FromStringScore.IsNullOrEmpty()
          		? RedisExec(r => r.GetRangeFromSortedSetByLowestScore(request.Id, request.FromScore, request.ToScore, request.Skip, request.Take))
				: RedisExec(r => r.GetRangeFromSortedSetByLowestScore(request.Id, request.FromStringScore, request.ToStringScore, request.Skip, request.Take));

			return new GetRangeFromSortedSetByLowestScoreResponse
	       	{
				Items = new ArrayOfString(results)
	       	};
		}
	}
}