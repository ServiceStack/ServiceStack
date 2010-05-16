using RedisWebServices.ServiceModel.Operations.SortedSet;

namespace RedisWebServices.ServiceInterface.SortedSet
{
	public class PopItemWithHighestScoreFromSortedSetService
		: RedisServiceBase<PopItemWithHighestScoreFromSortedSet>
	{
		protected override object Run(PopItemWithHighestScoreFromSortedSet request)
		{
			return new PopItemWithHighestScoreFromSortedSetResponse
	       	{
				Item = RedisExec(r => r.PopItemWithHighestScoreFromSortedSet(request.Id))
	       	};
		}
	}
}