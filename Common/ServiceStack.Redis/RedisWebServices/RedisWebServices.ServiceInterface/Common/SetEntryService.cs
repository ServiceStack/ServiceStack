using RedisWebServices.ServiceModel.Operations.Common;

namespace RedisWebServices.ServiceInterface.Common
{
	public class SetEntryService
		: RedisServiceBase<SetEntry>
	{
		protected override object Run(SetEntry request)
		{
			if (request.ExpireIn.HasValue)
			{
				RedisExec(r => r.SetEntry(request.Key, request.Value, request.ExpireIn.Value));
			}
			else
			{
				RedisExec(r => r.SetEntry(request.Key, request.Value));
			}
			
			return new SetEntryResponse();
		}
	}
}