using System.IO;
using ServiceStack.Web;

namespace ServiceStack.Html
{
    public interface IViewEngine
    {
        bool HasView(string viewName, IRequest httpReq = null);
        string RenderPartial(string pageName, object model, bool renderHtml, StreamWriter writer = null, HtmlHelper htmlHelper = null);
        bool ProcessRequest(IRequest req, IResponse res, object dto);
    }
}