using RedisWebServices.ServiceModel.Operations.List;

namespace RedisWebServices.ServiceInterface.List
{
	public class BlockingDequeueItemFromListService
		: RedisServiceBase<BlockingDequeueItemFromList>
	{
		protected override object Run(BlockingDequeueItemFromList request)
		{
			return new BlockingDequeueItemFromListResponse
	       	{
				Item = RedisExec(r => r.BlockingDequeueItemFromList(request.Id, request.TimeOut))
	       	};
		}
	}
}