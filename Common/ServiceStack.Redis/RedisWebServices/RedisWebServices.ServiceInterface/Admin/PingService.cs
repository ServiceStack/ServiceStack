using RedisWebServices.ServiceModel.Operations.Common;

namespace RedisWebServices.ServiceInterface.Admin
{
	public class PingService
		: RedisServiceBase<Ping>
	{
		protected override object Run(Ping request)
		{
			return new PingResponse
	       	{
	       		Result = RedisNativeExec(r => r.Ping())
	       	};
		}
	}
}