using RedisWebServices.ServiceModel.Operations.SortedSet;

namespace RedisWebServices.ServiceInterface.SortedSet
{
	public class StoreIntersectFromSortedSetsService
		: RedisServiceBase<StoreIntersectFromSortedSets>
	{
		protected override object Run(StoreIntersectFromSortedSets request)
		{
			return new StoreIntersectFromSortedSetsResponse
	       	{
				Count = RedisExec(r => r.StoreIntersectFromSortedSets(request.Id, request.FromSetIds.ToArray()))
	       	};
		}
	}
}