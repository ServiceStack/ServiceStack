using RedisWebServices.ServiceModel.Operations.List;

namespace RedisWebServices.ServiceInterface.List
{
	public class PopItemFromListService
		: RedisServiceBase<PopItemFromList>
	{
		protected override object Run(PopItemFromList request)
		{
			return new PopItemFromListResponse
			{
				Item = RedisExec(r => r.PopItemFromList(request.Id))
			};
		}
	}
}