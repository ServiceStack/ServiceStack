using RedisWebServices.ServiceModel.Operations.SortedSet;

namespace RedisWebServices.ServiceInterface.SortedSet
{
	public class RemoveRangeFromSortedSetService
		: RedisServiceBase<RemoveRangeFromSortedSet>
	{
		protected override object Run(RemoveRangeFromSortedSet request)
		{
			return new RemoveRangeFromSortedSetResponse
	       	{
				ItemsRemovedCount = RedisExec(r => r.RemoveRangeFromSortedSet(
					request.Id, request.FromRank, request.ToRank))
	       	};
		}
	}
}