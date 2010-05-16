using RedisWebServices.ServiceModel.Operations.SortedSet;

namespace RedisWebServices.ServiceInterface.SortedSet
{
	public class SortedSetContainsItemService
		: RedisServiceBase<SortedSetContainsItem>
	{
		protected override object Run(SortedSetContainsItem request)
		{
			return new SortedSetContainsItemResponse
	       	{
				Result = RedisExec(r => r.SortedSetContainsItem(request.Id, request.Item))
	       	};
		}
	}
}