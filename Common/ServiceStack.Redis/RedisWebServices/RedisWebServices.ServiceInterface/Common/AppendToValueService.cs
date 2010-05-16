using RedisWebServices.ServiceModel.Operations.Common;

namespace RedisWebServices.ServiceInterface.Common
{
	public class AppendToValueService
		: RedisServiceBase<AppendToValue>
	{
		protected override object Run(AppendToValue request)
		{
			return new AppendToValueResponse
			{
				ValueLength = RedisExec(r => r.AppendToValue(request.Key, request.Value))
			};
		}
	}
}