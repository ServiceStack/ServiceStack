using RedisWebServices.ServiceModel.Operations.List;
using ServiceStack.ServiceInterface.ServiceModel;

namespace RedisWebServices.ServiceInterface.List
{
	public class GetAllItemsFromListService
		: RedisServiceBase<GetAllItemsFromList>
	{
		protected override object Run(GetAllItemsFromList request)
		{
			return new GetAllItemsFromListResponse
			{
				Items = new ArrayOfString(
					RedisExec(r => r.GetAllItemsFromList(request.Id))
				)
			};
		}
	}
}