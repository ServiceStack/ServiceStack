using RedisWebServices.ServiceModel.Operations.List;
using ServiceStack.Text;

namespace RedisWebServices.ServiceInterface.List
{
	public class AddRangeToListService
		: RedisServiceBase<AddRangeToList>
	{
		protected override object Run(AddRangeToList request)
		{
			if (!request.Items.IsNullOrEmpty())
			{
				RedisExec(r => request.Items.ForEach(x => r.AddItemToList(request.Id, x)));
			}

			return new AddRangeToListResponse();
		}
	}
}