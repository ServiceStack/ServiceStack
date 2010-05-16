using RedisWebServices.ServiceModel.Operations.List;

namespace RedisWebServices.ServiceInterface.List
{
	public class GetItemFromListService
		: RedisServiceBase<GetItemFromList>
	{
		protected override object Run(GetItemFromList request)
		{
			return new GetItemFromListResponse
			{
				Item = RedisExec(r => r.GetItemFromList(request.Id, request.Index))
			};
		}
	}
}