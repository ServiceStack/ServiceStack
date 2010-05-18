using RedisWebServices.ServiceModel.Operations.Common;

namespace RedisWebServices.ServiceInterface.Admin
{
	public class EchoService
		: RedisServiceBase<Echo>
	{
		protected override object Run(Echo request)
		{
			return new EchoResponse
	       	{
	       		Text = RedisNativeExec(r => r.Echo(request.Text))
	       	};
		}
	}
}