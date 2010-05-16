using RedisWebServices.ServiceModel.Operations.List;

namespace RedisWebServices.ServiceInterface.List
{
	public class RemoveAllFromListService
		: RedisServiceBase<RemoveAllFromList>
	{
		protected override object Run(RemoveAllFromList request)
		{
			RedisExec(r => r.RemoveAllFromList(request.Id));

			return new RemoveAllFromListResponse();
		}
	}
}