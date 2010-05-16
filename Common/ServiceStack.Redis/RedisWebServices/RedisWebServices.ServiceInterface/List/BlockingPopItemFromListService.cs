using RedisWebServices.ServiceModel.Operations.List;

namespace RedisWebServices.ServiceInterface.List
{
	public class BlockingPopItemFromListService
		: RedisServiceBase<BlockingPopItemFromList>
	{
		protected override object Run(BlockingPopItemFromList request)
		{
			return new BlockingPopItemFromListResponse
			{
				Item = RedisExec(r => r.BlockingPopItemFromList(request.Id, request.TimeOut))
			};
		}
	}
}