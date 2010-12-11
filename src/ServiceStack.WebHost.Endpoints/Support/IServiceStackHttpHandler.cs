using System.Web;
using ServiceStack.WebHost.Endpoints.Extensions;

namespace ServiceStack.WebHost.Endpoints.Support
{
	public interface IServiceStackHttpHandler
	{
		void ProcessRequest(IHttpRequest httpReq, IHttpResponse httpRes, string operationName);
	}
}