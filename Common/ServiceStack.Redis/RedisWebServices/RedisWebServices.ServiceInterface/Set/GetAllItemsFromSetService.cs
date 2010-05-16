using RedisWebServices.ServiceModel.Operations.Set;
using ServiceStack.ServiceInterface.ServiceModel;

namespace RedisWebServices.ServiceInterface.Set
{
	public class GetAllItemsFromSetService
		: RedisServiceBase<GetAllItemsFromSet>
	{
		protected override object Run(GetAllItemsFromSet request)
		{
			return new GetAllItemsFromSetResponse
	       	{
	       		Items = new ArrayOfString(RedisExec(r => r.GetAllItemsFromSet(request.Id)))
	       	};
		}
	}
}