using System.Threading.Tasks;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack.Host.Handlers;

public class NotFoundHttpHandler : HttpAsyncTaskHandler
{
    public NotFoundHttpHandler() => this.RequestName = nameof(NotFoundHttpHandler);

    public string WebHostPhysicalPath { get; set; }
    public string WebHostUrl { get; set; }
    public string DefaultRootFileName { get; set; }
    public string DefaultHandler { get; set; }

    public override Task ProcessRequestAsync(IRequest request, IResponse response, string operationName)
    {
        HostContext.AppHost.OnLogError(typeof(NotFoundHttpHandler),
            $"{request.UserHostAddress} Request not found: {request.RawUrl}");

        var sb = StringBuilderCache.Allocate();

        var responseStatus = response.Dto.GetResponseStatus();
        if (responseStatus != null)
        {
            sb.AppendLine(
                responseStatus.ErrorCode != responseStatus.Message
                    ? $"Error ({responseStatus.ErrorCode}): {responseStatus.Message}\n"
                    : $"Error: {responseStatus.Message ?? responseStatus.ErrorCode}\n");
        }

        if (HostContext.DebugMode)
        {
            sb.AppendLine("Handler for Request not found (404):\n")
                .AppendLine("  Request.HttpMethod: " + request.Verb)
                .AppendLine("  Request.PathInfo: " + request.PathInfo)
                .AppendLine("  Request.QueryString: " + request.QueryString)
                .AppendLine("  Request.RawUrl: " + request.RawUrl);
        }
        else
        {
            sb.Append("404");
        }

        response.ContentType = "text/plain";
        response.StatusCode = 404;

        if (responseStatus != null)
            response.StatusDescription = responseStatus.ErrorCode;

        var text = StringBuilderCache.ReturnAndFree(sb);
        return response.EndHttpHandlerRequestAsync(skipClose: true, afterHeaders: r => r.WriteAsync(text));
    }

    public override bool IsReusable => true;
}
