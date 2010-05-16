using RedisWebServices.ServiceModel.Operations.Set;

namespace RedisWebServices.ServiceInterface.Set
{
	public class StoreUnionFromSetsService
		: RedisServiceBase<StoreUnionFromSets>
	{
		protected override object Run(StoreUnionFromSets request)
		{
			RedisExec(r => r.StoreUnionFromSets(request.Id, request.SetIds.ToArray()));

			return new StoreUnionFromSetsResponse();
		}
	}
}