using RedisWebServices.ServiceModel.Operations.Common;

namespace RedisWebServices.ServiceInterface.Common
{
	public class DecrementValueService
		: RedisServiceBase<DecrementValue>
	{
		protected override object Run(DecrementValue request)
		{
			return new DecrementValueResponse
			{
				Value = RedisExec(r => r.DecrementValueBy(
					request.Key, request.DecrementBy.GetValueOrDefault(1)))
			};
		}
	}
}