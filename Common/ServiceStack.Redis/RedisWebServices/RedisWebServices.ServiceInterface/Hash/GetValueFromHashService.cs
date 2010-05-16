using RedisWebServices.ServiceModel.Operations.Hash;

namespace RedisWebServices.ServiceInterface.Hash
{
	public class GetValueFromHashService
		: RedisServiceBase<GetValueFromHash>
	{
		protected override object Run(GetValueFromHash request)
		{
			return new GetValueFromHashResponse
			{
				Value = RedisExec(r => r.GetValueFromHash(request.Id, request.Key))
			};
		}
	}
}