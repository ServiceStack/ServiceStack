using System.Net;
using ServiceStack.Formats;
using ServiceStack.Host.Handlers;
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

        public override void ProcessRequest(IRequest httpReq, IResponse httpRes, string operationName)
        {
            HostContext.ApplyCustomHandlerRequestFilters(httpReq, httpRes);
            if (httpRes.IsClosed) return;

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

            MarkdownFormat.ProcessMarkdownPage(httpReq, contentPage, model, httpRes.OutputStream);
        }
    }
}