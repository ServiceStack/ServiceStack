using ServiceStack.ServiceHost;

namespace ServiceStack.Html
{
	public interface IViewEngine
	{
        string RenderPartial(string pageName, object model, bool renderHtml);
        bool ProcessRequest(IHttpRequest httpReq, IHttpResponse httpRes, object dto);
	}
}