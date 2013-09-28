using System.Net;
using ServiceStack.Formats;
using ServiceStack.Host.Handlers;
using ServiceStack.Support.WebHost;
using ServiceStack.Web;

namespace ServiceStack.Support.Markdown
{
    public class MarkdownHandler : ServiceStackHandlerBase
    {
        public MarkdownFormat MarkdownFormat { get; set; }
        public MarkdownPage MarkdownPage { get; set; }
        public object Model { get; set; }

        public string PathInfo { get; set; }

        public MarkdownHandler(string pathInfo)
        {
            PathInfo = pathInfo;
        }

        public override void ProcessRequest(IHttpRequest httpReq, IHttpResponse httpRes, string operationName)
        {
            if (MarkdownFormat == null)
                MarkdownFormat = MarkdownFormat.Instance;

            var contentPage = MarkdownPage ?? MarkdownFormat.FindByPathInfo(PathInfo);
            if (contentPage == null)
            {
                httpRes.StatusCode = (int)HttpStatusCode.NotFound;
                httpRes.EndHttpHandlerRequest();
                return;
            }

            MarkdownFormat.ReloadModifiedPageAndTemplates(contentPage);

            if (httpReq.DidReturn304NotModified(contentPage.GetLastModified(), httpRes))
                return;

            var model = Model;
            if (model == null)
                httpReq.Items.TryGetValue("Model", out model);

            MarkdownFormat.ProcessMarkdownPage(httpReq, contentPage, model, httpRes);
        }

        public override object CreateRequest(IHttpRequest request, string operationName)
        {
            return null;
        }

        public override object GetResponse(IHttpRequest httpReq, IHttpResponse httpRes, object request)
        {
            return null;
        }
    }
}