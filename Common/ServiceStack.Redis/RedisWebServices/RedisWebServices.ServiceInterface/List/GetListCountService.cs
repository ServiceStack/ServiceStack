using RedisWebServices.ServiceModel.Operations.List;

namespace RedisWebServices.ServiceInterface.List
{
	public class GetListCountService
		: RedisServiceBase<GetListCount>
	{
		protected override object Run(GetListCount request)
		{
			return new GetListCountResponse
			{
				Count = RedisExec(r => r.GetListCount(request.Id))
			};
		}
	}
}