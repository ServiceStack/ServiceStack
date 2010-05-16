using RedisWebServices.ServiceModel.Operations.List;

namespace RedisWebServices.ServiceInterface.List
{
	public class EnqueueItemOnListService
		: RedisServiceBase<EnqueueItemOnList>
	{
		protected override object Run(EnqueueItemOnList request)
		{
			RedisExec(r => r.EnqueueItemOnList(request.Id, request.Item));

			return new EnqueueItemOnListResponse();
		}
	}
}