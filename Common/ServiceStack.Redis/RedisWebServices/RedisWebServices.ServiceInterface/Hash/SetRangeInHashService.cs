using System.Collections.Generic;
using RedisWebServices.ServiceModel.Operations.Hash;

namespace RedisWebServices.ServiceInterface.Hash
{
	public class SetRangeInHashService
		: RedisServiceBase<SetRangeInHash>
	{
		protected override object Run(SetRangeInHash request)
		{
			var map = new Dictionary<string, string>();
			request.KeyValuePairs.ForEach(kvp => map[kvp.Key] = kvp.Value);

			RedisExec(r => r.SetRangeInHash(request.Id, map));

			return new SetRangeInHashResponse();
		}
	}
}