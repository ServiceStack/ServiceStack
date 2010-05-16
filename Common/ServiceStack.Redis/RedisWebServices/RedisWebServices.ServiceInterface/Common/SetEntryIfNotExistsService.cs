using RedisWebServices.ServiceModel.Operations.Common;

namespace RedisWebServices.ServiceInterface.Common
{
	public class SetEntryIfNotExistsService
		: RedisServiceBase<SetEntryIfNotExists>
	{
		protected override object Run(SetEntryIfNotExists request)
		{
			return new SetEntryIfNotExistsResponse
       		{
				Result = RedisExec(r => r.SetEntryIfNotExists(request.Key, request.Value))
       		};
		}
	}
}