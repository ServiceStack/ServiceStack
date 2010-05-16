using RedisWebServices.ServiceModel.Operations.SortedSet;

namespace RedisWebServices.ServiceInterface.SortedSet
{
	public class RemoveItemFromSortedSetService
		: RedisServiceBase<RemoveItemFromSortedSet>
	{
		protected override object Run(RemoveItemFromSortedSet request)
		{
			return new RemoveItemFromSortedSetResponse
	       	{
				Result = RedisExec(r => r.RemoveItemFromSortedSet(request.Id, request.Item))
	       	};
		}
	}
}