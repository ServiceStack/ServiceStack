using RedisWebServices.ServiceModel.Operations.Common;
using RedisWebServices.ServiceModel.Types;

namespace RedisWebServices.ServiceInterface.Common
{
	public class GetEntryTypesService
		: RedisServiceBase<GetEntryTypes>
	{
		protected override object Run(GetEntryTypes request)
		{
			var response = new GetEntryTypesResponse();
			using (var redis = ClientsManager.GetClient())
			{
				foreach (var key in request.Keys)
				{
					var keyType = redis.GetEntryType(key);
					response.KeyTypes.Add(new KeyValuePair(key, keyType.ToString()));
				}
			}

			return response;
		}
	}
}