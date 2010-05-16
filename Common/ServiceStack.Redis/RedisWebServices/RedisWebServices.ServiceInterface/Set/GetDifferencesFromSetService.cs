using RedisWebServices.ServiceModel.Operations.Set;
using ServiceStack.ServiceInterface.ServiceModel;

namespace RedisWebServices.ServiceInterface.Set
{
	public class GetDifferencesFromSetService
		: RedisServiceBase<GetDifferencesFromSet>
	{
		protected override object Run(GetDifferencesFromSet request)
		{
			return new GetDifferencesFromSetResponse
	       	{
	       		Items = new ArrayOfString(RedisExec(r => r.GetDifferencesFromSet(
					request.Id, request.SetIds.ToArray())))
	       	};
		}
	}
}