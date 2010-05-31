using RedisWebServices.ServiceModel.Operations.SortedSet;
using ServiceStack.ServiceInterface.ServiceModel;

namespace RedisWebServices.ServiceInterface.SortedSet
{
	public class GetAllItemsFromSortedSetService
		: RedisServiceBase<GetAllItemsFromSortedSet>
	{
		protected override object Run(GetAllItemsFromSortedSet request)
		{
			return new GetAllItemsFromSortedSetResponse
	       	{
	       		Items = new ArrayOfString(RedisExec(r => r.GetAllItemsFromSortedSet(request.Id)))
	       	};
		}
	}
}