using RedisWebServices.ServiceModel.Operations.Common;

namespace RedisWebServices.ServiceInterface.Common
{
	public class RemoveEntryService
		: RedisServiceBase<RemoveEntry>
	{
		protected override object Run(RemoveEntry request)
		{
			return new RemoveEntryResponse
			{
				Result = RedisExec(r => r.RemoveEntry(request.Keys.ToArray()))
			};
		}
	}
}