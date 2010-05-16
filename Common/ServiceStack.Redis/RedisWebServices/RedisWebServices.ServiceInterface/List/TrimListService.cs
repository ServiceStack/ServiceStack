using RedisWebServices.ServiceModel.Operations.List;

namespace RedisWebServices.ServiceInterface.List
{
	public class TrimListService
		: RedisServiceBase<TrimList>
	{
		protected override object Run(TrimList request)
		{
			RedisExec(r => r.TrimList(request.Id, request.KeepStartingFrom, request.KeepEndingAt));

			return new TrimListResponse();
		}
	}
}