using RedisWebServices.ServiceModel.Operations.Set;

namespace RedisWebServices.ServiceInterface.Set
{
	public class AddItemToSetService
		: RedisServiceBase<AddItemToSet>
	{
		protected override object Run(AddItemToSet request)
		{
			RedisExec(r => r.AddItemToSet(request.Id, request.Item));

			return new AddItemToSetResponse();
		}
	}
}