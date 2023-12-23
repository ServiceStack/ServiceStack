#if NETCORE        
using ServiceStack.Host;
#else
using System.Web;
#endif
using ServiceStack.Host.Handlers;

#if NET8_0_OR_GREATER
using Microsoft.AspNetCore.Builder;
#endif

namespace ServiceStack;

public class RequestInfoFeature : IPlugin, Model.IHasStringId
{
    public string Id { get; set; } = Plugins.RequestInfo;
    public void Register(IAppHost appHost)
    {
        appHost.CatchAllHandlers.Add(ProcessRequest);

        appHost.ConfigurePlugin<MetadataFeature>(
            feature => feature.AddDebugLink($"?{Keywords.Debug}={Keywords.RequestInfo}", "Request Info"));
        
#if NET8_0_OR_GREATER
        var host = (AppHostBase)appHost;
        host.MapEndpoints(routeBuilder =>
        {
            var handler = new RequestInfoHandler();
            routeBuilder.MapGet("/" + Keywords.RequestInfo, httpContext => httpContext.ProcessRequestAsync(handler));
        });
#endif
    }

    public IHttpHandler ProcessRequest(string httpMethod, string pathInfo, string filePath)
    {
        var pathParts = pathInfo.TrimStart('/').Split('/');
        return pathParts.Length == 0 ? null : GetHandlerForPathParts(pathParts);
    }

    private static IHttpHandler GetHandlerForPathParts(string[] pathParts)
    {
        var pathController = pathParts[0].ToLower();
        return pathController == Keywords.RequestInfo
            ? new RequestInfoHandler()
            : null;
    }
}