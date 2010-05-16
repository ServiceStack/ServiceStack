using RedisWebServices.ServiceModel.Operations.Hash;

namespace RedisWebServices.ServiceInterface.Hash
{
	public class GetHashCountService
		: RedisServiceBase<GetHashCount>
	{
		protected override object Run(GetHashCount request)
		{
			return new GetHashCountResponse
			{
				Count = RedisExec(r => r.GetHashCount(request.Id))
			};
		}
	}
}