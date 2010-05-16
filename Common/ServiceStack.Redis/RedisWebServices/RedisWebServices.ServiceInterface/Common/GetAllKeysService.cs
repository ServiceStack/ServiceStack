using RedisWebServices.ServiceModel.Operations.Common;
using ServiceStack.ServiceInterface.ServiceModel;

namespace RedisWebServices.ServiceInterface.Common
{
	public class GetAllKeysService
		: RedisServiceBase<GetAllKeys>
	{
		protected override object Run(GetAllKeys request)
		{
			return new GetAllKeysResponse
			{
				Keys = new ArrayOfString(
					RedisExec(r => r.GetAllKeys())
				)
			};
		}
	}
}