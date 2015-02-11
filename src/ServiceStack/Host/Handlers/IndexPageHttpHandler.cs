using System.Net;
using System.Web;
using ServiceStack.Web;

namespace ServiceStack.Host.Handlers
{
    public class IndexPageHttpHandler : HttpAsyncTaskHandler
    {
        public IndexPageHttpHandler()
        {
            this.RequestName = GetType().Name;
        }

        /// <summary>
        /// Non ASP.NET requests
        /// </summary>
        public override void ProcessRequest(IRequest request, IResponse response, string operationName)
        {
            var defaultUrl = HostContext.Config.ServiceEndpointsMetadataConfig.DefaultMetadataUri;

            if (request.PathInfo == "/")
            {
                var relativeUrl = defaultUrl.Substring(defaultUrl.IndexOf('/'));
                var absoluteUrl = request.RawUrl.TrimEnd('/') + relativeUrl;
                response.StatusCode = (int)HttpStatusCode.Redirect;
                response.AddHeader(HttpHeaders.Location, absoluteUrl);
            }
            else
            {
                response.StatusCode = (int)HttpStatusCode.Redirect;
                response.AddHeader(HttpHeaders.Location, defaultUrl);
            }
        }

        /// <summary>
        /// ASP.NET requests
        /// </summary>
        public override void ProcessRequest(HttpContextBase context)
        {
            var defaultUrl = HostContext.Config.ServiceEndpointsMetadataConfig.DefaultMetadataUri;

            if (context.Request.PathInfo == "/"
                || context.Request.FilePath.EndsWith("/"))
            {
                //new NotFoundHttpHandler().ProcessRequest(context); return;

                var relativeUrl = defaultUrl.Substring(defaultUrl.IndexOf('/'));
                var absoluteUrl = context.Request.Url.AbsoluteUri.TrimEnd('/') + relativeUrl;
                context.Response.Redirect(absoluteUrl);
            }
            else
            {
                context.Response.Redirect(defaultUrl);
            }

        }

        public override bool IsReusable
        {
            get { return true; }
        }
    }
}