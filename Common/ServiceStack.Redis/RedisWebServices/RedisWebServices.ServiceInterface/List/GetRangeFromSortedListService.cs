using RedisWebServices.ServiceModel.Operations.List;
using ServiceStack.ServiceInterface.ServiceModel;

namespace RedisWebServices.ServiceInterface.List
{
	public class GetRangeFromSortedListService
		: RedisServiceBase<GetRangeFromSortedList>
	{
		protected override object Run(GetRangeFromSortedList request)
		{
			var response = new GetRangeFromSortedListResponse
			{
				Items = new ArrayOfString(
					RedisExec(r => r.GetRangeFromSortedList(request.Id, request.StartingFrom, request.EndingAt))
				)
			};

			return response;
		}
	}
}