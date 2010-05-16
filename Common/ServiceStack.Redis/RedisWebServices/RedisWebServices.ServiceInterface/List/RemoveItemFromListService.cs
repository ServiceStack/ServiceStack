using RedisWebServices.ServiceModel.Operations.List;

namespace RedisWebServices.ServiceInterface.List
{
	public class RemoveItemFromListService
		: RedisServiceBase<RemoveItemFromList>
	{
		protected override object Run(RemoveItemFromList request)
		{
			return new RemoveItemFromListResponse
			{
				ItemsRemovedCount = RedisExec(r => r.RemoveItemFromList(request.Id, 
					request.Item, request.NoOfMatches.GetValueOrDefault(0)))
			};
		}
	}
}