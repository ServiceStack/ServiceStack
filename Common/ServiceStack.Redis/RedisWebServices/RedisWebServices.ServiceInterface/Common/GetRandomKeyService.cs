using RedisWebServices.ServiceModel.Operations.Common;

namespace RedisWebServices.ServiceInterface.Common
{
	public class GetRandomKeyService
		: RedisServiceBase<GetRandomKey>
	{
		protected override object Run(GetRandomKey request)
		{
			return new GetRandomKeyResponse
			{
				Key = RedisExec(r => r.GetRandomKey())
			};
		}
	}
}