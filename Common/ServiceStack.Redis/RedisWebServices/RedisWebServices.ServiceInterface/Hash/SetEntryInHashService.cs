using RedisWebServices.ServiceModel.Operations.Hash;

namespace RedisWebServices.ServiceInterface.Hash
{
	public class SetEntryInHashService
		: RedisServiceBase<SetEntryInHash>
	{
		protected override object Run(SetEntryInHash request)
		{
			return new SetEntryInHashResponse
			{
				Result = RedisExec(r => r.SetEntryInHash(request.Id, request.Key, request.Value))
			};
		}
	}
}