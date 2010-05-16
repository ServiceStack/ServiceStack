using RedisWebServices.ServiceModel.Operations.Admin;

namespace RedisWebServices.ServiceInterface.Admin
{
	public class SlaveOfService
		: RedisServiceBase<SlaveOf>
	{
		protected override object Run(SlaveOf request)
		{
			if (request.NoOne)
			{
				RedisNativeExec(r => r.SlaveOfNoOne());
			}
			else
			{
				RedisNativeExec(r => r.SlaveOf(request.Host, request.Port));
			}
			
			return new SlaveOfResponse();
		}
	}
}