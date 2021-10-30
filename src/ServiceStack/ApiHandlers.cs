using System;
using ServiceStack.Host;

namespace ServiceStack;

/// <summary>
/// Add a new API Handler at a custom route, e.g
/// appHost.ApiHandler(ApiHandlers.Json("/api")) => delegates /api/* requests to JSON Request Handler
///
/// Where the last path segment is treated as containing the API Name (Request DTO) to call, e.g:
///  - /api/Hello      => Hello
///  - /api/path/Hello => Hello 
/// </summary>
public static class ApiHandlers
{
    public static HttpHandlerResolverDelegate Json(string pathPrefix) => 
        Generic(pathPrefix, MimeTypes.Json, RequestAttributes.Reply | RequestAttributes.Json, Feature.Json);
    public static HttpHandlerResolverDelegate Jsv(string pathPrefix) => 
        Generic(pathPrefix, MimeTypes.Jsv, RequestAttributes.Reply | RequestAttributes.Jsv, Feature.Jsv);
    public static HttpHandlerResolverDelegate Csv(string pathPrefix) => 
        Generic(pathPrefix, MimeTypes.Csv, RequestAttributes.Reply | RequestAttributes.Csv, Feature.Csv);
    public static HttpHandlerResolverDelegate Xml(string pathPrefix) => 
        Generic(pathPrefix, MimeTypes.Xml, RequestAttributes.Reply | RequestAttributes.Xml, Feature.Xml);
    
    public static HttpHandlerResolverDelegate Generic(string pathPrefix, 
        string contentType, RequestAttributes requestAttributes, Feature feature)
    {
        if (string.IsNullOrEmpty(pathPrefix))
            throw new ArgumentNullException(nameof(pathPrefix));
        if (pathPrefix[0] != '/')
            throw new ArgumentException(pathPrefix + " must start with '/'");
        
        return (string httpMethod, string pathInfo, string filePath) => {
            if (pathInfo.StartsWith(pathPrefix) && pathInfo.Substring(1).IndexOf('/') >= 0)
            {
                var apiName = pathInfo.LastRightPart('/');
                return new Host.Handlers.GenericHandler(contentType, requestAttributes, feature) {
                    RequestName = apiName
                };
            }
            return null;
        };
    }
}

public static class ApiHandlersExtensions
{
    /// <summary>
    /// Registers an API Handler at a custom path, e.g:
    /// ApiHandler(ApiHandlers.Json("/api")) => delegates /api/* requests to JSON Request Handler
    /// </summary>
    public static IAppHost ApiHandler(this IAppHost appHost, HttpHandlerResolverDelegate apiHandler)
    {
        appHost.CatchAllHandlers.Add(apiHandler);
        return appHost;
    }
}