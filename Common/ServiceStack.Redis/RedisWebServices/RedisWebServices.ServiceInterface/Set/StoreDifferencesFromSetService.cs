using RedisWebServices.ServiceModel.Operations.Set;

namespace RedisWebServices.ServiceInterface.Set
{
	public class StoreDifferencesFromSetService
		: RedisServiceBase<StoreDifferencesFromSet>
	{
		protected override object Run(StoreDifferencesFromSet request)
		{
			RedisExec(r => r.StoreDifferencesFromSet(request.Id, request.FromSetId, request.SetIds.ToArray()));

			return new StoreDifferencesFromSetResponse();
		}
	}
}