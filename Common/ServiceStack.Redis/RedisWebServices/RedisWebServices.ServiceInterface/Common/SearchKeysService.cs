using System;
using RedisWebServices.ServiceModel.Operations.Common;
using ServiceStack.ServiceInterface.ServiceModel;

namespace RedisWebServices.ServiceInterface.Common
{
	public class SearchKeysService
		: RedisServiceBase<SearchKeys>
	{
		protected override object Run(SearchKeys request)
		{
			return new SearchKeysResponse
			{
				Keys = new ArrayOfString(
					RedisExec(r => r.SearchKeys(request.Pattern))
				)
			};
		}
	}
}