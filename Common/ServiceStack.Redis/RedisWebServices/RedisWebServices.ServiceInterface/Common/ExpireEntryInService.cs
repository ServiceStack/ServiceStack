using System;
using RedisWebServices.ServiceModel.Operations.Common;

namespace RedisWebServices.ServiceInterface.Common
{
	public class ExpireEntryInService
		: RedisServiceBase<ExpireEntryIn>
	{
		protected override object Run(ExpireEntryIn request)
		{
			return new ExpireEntryInResponse
			{
				Result = RedisExec(r => r.ExpireEntryIn(request.Key, request.ExpireIn))
			};
		}
	}
}