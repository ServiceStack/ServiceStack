using RedisWebServices.ServiceModel.Operations.Admin;

namespace RedisWebServices.ServiceInterface.Admin
{
	public class FlushAllService
		: RedisServiceBase<FlushAll>
	{
		protected override object Run(FlushAll request)
		{
			RedisNativeExec(r => r.FlushAll());
			
			return new FlushAllResponse();
		}
	}
}