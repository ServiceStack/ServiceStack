using RedisWebServices.ServiceModel.Operations.SortedSet;
using ServiceStack.ServiceInterface.ServiceModel;

namespace RedisWebServices.ServiceInterface.SortedSet
{
	public class GetRangeFromSortedSetService
		: RedisServiceBase<GetRangeFromSortedSet>
	{
		protected override object Run(GetRangeFromSortedSet request)
		{
			var results = !request.SortDescending
				? RedisExec(r => r.GetRangeFromSortedSet(request.Id, request.FromRank, request.ToRank))
				: RedisExec(r => r.GetRangeFromSortedSetDesc(request.Id, request.FromRank, request.ToRank));

			return new GetRangeFromSortedSetResponse
	       	{
				Items = new ArrayOfString(results)
	       	};
		}
	}
}