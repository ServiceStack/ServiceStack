using RedisWebServices.ServiceModel.Operations.Common;

namespace RedisWebServices.ServiceInterface.Common
{
	public class GetSubstringService
		: RedisServiceBase<GetSubstring>
	{
		protected override object Run(GetSubstring request)
		{
			return new GetSubstringResponse
			{
				Value = RedisExec(r => r.GetSubstring(request.Key, request.FromIndex, request.ToIndex))
			};
		}
	}
}