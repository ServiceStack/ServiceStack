using RedisWebServices.ServiceModel.Operations.Common;

namespace RedisWebServices.ServiceInterface.Common
{
	public class GetAndSetEntryService
		: RedisServiceBase<GetAndSetEntry>
	{
		protected override object Run(GetAndSetEntry request)
		{
			return new GetAndSetEntryResponse
			{
				ExistingValue = RedisExec(r => r.GetAndSetEntry(request.Key, request.Value))
			};
		}
	}
}