using RedisWebServices.ServiceModel.Operations.Set;

namespace RedisWebServices.ServiceInterface.Set
{
	public class GetSetCountService
		: RedisServiceBase<GetSetCount>
	{
		protected override object Run(GetSetCount request)
		{
			return new GetSetCountResponse
	       	{
				Count = RedisExec(r => r.GetSetCount(request.Id))
	       	};
		}
	}
}