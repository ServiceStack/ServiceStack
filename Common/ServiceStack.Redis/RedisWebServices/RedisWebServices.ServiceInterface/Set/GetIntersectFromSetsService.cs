using RedisWebServices.ServiceModel.Operations.Set;
using ServiceStack.ServiceInterface.ServiceModel;

namespace RedisWebServices.ServiceInterface.Set
{
	public class GetIntersectFromSetsService
		: RedisServiceBase<GetIntersectFromSets>
	{
		protected override object Run(GetIntersectFromSets request)
		{
			return new GetIntersectFromSetsResponse
	       	{
	       		Items = new ArrayOfString(RedisExec(r => r.GetIntersectFromSets(request.SetIds.ToArray())))
	       	};
		}
	}
}