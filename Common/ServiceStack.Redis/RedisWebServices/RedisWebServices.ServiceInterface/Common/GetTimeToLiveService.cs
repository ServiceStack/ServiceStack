using RedisWebServices.ServiceModel.Operations.Common;

namespace RedisWebServices.ServiceInterface.Common
{
	public class GetTimeToLiveService
		: RedisServiceBase<GetTimeToLive>
	{
		protected override object Run(GetTimeToLive request)
		{
			return new GetTimeToLiveResponse
			{
				TimeRemaining = RedisExec(r => r.GetTimeToLive(request.Key))
			};
		}
	}
}