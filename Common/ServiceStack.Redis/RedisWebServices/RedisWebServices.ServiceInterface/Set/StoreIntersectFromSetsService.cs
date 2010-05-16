using RedisWebServices.ServiceModel.Operations.Set;

namespace RedisWebServices.ServiceInterface.Set
{
	public class StoreIntersectFromSetsService
		: RedisServiceBase<StoreIntersectFromSets>
	{
		protected override object Run(StoreIntersectFromSets request)
		{
			RedisExec(r => r.StoreIntersectFromSets(request.Id, request.SetIds.ToArray()));

			return new StoreIntersectFromSetsResponse();
		}
	}
}