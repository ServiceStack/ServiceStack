using RedisWebServices.ServiceModel.Operations.SortedSet;

namespace RedisWebServices.ServiceInterface.SortedSet
{
	public class GetItemScoreInSortedSetService
		: RedisServiceBase<GetItemScoreInSortedSet>
	{
		protected override object Run(GetItemScoreInSortedSet request)
		{
			return new GetItemScoreInSortedSetResponse
	       	{
				Score = RedisExec(r => r.GetItemScoreInSortedSet(request.Id, request.Item))
	       	};
		}
	}
}