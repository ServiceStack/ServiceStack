using RedisWebServices.ServiceModel.Operations.Hash;
using ServiceStack.ServiceInterface.ServiceModel;

namespace RedisWebServices.ServiceInterface.Hash
{
	public class GetHashValuesService
		: RedisServiceBase<GetHashValues>
	{
		protected override object Run(GetHashValues request)
		{
			return new GetHashValuesResponse
			{
				Values = new ArrayOfString(
					RedisExec(r => r.GetHashValues(request.Id))
				)
			};
		}
	}
}