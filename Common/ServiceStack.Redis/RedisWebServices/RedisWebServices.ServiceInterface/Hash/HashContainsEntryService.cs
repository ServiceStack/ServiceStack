using RedisWebServices.ServiceModel.Operations.Hash;

namespace RedisWebServices.ServiceInterface.Hash
{
	public class HashContainsEntryService
		: RedisServiceBase<HashContainsEntry>
	{
		protected override object Run(HashContainsEntry request)
		{
			return new HashContainsEntryResponse
			{
				Result= RedisExec(r => r.HashContainsEntry(request.Id, request.Key))
			};
		}
	}
}