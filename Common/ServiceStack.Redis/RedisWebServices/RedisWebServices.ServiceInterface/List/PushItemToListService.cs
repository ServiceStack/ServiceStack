using RedisWebServices.ServiceModel.Operations.List;

namespace RedisWebServices.ServiceInterface.List
{
	public class PushItemToListService
		: RedisServiceBase<PushItemToList>
	{
		protected override object Run(PushItemToList request)
		{
			RedisExec(r => r.PushItemToList(request.Id, request.Item));

			return new PushItemToListResponse();
		}
	}
}