using System.IO;
using System.Threading.Tasks;
using ServiceStack.Web;

namespace ServiceStack.Html;

public interface IHtmlContext
{
    IHttpRequest HttpRequest { get; }
}
    
public interface IViewEngine
{
    bool HasView(string viewName, IRequest httpReq = null);
    string RenderPartial(string pageName, object model, bool renderHtml, StreamWriter writer = null, IHtmlContext htmlHelper = null);
    Task<bool> ProcessRequestAsync(IRequest req, object dto, Stream outputStream);
}

public static class HtmlContextExtensions
{
    public static IRequest GetHttpRequest(this IHtmlContext html)
    {
        return html?.HttpRequest;
    }
}