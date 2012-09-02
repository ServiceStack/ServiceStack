using ServiceStack.ServiceHost;

namespace ServiceStack.Html
{
	public interface IViewEngine
	{
        bool HasView(string viewName, IHttpRequest httpReq = null);
        string RenderPartial(string pageName, object model, bool renderHtml, IHttpRequest httpReq = null);
        bool ProcessRequest(IHttpRequest httpReq, IHttpResponse httpRes, object dto);
	}
}