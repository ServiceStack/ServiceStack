using RedisWebServices.ServiceModel.Operations.SortedSet;
using ServiceStack.ServiceInterface.ServiceModel;

namespace RedisWebServices.ServiceInterface.SortedSet
{
	public class GetAllItemsFromSortedSetDescService
		: RedisServiceBase<GetAllItemsFromSortedSetDesc>
	{
		protected override object Run(GetAllItemsFromSortedSetDesc request)
		{
			return new GetAllItemsFromSortedSetDescResponse
	       	{
	       		Items = new ArrayOfString(RedisExec(r => r.GetAllItemsFromSortedSetDesc(request.Id)))
	       	};
		}
	}
}