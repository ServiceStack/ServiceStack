using RedisWebServices.ServiceModel.Operations.Hash;
using RedisWebServices.ServiceModel.Types;

namespace RedisWebServices.ServiceInterface.Hash
{
	public class GetAllEntriesFromHashService
		: RedisServiceBase<GetAllEntriesFromHash>
	{
		protected override object Run(GetAllEntriesFromHash request)
		{
			return new GetAllEntriesFromHashResponse
			{
				KeyValuePairs = new ArrayOfKeyValuePair(
					RedisExec(r => r.GetAllEntriesFromHash(request.Id))
				)
			};
		}
	}
}