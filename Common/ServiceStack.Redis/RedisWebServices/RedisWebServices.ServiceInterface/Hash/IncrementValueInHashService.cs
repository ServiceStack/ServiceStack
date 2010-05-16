using RedisWebServices.ServiceModel.Operations.Hash;

namespace RedisWebServices.ServiceInterface.Hash
{
	public class IncrementValueInHashService
		: RedisServiceBase<IncrementValueInHash>
	{
		protected override object Run(IncrementValueInHash request)
		{
			return new IncrementValueInHashResponse
			{
				Value = RedisExec(r => r.IncrementValueInHash(
					request.Id, request.Key, request.IncrementBy.GetValueOrDefault(1)))
			};
		}
	}
}