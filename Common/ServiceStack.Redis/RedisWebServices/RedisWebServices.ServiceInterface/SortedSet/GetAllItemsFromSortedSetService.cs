using RedisWebServices.ServiceModel.Operations.SortedSet;
using ServiceStack.ServiceInterface.ServiceModel;

namespace RedisWebServices.ServiceInterface.SortedSet
{
	public class GetAllItemsFromSortedSetService
		: RedisServiceBase<GetAllItemsFromSortedSet>
	{
		protected override object Run(GetAllItemsFromSortedSet request)
		{
			var results = !request.SortDescending
				? RedisExec(r => r.GetAllItemsFromSortedSet(request.Id))
				: RedisExec(r => r.GetAllItemsFromSortedSetDesc(request.Id));

			return new GetAllItemsFromSortedSetResponse
	       	{
	       		Items = new ArrayOfString(results)
	       	};
		}
	}
}