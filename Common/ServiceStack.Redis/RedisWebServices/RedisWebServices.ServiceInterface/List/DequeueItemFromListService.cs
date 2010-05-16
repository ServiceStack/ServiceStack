using RedisWebServices.ServiceModel.Operations.List;

namespace RedisWebServices.ServiceInterface.List
{
	public class DequeueItemFromListService
		: RedisServiceBase<DequeueItemFromList>
	{
		protected override object Run(DequeueItemFromList request)
		{
			return new DequeueItemFromListResponse
			{
				Item = RedisExec(r => r.DequeueItemFromList(request.Id))
			};
		}
	}
}