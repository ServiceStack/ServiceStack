using RedisWebServices.ServiceModel.Operations.Set;

namespace RedisWebServices.ServiceInterface.Set
{
	public class RemoveItemFromSetService
		: RedisServiceBase<RemoveItemFromSet>
	{
		protected override object Run(RemoveItemFromSet request)
		{
			RedisExec(r => r.RemoveItemFromSet(request.Id, request.Item));

			return new RemoveItemFromSetResponse();
		}
	}
}