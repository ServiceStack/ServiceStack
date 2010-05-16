using RedisWebServices.ServiceModel.Operations.SortedSet;

namespace RedisWebServices.ServiceInterface.SortedSet
{
	public class IncrementItemInSortedSetService
		: RedisServiceBase<IncrementItemInSortedSet>
	{
		protected override object Run(IncrementItemInSortedSet request)
		{
			return new IncrementItemInSortedSetResponse
	       	{
				Score = RedisExec(r => r.IncrementItemInSortedSet(request.Id, request.Item, request.IncrementBy.GetValueOrDefault(1)))
	       	};
		}
	}
}