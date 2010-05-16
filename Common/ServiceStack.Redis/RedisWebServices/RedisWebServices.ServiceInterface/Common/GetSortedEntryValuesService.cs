using RedisWebServices.ServiceModel.Operations.Common;
using ServiceStack.ServiceInterface.ServiceModel;

namespace RedisWebServices.ServiceInterface.Common
{
	public class GetSortedEntryValuesService
		: RedisServiceBase<GetSortedEntryValues>
	{
		protected override object Run(GetSortedEntryValues request)
		{
			return new GetSortedEntryValuesResponse
			{
				Values = new ArrayOfString(
					RedisExec(r => r.GetSortedEntryValues(request.Key,
						request.StartingFrom, request.EndingAt))
				)
			};
		}
	}
}