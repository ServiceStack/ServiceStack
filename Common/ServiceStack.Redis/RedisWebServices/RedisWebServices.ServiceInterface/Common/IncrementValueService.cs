using RedisWebServices.ServiceModel.Operations.Common;

namespace RedisWebServices.ServiceInterface.Common
{
	public class IncrementValueService
		: RedisServiceBase<IncrementValue>
	{
		protected override object Run(IncrementValue request)
		{
			return new IncrementValueResponse
			{
				Value = RedisExec(r => r.IncrementValueBy(request.Key, request.IncrementBy.GetValueOrDefault(1)))
			};
		}
	}
}