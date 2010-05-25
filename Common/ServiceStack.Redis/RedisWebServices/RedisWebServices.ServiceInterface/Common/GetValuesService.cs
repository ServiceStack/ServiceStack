using RedisWebServices.ServiceModel.Operations.Common;
using ServiceStack.ServiceInterface.ServiceModel;

namespace RedisWebServices.ServiceInterface.Common
{
	public class GetValuesService
		: RedisServiceBase<GetValues>
	{
		protected override object Run(GetValues request)
		{
			var response = new GetValuesResponse
			{
				Values = new ArrayOfString(
					RedisExec(r => r.GetValues(request.Keys))	
				)
			};

			return response;
		}
	}
}