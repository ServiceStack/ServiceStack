using RedisWebServices.ServiceModel.Operations.Set;

namespace RedisWebServices.ServiceInterface.Set
{
	public class PopItemFromSetService
		: RedisServiceBase<PopItemFromSet>
	{
		protected override object Run(PopItemFromSet request)
		{
			return new PopItemFromSetResponse
	       	{
				Item = RedisExec(r => r.PopItemFromSet(request.Id))
	       	};
		}
	}
}