using RedisWebServices.ServiceModel.Operations.SortedSet;

namespace RedisWebServices.ServiceInterface.SortedSet
{
	public class GetSortedSetCountService
		: RedisServiceBase<GetSortedSetCount>
	{
		protected override object Run(GetSortedSetCount request)
		{
			return new GetSortedSetCountResponse
	       	{
				Count = RedisExec(r => r.GetSortedSetCount(request.Id))
	       	};
		}
	}
}