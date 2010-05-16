using RedisWebServices.ServiceModel.Operations.List;
using ServiceStack.ServiceInterface.ServiceModel;

namespace RedisWebServices.ServiceInterface.List
{
	public class GetRangeFromListService
		: RedisServiceBase<GetRangeFromList>
	{
		protected override object Run(GetRangeFromList request)
		{
			return new GetRangeFromListResponse
			{
				Items = new ArrayOfString(
					RedisExec(r => r.GetRangeFromList(request.Id, request.StartingFrom, request.EndingAt))
				)
			};
		}
	}
}