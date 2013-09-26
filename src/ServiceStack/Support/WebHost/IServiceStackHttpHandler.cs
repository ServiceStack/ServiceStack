using ServiceStack.Server;

namespace ServiceStack.Support.WebHost
{
	public interface IServiceStackHttpHandler
	{
		void ProcessRequest(IHttpRequest httpReq, IHttpResponse httpRes, string operationName);
	}
}