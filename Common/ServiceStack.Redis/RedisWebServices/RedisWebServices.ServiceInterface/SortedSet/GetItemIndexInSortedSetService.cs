using RedisWebServices.ServiceModel.Operations.SortedSet;

namespace RedisWebServices.ServiceInterface.SortedSet
{
	public class GetItemIndexInSortedSetService
		: RedisServiceBase<GetItemIndexInSortedSet>
	{
		protected override object Run(GetItemIndexInSortedSet request)
		{
			return new GetItemIndexInSortedSetResponse
	       	{
				Index = RedisExec(r => r.GetItemIndexInSortedSet(request.Id, request.Item))
	       	};
		}
	}
}