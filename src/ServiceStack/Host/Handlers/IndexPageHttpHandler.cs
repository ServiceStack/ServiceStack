using System.Net;
using ServiceStack.Web;

namespace ServiceStack.Host.Handlers
{
    public class IndexPageHttpHandler : HttpAsyncTaskHandler
    {
        public IndexPageHttpHandler() => this.RequestName = nameof(IndexPageHttpHandler);

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
            response.EndHttpHandlerRequest(skipHeaders:true);
        }

        public override bool IsReusable => true;
    }
}