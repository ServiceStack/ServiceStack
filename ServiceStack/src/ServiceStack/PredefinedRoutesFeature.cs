using System;
using System.Collections.Generic;
using System.Linq;
using ServiceStack.Host.Handlers;
using ServiceStack.NativeTypes.CSharp;
using ServiceStack.Web;
#if NETFX
using System.Web;
#else
using ServiceStack.Host;
#endif

namespace ServiceStack;

public class PredefinedRoutesFeature : IPlugin, Model.IHasStringId
{
    public string Id { get; set; } = Plugins.PredefinedRoutes;
    public Dictionary<string, Func<IHttpHandler>> HandlerMappings { get; } = new();

    public string JsonApiRoute { get; set; } = "/api/{Request}";

    public bool DisableApiRoute
    {
        get => JsonApiRoute == null;
        set => JsonApiRoute = value ? null : JsonApiRoute;
    }

    public Func<IRequest, Dictionary<string, List<ApiDescription>>> ApiIndex { get; set; } = DefaultApiIndex;

    public static Dictionary<string, List<ApiDescription>> DefaultApiIndex(IRequest req)
    {
        var opNames = HostContext.Metadata.GetOperationNamesForMetadata(req.RequestAttributes);
        var to = new Dictionary<string, List<ApiDescription>>();
        var other = "other";
        var defaultTags = new List<string> { other };
        var baseUrl = req.GetBaseUrl();
        var gen = new CSharpGenerator(new MetadataTypesConfig());

        foreach (var opName in opNames)
        {
            var opType = HostContext.Metadata.GetOperationType(opName);
            var op = HostContext.Metadata.GetOperation(opType);
            if (op == null) continue;
                
            var tags = op.Tags?.Count > 0 ? op.Tags : defaultTags;
            foreach (var tag in tags)
            {
                var tagOps = to.GetOrAdd(tag, _ => new List<ApiDescription>());
                var resType = op.ResponseType;

                var apiDesc = new ApiDescription {
                    Name = op.Name,
                    Returns = resType != null 
                        ? gen.Type(resType.Name, resType.IsGenericType ? resType.GetGenericArguments().Select(x => x.Name).ToArray() : Array.Empty<string>()) 
                        : null,
                    Description = op.Description,
                    Notes = op.Notes,
                    Links = new() {
                        ["api"] = baseUrl.CombineWith("/api/" + op.Name),
                        ["ui"] = baseUrl.CombineWith("/ui/" + op.Name),
                    }
                };
                if (Crud.IsCrudQueryDb(op.RequestType))
                {
                    apiDesc.Links["locode"] = baseUrl.CombineWith("/locode/" + op.Name);
                }
                tagOps.Add(apiDesc);
            }
        }

        // Use 'apis' if not using any custom tags
        if (to.Keys.Any(x => x != TagNames.Auth && x != TagNames.Admin && x != other))
        {
            if (to.TryRemove(other, out var apis))
            {
                to["apis"] = apis;
            }
        }
            
        return to;
    }

    public void Register(IAppHost appHost)
    {
        if ((appHost.PathBase == null || !appHost.PathBase.Contains("api")) 
            && JsonApiRoute != null && !appHost.VirtualFileSources.DirectoryExists("api"))
        {
            appHost.RawHttpHandlers.Add(ApiHandlers.Json(JsonApiRoute));
            appHost.AddToAppMetadata(metadata => metadata.HttpHandlers["ApiHandlers.Json"] = JsonApiRoute);
        }
        else
        {
            JsonApiRoute = null;
        }
            
        appHost.CatchAllHandlers.Add(ProcessRequest);
    }

    public IHttpHandler ProcessRequest(string httpMethod, string pathInfo, string filePath)
    {
        var pathParts = pathInfo.TrimStart('/').Split('/');
        if (pathParts.Length == 0) return null;
        return GetHandlerForPathParts(pathParts);
    }

    private IHttpHandler GetHandlerForPathParts(string[] pathParts)
    {
        var pathController = pathParts[0].ToLower();

        if (pathParts.Length == 1)
        {
            if (HandlerMappings.TryGetValue(pathController, out var handlerFn))
                return handlerFn();

            return null;
        }

        var pathAction = pathParts[1].ToLower();
        var requestName = pathParts.Length > 2 ? pathParts[2] : null;
        var isReply = pathAction == "reply";
        var isOneWay = pathAction == "oneway";
        switch (pathController)
        {
            case "json":
                if (isReply)
                    return new JsonReplyHandler { RequestName = requestName };
                if (isOneWay)
                    return new JsonOneWayHandler { RequestName = requestName };
                break;

            case "xml":
                if (isReply)
                    return new XmlReplyHandler { RequestName = requestName };
                if (isOneWay)
                    return new XmlOneWayHandler { RequestName = requestName };
                break;

            case "jsv":
                if (isReply)
                    return new JsvReplyHandler { RequestName = requestName };
                if (isOneWay)
                    return new JsvOneWayHandler { RequestName = requestName };
                break;

            default:
                if (HostContext.ContentTypes.ContentTypeFormats.TryGetValue(pathController, out var contentType))
                {
                    var feature = contentType.ToFeature();
                    if (feature == Feature.None) feature = Feature.CustomFormat;

                    if (isReply)
                        return new GenericHandler(contentType, RequestAttributes.Reply, feature) {
                            RequestName = requestName,
                        };
                    if (isOneWay)
                        return new GenericHandler(contentType, RequestAttributes.OneWay, feature) {
                            RequestName = requestName,
                        };
                }
                break;
        }

        return null;
    }
}