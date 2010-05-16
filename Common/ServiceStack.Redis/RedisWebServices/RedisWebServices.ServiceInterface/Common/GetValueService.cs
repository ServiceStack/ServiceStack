using RedisWebServices.ServiceModel.Operations.Common;

namespace RedisWebServices.ServiceInterface.Common
{
	public class GetValueService
		: RedisServiceBase<GetValue>
	{
		protected override object Run(GetValue request)
		{
			return new GetValueResponse
			{
				Value = RedisExec(r => r.GetValue(request.Key))
			};
		}
	}
}