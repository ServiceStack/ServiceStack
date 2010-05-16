using RedisWebServices.ServiceModel.Operations.Set;

namespace RedisWebServices.ServiceInterface.Set
{
	public class SetContainsItemService
		: RedisServiceBase<SetContainsItem>
	{
		protected override object Run(SetContainsItem request)
		{
			return new SetContainsItemResponse
	       	{
				Result = RedisExec(r => r.SetContainsItem(request.Id, request.Item))
	       	};
		}
	}
}