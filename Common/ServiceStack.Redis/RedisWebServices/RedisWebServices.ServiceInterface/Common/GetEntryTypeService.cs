using RedisWebServices.ServiceModel.Operations.Common;

namespace RedisWebServices.ServiceInterface.Common
{
	public class GetEntryTypeService
		: RedisServiceBase<GetEntryType>
	{
		protected override object Run(GetEntryType request)
		{
			return new GetEntryTypeResponse
			{
				KeyType = RedisExec(r => r.GetEntryType(request.Key)).ToString()
			};
		}
	}
}