using RedisWebServices.ServiceModel.Operations.SortedSet;

namespace RedisWebServices.ServiceInterface.SortedSet
{
	public class StoreUnionFromSortedSetsService
		: RedisServiceBase<StoreUnionFromSortedSets>
	{
		protected override object Run(StoreUnionFromSortedSets request)
		{
			return new StoreUnionFromSortedSetsResponse
	       	{
				Count = RedisExec(r => r.StoreUnionFromSortedSets(request.Id, request.FromSetIds.ToArray()))
	       	};
		}
	}
}