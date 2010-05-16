using RedisWebServices.ServiceModel.Operations.Set;

namespace RedisWebServices.ServiceInterface.Set
{
	public class MoveBetweenSetsService
		: RedisServiceBase<MoveBetweenSets>
	{
		protected override object Run(MoveBetweenSets request)
		{
			RedisExec(r => r.MoveBetweenSets(request.FromSetId, request.ToSetId, request.Item));

			return new MoveBetweenSetsResponse();
		}
	}
}