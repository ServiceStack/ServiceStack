using RedisWebServices.ServiceModel.Operations.SortedSet;

namespace RedisWebServices.ServiceInterface.SortedSet
{
	public class PopItemWithLowestScoreFromSortedSetService
		: RedisServiceBase<PopItemWithLowestScoreFromSortedSet>
	{
		protected override object Run(PopItemWithLowestScoreFromSortedSet request)
		{
			return new PopItemWithLowestScoreFromSortedSetResponse
	       	{
				Item = RedisExec(r => r.PopItemWithLowestScoreFromSortedSet(request.Id))
	       	};
		}
	}
}