using RedisWebServices.ServiceModel.Operations.Set;
using ServiceStack.ServiceInterface.ServiceModel;

namespace RedisWebServices.ServiceInterface.Set
{
	public class GetUnionFromSetsService
		: RedisServiceBase<GetUnionFromSets>
	{
		protected override object Run(GetUnionFromSets request)
		{
			return new GetUnionFromSetsResponse
	       	{
	       		Items = new ArrayOfString(RedisExec(r => r.GetUnionFromSets(request.SetIds.ToArray())))
	       	};
		}
	}
}