using System;
using ServiceStack.Web;
using ServiceStack.Host.Handlers;

namespace ServiceStack;

/// <summary>
/// Add a new API Handler at a custom route.
/// 
/// RawHttpHandlers.Add(ApiHandlers.Json("/api/{Request}")) => delegates /api/* requests to JSON Request Handler, e.g:
///  - /api/Hello            => {"result":"Hello"}
///  - /api/Hello?name=World => {"result":"Hello, World"}
/// </summary>
public static class ApiHandlers
{
    public static Func<IHttpRequest, HttpAsyncTaskHandler> Json(string apiPath) => 
        Generic(apiPath, MimeTypes.Json, RequestAttributes.Reply | RequestAttributes.Json, Feature.Json);
    public static Func<IHttpRequest, HttpAsyncTaskHandler> Jsv(string apiPath) => 
        Generic(apiPath, MimeTypes.Jsv, RequestAttributes.Reply | RequestAttributes.Jsv, Feature.Jsv);
    public static Func<IHttpRequest, HttpAsyncTaskHandler> Csv(string apiPath) => 
        Generic(apiPath, MimeTypes.Csv, RequestAttributes.Reply | RequestAttributes.Csv, Feature.Csv);
    public static Func<IHttpRequest, HttpAsyncTaskHandler> Xml(string apiPath) => 
        Generic(apiPath, MimeTypes.Xml, RequestAttributes.Reply | RequestAttributes.Xml, Feature.Xml);
    
    public static Func<IHttpRequest, HttpAsyncTaskHandler> Generic(string apiPath, 
        string contentType, RequestAttributes requestAttributes, Feature feature)
    {
        if (string.IsNullOrEmpty(apiPath))
            throw new ArgumentNullException(nameof(apiPath));
        if (apiPath[0] != '/')
            throw new ArgumentException(apiPath + " must start with '/'");
        if (!apiPath.EndsWith("/{Request}"))
            throw new ArgumentException(apiPath + " must end with '/{Request}'");
        var useApiPath = apiPath.LastLeftPart('/') + '/';
        
        return req => {
            // Don't handle OPTIONS CORS requests
            if (req.HttpMethod == HttpMethods.Options) return null;
            
            var pathInfo = req.PathInfo;
            if (pathInfo.StartsWith(useApiPath))
            {
                // Add support for overriding content type with ext, e.g. .csv
                var apiName = pathInfo.LastRightPart('/');
                var useContentType = contentType;
                var useRequestAttrs = requestAttributes;
                var useFeature = feature;
                if (apiName.IndexOf('.') >= 0)
                {
                    var ext = apiName.RightPart('.');
                    apiName = apiName.LeftPart('.');
                    useContentType = MimeTypes.GetMimeType(ext);
                    useRequestAttrs = RequestAttributes.Reply | ContentFormat.GetEndpointAttributes(useContentType);
                    useFeature = useContentType.ToFeature();
                }

                return new GenericHandler(useContentType, useRequestAttrs, useFeature) {
                    RequestName = apiName
                };
            }
            return null;
        };
    }
}
