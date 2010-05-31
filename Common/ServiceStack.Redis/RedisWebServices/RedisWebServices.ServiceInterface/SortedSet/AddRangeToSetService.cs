using RedisWebServices.ServiceModel.Operations.SortedSet;
using ServiceStack.Text;

namespace RedisWebServices.ServiceInterface.SortedSet
{
	public class AddRangeToSortedSetService
		: RedisServiceBase<AddRangeToSortedSet>
	{
		protected override object Run(AddRangeToSortedSet request)
		{
			if (!request.Items.IsNullOrEmpty())
			{
				RedisExec(r => request.Items.ForEach(x => r.AddItemToSortedSet(request.Id, x.Item, x.Score)));
			}

			return new AddRangeToSortedSetResponse();
		}
	}
}