using System;
using RedisWebServices.ServiceModel.Operations.Common;

namespace RedisWebServices.ServiceInterface.Common
{
	public class ExpireEntryAtService
		: RedisServiceBase<ExpireEntryAt>
	{
		protected override object Run(ExpireEntryAt request)
		{
			return new ExpireEntryAtResponse
			{
				Result = RedisExec(r => r.ExpireEntryAt(request.Key, request.ExpireAt))
			};
		}
	}
}