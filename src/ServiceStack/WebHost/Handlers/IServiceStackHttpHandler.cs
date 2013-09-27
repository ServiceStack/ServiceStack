using ServiceStack.Server;

namespace ServiceStack.WebHost.Handlers
{
	public interface IServiceStackHttpHandler
	{
		void ProcessRequest(IHttpRequest httpReq, IHttpResponse httpRes, string operationName);
	}
}