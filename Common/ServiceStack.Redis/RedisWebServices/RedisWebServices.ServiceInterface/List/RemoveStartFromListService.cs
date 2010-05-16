using RedisWebServices.ServiceModel.Operations.List;

namespace RedisWebServices.ServiceInterface.List
{
	public class RemoveStartFromListService
		: RedisServiceBase<RemoveStartFromList>
	{
		protected override object Run(RemoveStartFromList request)
		{
			return new RemoveStartFromListResponse
			{
				Item = RedisExec(r => r.RemoveStartFromList(request.Id))
			};
		}
	}
}