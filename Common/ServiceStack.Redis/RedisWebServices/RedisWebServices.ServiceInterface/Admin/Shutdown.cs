using RedisWebServices.ServiceModel.Operations.Admin;

namespace RedisWebServices.ServiceInterface.Admin
{
	public class ShutdownService
		: RedisServiceBase<Shutdown>
	{
		protected override object Run(Shutdown request)
		{
			RedisNativeExec(r => r.Shutdown());
			
			return new ShutdownResponse();
		}
	}
}