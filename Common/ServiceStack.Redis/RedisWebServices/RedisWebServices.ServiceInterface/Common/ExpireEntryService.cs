using System;
using RedisWebServices.ServiceModel.Operations.Common;

namespace RedisWebServices.ServiceInterface.Common
{
	public class ExpireEntryService
		: RedisServiceBase<ExpireEntry>
	{
		protected override object Run(ExpireEntry request)
		{
			var success = request.ExpireAt.HasValue
              	? RedisExec(r => r.ExpireEntryAt(request.Key, request.ExpireAt.Value))
              	: RedisExec(r => r.ExpireEntryIn(request.Key, request.ExpireIn.GetValueOrDefault(TimeSpan.Zero)));

			return new ExpireEntryResponse
			{
				Result = success
			};
		}
	}
}