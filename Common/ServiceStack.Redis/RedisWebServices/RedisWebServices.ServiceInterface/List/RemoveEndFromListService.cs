using RedisWebServices.ServiceModel.Operations.List;

namespace RedisWebServices.ServiceInterface.List
{
	public class RemoveEndFromListService
		: RedisServiceBase<RemoveEndFromList>
	{
		protected override object Run(RemoveEndFromList request)
		{
			return new RemoveEndFromListResponse
			{
				Item = RedisExec(r => r.RemoveEndFromList(request.Id))
			};
		}
	}
}