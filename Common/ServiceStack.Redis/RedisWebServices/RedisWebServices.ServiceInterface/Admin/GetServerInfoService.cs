using RedisWebServices.ServiceModel.Operations.Common;
using RedisWebServices.ServiceModel.Types;

namespace RedisWebServices.ServiceInterface.Admin
{
	public class GetServerInfoService
		: RedisServiceBase<GetServerInfo>
	{
		protected override object Run(GetServerInfo request)
		{
			return new GetServerInfoResponse
	       	{
				ServerInfo = new ArrayOfKeyValuePair(RedisNativeExec(r => r.Info))
	       	};
		}
	}
}