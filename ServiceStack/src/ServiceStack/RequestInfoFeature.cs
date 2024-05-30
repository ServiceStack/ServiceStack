﻿#if !NETFRAMEWORK        
using ServiceStack.Host;
#else
using System.Web;
#endif
using ServiceStack.Host.Handlers;
using ServiceStack.Web;

#if NET8_0_OR_GREATER
using Microsoft.AspNetCore.Builder;
#endif

namespace ServiceStack;

public class RequestInfoFeature : IPlugin, Model.IHasStringId
{
    public string Id { get; set; } = Plugins.RequestInfo;
    public void Register(IAppHost appHost)
    {
        appHost.CatchAllHandlers.Add(GetHandler);

        appHost.ConfigurePlugin<MetadataFeature>(
            feature => feature.AddDebugLink($"?{Keywords.Debug}={Keywords.RequestInfo}", "Request Info"));
        
#if NET8_0_OR_GREATER
        (appHost as IAppHostNetCore).MapEndpoints(routeBuilder =>
        {
            var handler = new RequestInfoHandler();
            routeBuilder.MapGet("/" + Keywords.RequestInfo, httpContext => httpContext.ProcessRequestAsync(handler))
                .WithMetadata<RequestInfoResponse>(nameof(RequestInfoFeature), contentType:MimeTypes.Json);
        });
#endif
    }

    public IHttpHandler GetHandler(IRequest req)
    {
        var pathParts = req.PathInfo.TrimStart('/').Split('/');
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
