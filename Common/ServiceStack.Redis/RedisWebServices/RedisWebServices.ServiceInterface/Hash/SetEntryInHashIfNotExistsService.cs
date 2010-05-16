using RedisWebServices.ServiceModel.Operations.Hash;

namespace RedisWebServices.ServiceInterface.Hash
{
	public class SetEntryInHashIfNotExistsService
		: RedisServiceBase<SetEntryInHashIfNotExists>
	{
		protected override object Run(SetEntryInHashIfNotExists request)
		{
			return new SetEntryInHashIfNotExistsResponse
			{
				Result = RedisExec(r => r.SetEntryInHashIfNotExists(request.Id, request.Key, request.Value))
			};
		}
	}
}