using RedisWebServices.ServiceModel.Operations.Hash;

namespace RedisWebServices.ServiceInterface.Hash
{
	public class RemoveEntryFromHashService
		: RedisServiceBase<RemoveEntryFromHash>
	{
		protected override object Run(RemoveEntryFromHash request)
		{
			return new RemoveEntryFromHashResponse
			{
				Result = RedisExec(r => r.RemoveEntryFromHash(request.Id, request.Key))
			};
		}
	}
}