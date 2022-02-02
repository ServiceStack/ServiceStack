using System;
using System.Net;
using System.Threading.Tasks;
using ServiceStack.Host.Handlers;
using ServiceStack.Razor.Managers;
using ServiceStack.Web;

namespace ServiceStack.Razor
{
    public class RazorHandler : ServiceStackHandlerBase
    {
        public Action<IRequest> Filter { get; set; }
        public RazorFormat RazorFormat { get; set; }
        public RazorPage RazorPage { get; set; }
        public object Model { get; set; }

        public string PathInfo { get; set; }

        public RazorHandler(string pathInfo)
        {
            PathInfo = pathInfo;
        }

        public override bool RunAsAsync() => true;

        public override async Task ProcessRequestAsync(IRequest httpReq, IResponse httpRes, string operationName)
        {
            if (HostContext.ApplyCustomHandlerRequestFilters(httpReq, httpRes))
                return;

            Filter?.Invoke(httpReq);

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
                    : await DeserializeHttpRequestAsync(modelType, httpReq, httpReq.ContentType);
            }

            using (RazorFormat.ProcessRazorPage(httpReq, contentPage, model, httpRes))
            {
                httpRes.EndHttpHandlerRequest(skipHeaders: true);
            }
        }
    }
}