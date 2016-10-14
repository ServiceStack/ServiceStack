using System.Net;
using ServiceStack.Host.Handlers;
using ServiceStack.Razor.Managers;
using ServiceStack.Web;

namespace ServiceStack.Razor
{
    public class RazorHandler : ServiceStackHandlerBase
    {
        public RazorFormat RazorFormat { get; set; }
        public RazorPage RazorPage { get; set; }
        public object Model { get; set; }

        public string PathInfo { get; set; }

        public RazorHandler(string pathInfo)
        {
            PathInfo = pathInfo;
        }

        public override void ProcessRequest(IRequest httpReq, IResponse httpRes, string operationName)
        {
            HostContext.ApplyCustomHandlerRequestFilters(httpReq, httpRes);
            if (httpRes.IsClosed) return;

            httpRes.ContentType = MimeTypes.Html;
            if (RazorFormat == null)
                RazorFormat = RazorFormat.Instance;

            var contentPage = RazorPage ?? RazorFormat.GetContentPage(PathInfo);
            if (contentPage == null)
            {
                httpRes.StatusCode = (int)HttpStatusCode.NotFound;
                httpRes.EndHttpHandlerRequest();
                return;
            }

            var model = Model;
            if (model == null)
                httpReq.Items.TryGetValue("Model", out model);
            if (model == null)
            {
                var modelType = RazorPage?.ModelType;
                model = modelType == null || modelType == typeof(DynamicRequestObject)
                    ? null
                    : DeserializeHttpRequest(modelType, httpReq, httpReq.ContentType);
            }

            RazorFormat.ProcessRazorPage(httpReq, contentPage, model, httpRes);
            httpRes.EndHttpHandlerRequest(skipHeaders:true);
        }

        public override object CreateRequest(IRequest request, string operationName)
        {
            return null;
        }

        public override object GetResponse(IRequest httpReq, object request)
        {
            return null;
        }
    }
}