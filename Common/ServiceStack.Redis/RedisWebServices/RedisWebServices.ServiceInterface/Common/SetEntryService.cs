using RedisWebServices.ServiceModel.Operations.Common;

namespace RedisWebServices.ServiceInterface.Common
{
	public class SetEntryService
		: RedisServiceBase<SetEntry>
	{
		protected override object Run(SetEntry request)
		{
			RedisExec(r => r.SetEntry(request.Key, request.Value));
			
			return new SetEntryResponse();
		}
	}
}