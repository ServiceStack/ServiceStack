using RedisWebServices.ServiceModel.Operations.SortedSet;

namespace RedisWebServices.ServiceInterface.SortedSet
{
	public class AddItemToSortedSetService
		: RedisServiceBase<AddItemToSortedSet>
	{
		protected override object Run(AddItemToSortedSet request)
		{
			RedisExec(r => r.AddItemToSortedSet(request.Id, request.Item));

			return new AddItemToSortedSetResponse();
		}
	}
}