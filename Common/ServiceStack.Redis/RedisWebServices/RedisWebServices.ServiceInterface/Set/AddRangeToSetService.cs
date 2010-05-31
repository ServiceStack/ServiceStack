using RedisWebServices.ServiceModel.Operations.Set;
using ServiceStack.Text;

namespace RedisWebServices.ServiceInterface.Set
{
	public class AddRangeToSetService
		: RedisServiceBase<AddRangeToSet>
	{
		protected override object Run(AddRangeToSet request)
		{
			if (!request.Items.IsNullOrEmpty())
			{
				RedisExec(r => request.Items.ForEach(x => r.AddItemToSet(request.Id, x)));
			}

			return new AddRangeToSetResponse();
		}
	}
}