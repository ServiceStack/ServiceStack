using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack.Host.Handlers
{
    public class ForbiddenHttpHandler : HttpAsyncTaskHandler
    {
        public ForbiddenHttpHandler()
        {
            this.RequestName = nameof(ForbiddenHttpHandler);
        }

        public string WebHostPhysicalPath { get; set; }
        public string WebHostUrl { get; set; }
        public string DefaultRootFileName { get; set; }
        public string DefaultHandler { get; set; }

        public override Task ProcessRequestAsync(IRequest request, IResponse response, string operationName)
        {
            response.StatusCode = 403;
            response.ContentType = "text/plain";

            return response.EndHttpHandlerRequestAsync(skipClose: true, afterHeaders: r =>
            {
                var sb = StringBuilderCache.Allocate()
                    .Append($@"Forbidden

Request.HttpMethod: {request.Verb}
Request.PathInfo: {request.PathInfo}
Request.QueryString: {request.QueryString}

");
                
                if (HostContext.Config.DebugMode)
                {
                    sb.AppendLine($"Request.RawUrl: {request.RawUrl}");

                    if (!WebHostPhysicalPath.IsNullOrEmpty())
                        sb.AppendLine($"App.WebHostPhysicalPath: {WebHostPhysicalPath}");
                    if (!WebHostUrl.IsNullOrEmpty())
                        sb.AppendLine($"App.WebHostUrl: {WebHostUrl}");
                    if (!DefaultRootFileName.IsNullOrEmpty())
                        sb.AppendLine($"App.DefaultRootFileName: {DefaultRootFileName}");
                    if (!DefaultHandler.IsNullOrEmpty())
                        sb.AppendLine($"App.DefaultHandler: {DefaultHandler}");
                    if (!HttpHandlerFactory.DebugLastHandlerArgs.IsNullOrEmpty())
                        sb.AppendLine($"App.DebugLastHandlerArgs: {HttpHandlerFactory.DebugLastHandlerArgs}");
                }
                
                return response.OutputStream.WriteAsync(StringBuilderCache.ReturnAndFree(sb));
            });
        }

        public override bool IsReusable => true;

        public override bool RunAsAsync() => true;
    }
}