using RedisWebServices.ServiceModel.Operations.List;

namespace RedisWebServices.ServiceInterface.List
{
	public class BlockingRemoveStartFromListService
		: RedisServiceBase<BlockingRemoveStartFromList>
	{
		protected override object Run(BlockingRemoveStartFromList request)
		{
			return new BlockingRemoveStartFromListResponse
			{
				Item = RedisExec(r => r.BlockingRemoveStartFromList(request.Id, request.TimeOut))
			};
		}
	}
}