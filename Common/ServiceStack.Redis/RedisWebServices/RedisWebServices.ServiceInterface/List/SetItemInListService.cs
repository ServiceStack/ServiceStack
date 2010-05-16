using RedisWebServices.ServiceModel.Operations.List;

namespace RedisWebServices.ServiceInterface.List
{
	public class SetItemInListService
		: RedisServiceBase<SetItemInList>
	{
		protected override object Run(SetItemInList request)
		{
			RedisExec(r => r.SetItemInList(request.Id, request.Index, request.Item));

			return new SetItemInListResponse();
		}
	}
}