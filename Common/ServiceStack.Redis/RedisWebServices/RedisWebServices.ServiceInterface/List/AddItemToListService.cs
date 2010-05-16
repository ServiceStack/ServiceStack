using RedisWebServices.ServiceModel.Operations.List;

namespace RedisWebServices.ServiceInterface.List
{
	public class AddItemToListService
		: RedisServiceBase<AddItemToList>
	{
		protected override object Run(AddItemToList request)
		{
			RedisExec(r => r.AddItemToList(request.Id, request.Item));

			return new AddItemToListResponse();
		}
	}
}