using RedisWebServices.ServiceModel.Operations.List;

namespace RedisWebServices.ServiceInterface.List
{
	public class PrependItemToListService
		: RedisServiceBase<PrependItemToList>
	{
		protected override object Run(PrependItemToList request)
		{
			RedisExec(r => r.PrependItemToList(request.Id, request.Item));

			return new PrependItemToListResponse();
		}
	}
}