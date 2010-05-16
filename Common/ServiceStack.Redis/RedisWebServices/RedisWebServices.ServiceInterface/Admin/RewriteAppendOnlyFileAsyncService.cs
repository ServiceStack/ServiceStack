using RedisWebServices.ServiceModel.Operations.Admin;

namespace RedisWebServices.ServiceInterface.Admin
{
	public class RewriteAppendOnlyFileAsyncService
		: RedisServiceBase<RewriteAppendOnlyFileAsync>
	{
		protected override object Run(RewriteAppendOnlyFileAsync request)
		{
			RedisExec(r => r.RewriteAppendOnlyFileAsync());
			
			return new RewriteAppendOnlyFileAsyncResponse();
		}
	}
}