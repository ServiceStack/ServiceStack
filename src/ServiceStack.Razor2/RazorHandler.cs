using System.Net;
using ServiceStack.Common.Web;
using ServiceStack.ServiceHost;
using ServiceStack.WebHost.Endpoints.Extensions;
using ServiceStack.WebHost.Endpoints.Support;

namespace ServiceStack.Razor
{
    public class RazorHandler : EndpointHandlerBase
    {
        public RazorFormat RazorFormat { get; set; }
        public ViewPageRef RazorPage { get; set; }
        public object Model { get; set; }

        public string PathInfo { get; set; }

        public RazorHandler(string pathInfo)
        {
            PathInfo = pathInfo;
        }

        public override void ProcessRequest(IHttpRequest httpReq, IHttpResponse httpRes, string operationName)
        {
            httpRes.ContentType = ContentType.Html;
            if (RazorFormat == null)
                RazorFormat = RazorFormat.Instance;

            var contentPage = RazorPage ?? RazorFormat.FindByPathInfo(PathInfo);
            if (contentPage == null)
            {
                httpRes.StatusCode = (int)HttpStatusCode.NotFound;
                httpRes.EndHttpRequest();
                return;
            }

            if (RazorFormat.WatchForModifiedPages)
                RazorFormat.ReloadIfNeeeded(contentPage);

            //Add good caching support
            //if (httpReq.DidReturn304NotModified(contentPage.GetLastModified(), httpRes))
            //    return;

            var model = Model;
            if (model == null)
                httpReq.Items.TryGetValue("Model", out model);
            if (model == null)
            {
                var modelType = RazorPage != null ? RazorPage.GetRazorTemplate().ModelType : null;
                model = modelType == null || modelType == typeof(DynamicRequestObject)
                    ? null
                    : DeserializeHttpRequest(modelType, httpReq, httpReq.ContentType);
            }

            RazorFormat.ProcessRazorPage(httpReq, contentPage, model, httpRes);
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