using RedisWebServices.ServiceModel.Operations.Common;

namespace RedisWebServices.ServiceInterface.Common
{
	public class SetEntryWithExpiryService
		: RedisServiceBase<SetEntryWithExpiry>
	{
		protected override object Run(SetEntryWithExpiry request)
		{
			RedisExec(r => r.SetEntry(request.Key, request.Value, request.ExpireIn));
			
			return new SetEntryWithExpiryResponse();
		}
	}
}