using RedisWebServices.ServiceModel.Operations.Set;

namespace RedisWebServices.ServiceInterface.Set
{
	public class GetRandomItemFromSetService
		: RedisServiceBase<GetRandomItemFromSet>
	{
		protected override object Run(GetRandomItemFromSet request)
		{
			return new GetRandomItemFromSetResponse
	       	{
				Item = RedisExec(r => r.GetRandomItemFromSet(request.Id))
	       	};
		}
	}
}