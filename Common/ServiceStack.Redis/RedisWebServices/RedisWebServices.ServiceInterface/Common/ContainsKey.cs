using RedisWebServices.ServiceModel.Operations.Common;

namespace RedisWebServices.ServiceInterface.Common
{
	public class ContainsKeyService
		: RedisServiceBase<ContainsKey>
	{
		protected override object Run(ContainsKey request)
		{
			return new ContainsKeyResponse
			{
				Result = RedisExec(r => r.ContainsKey(request.Key))
			};
		}
	}
}