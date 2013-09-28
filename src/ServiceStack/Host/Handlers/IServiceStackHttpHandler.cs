using ServiceStack.Web;

namespace ServiceStack.Host.Handlers
{
	public interface IServiceStackHttpHandler
	{
		void ProcessRequest(IHttpRequest httpReq, IHttpResponse httpRes, string operationName);
	}
}