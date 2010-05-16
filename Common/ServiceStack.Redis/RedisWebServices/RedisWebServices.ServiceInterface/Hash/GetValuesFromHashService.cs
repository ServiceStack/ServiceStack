using RedisWebServices.ServiceModel.Operations.Hash;
using ServiceStack.ServiceInterface.ServiceModel;

namespace RedisWebServices.ServiceInterface.Hash
{
	public class GetValuesFromHashService
		: RedisServiceBase<GetValuesFromHash>
	{
		protected override object Run(GetValuesFromHash request)
		{
			return new GetValuesFromHashResponse
			{
				Values = new ArrayOfString(
					RedisExec(r => r.GetValuesFromHash(request.Id, request.Keys.ToArray()))
				)
			};
		}
	}
}