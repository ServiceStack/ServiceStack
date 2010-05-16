using RedisWebServices.ServiceModel.Operations.List;

namespace RedisWebServices.ServiceInterface.List
{
	public class PopAndPushItemBetweenListsService
		: RedisServiceBase<PopAndPushItemBetweenLists>
	{
		protected override object Run(PopAndPushItemBetweenLists request)
		{
			return new PopAndPushItemBetweenListsResponse
			{
				Item = RedisExec(r => r.PopAndPushItemBetweenLists(request.FromListId, request.ToListId))
			};
		}
	}
}