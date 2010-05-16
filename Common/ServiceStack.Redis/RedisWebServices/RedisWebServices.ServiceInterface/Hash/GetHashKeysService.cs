using RedisWebServices.ServiceModel.Operations.Hash;
using ServiceStack.ServiceInterface.ServiceModel;

namespace RedisWebServices.ServiceInterface.Hash
{
	public class GetHashKeysService
		: RedisServiceBase<GetHashKeys>
	{
		protected override object Run(GetHashKeys request)
		{
			return new GetHashKeysResponse
			{
				Keys = new ArrayOfString(
					RedisExec(r => r.GetHashKeys(request.Id))
				)
			};
		}
	}
}