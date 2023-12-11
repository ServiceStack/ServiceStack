using System;
using ServiceStack.Web;
using ServiceStack.Host.Handlers;
using ServiceStack.Text;

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
        string contentType, RequestAttributes requestAttributes, Feature features)
    {
        if (string.IsNullOrEmpty(apiPath))
            throw new ArgumentNullException(nameof(apiPath));
        if (apiPath[0] != '/')
            throw new ArgumentException(apiPath + " must start with '/'");
        if (!apiPath.EndsWith("/{Request}"))
            throw new ArgumentException(apiPath + " must end with '/{Request}'");
        var baseApiPath = apiPath.LastLeftPart('/'); 
        var useApiPath = apiPath.LastLeftPart('/') + '/';
        
        return req => {
            // Don't handle OPTIONS CORS requests
            if (req.HttpMethod == HttpMethods.Options)
            {
                var emitHandler = HostContext.GetPlugin<CorsFeature>()?.EmitGlobalHeadersHandler;
                if (emitHandler != null)
                    return emitHandler;
                return null;
            }
            
            var pathInfo = req.PathInfo;
            if (pathInfo == baseApiPath || pathInfo.StartsWith(useApiPath))
            {
                // Add support for overriding content type with ext, e.g. .csv
                var apiName = pathInfo == baseApiPath ? "" : pathInfo.LastRightPart('/');
                if (string.IsNullOrEmpty(apiName))
                {
                    var feature = HostContext.GetPlugin<PredefinedRoutesFeature>();
                    if (feature?.ApiIndex != null)
                    {
                        var ret = feature.ApiIndex(req);
                        return new CustomActionHandlerAsync(async (req, res) =>
                        {
                            res.ContentType = contentType;
                            var serializer = HostContext.ContentTypes.GetStreamSerializerAsync(contentType);
                            await serializer(req, ret, req.Response.OutputStream).ConfigAwait();
                        });
                    }
                }
                
                var useContentType = contentType;
                var useRequestAttrs = requestAttributes;
                var useFeature = features;
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
