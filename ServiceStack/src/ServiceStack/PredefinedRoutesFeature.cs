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

#if NET8_0_OR_GREATER
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
#endif

namespace ServiceStack;

public class PredefinedRoutesFeature : IPlugin, IAfterInitAppHost, Model.IHasStringId
{
    public string Id { get; set; } = Plugins.PredefinedRoutes;
    public Dictionary<string, Func<IHttpHandler>> HandlerMappings { get; } = new();

    public string JsonApiRoute { get; set; } = "/api/{Request}";

    public bool DisableApiRoute
    {
        get => JsonApiRoute == null;
        set => JsonApiRoute = value ? null : JsonApiRoute;
    }

#if NET8_0_OR_GREATER
    /// <summary>
    /// Whether to Exclude pre-defined routes from ASP .NET Core Metadata / Swagger UI / etc
    /// </summary>
    public bool ExcludeFromDescription { get; set; }
#endif    

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
        if ((appHost.PathBase == null || !appHost.PathBase.Contains("api")) && JsonApiRoute != null)
        {
            appHost.RawHttpHandlers.Add(ApiHandlers.Json(JsonApiRoute));
            appHost.AddToAppMetadata(metadata => metadata.HttpHandlers["ApiHandlers.Json"] = JsonApiRoute);
        }
        else
        {
            JsonApiRoute = null;
        }
            
        appHost.CatchAllHandlers.Add(GetHandler);
    }

    public void AfterInit(IAppHost appHost)
    {
        if (JsonApiRoute != null)
        {
#if NET8_0_OR_GREATER
            var host = appHost as IAppHostNetCore; 
            host.MapEndpoints(routeBuilder =>
            {
                var apiPath = ApiHandlers.GetBaseApiPath(JsonApiRoute);
                if (ApiIndex != null)
                {
                    // Map /api index route
                    routeBuilder.MapGet(apiPath, (HttpResponse response, HttpContext httpContext) =>
                        {
                            var req = httpContext.ToRequest();
                            var result = ApiIndex(req);
                            return Results.Json(result);
                        })
                        .WithMetadata<Dictionary<string, List<ApiDescription>>>();
                }

                var options = host.CreateEndpointOptions();
                string[] autoBatchVerbs = [HttpMethods.Post];
                
                // Map /api/{Request} routes
                var apis = routeBuilder.MapGroup(apiPath);
                foreach (var entry in appHost.Metadata.OperationsMap)
                {
                    var requestType = entry.Key;
                    var operation = entry.Value;
                
                    if (!host.EndpointVerbs.TryGetValue(operation.Method, out var verb))
                        continue;

                    var builder = apis.MapMethods("/" + requestType.Name, verb, (HttpResponse response, HttpContext httpContext) => 
                        httpContext.ProcessRequestAsync(ApiHandlers.JsonEndpointHandler(apiPath, httpContext.Request.Path), apiName:requestType.Name));

                    if (ExcludeFromDescription)
                    {
                        builder.ExcludeFromDescription();
                    }
                    
                    host.ConfigureOperationEndpoint(builder, operation, options);
                    
                    foreach (var handler in host.Options.RouteHandlerBuilders)
                    {
                        handler(builder, operation, operation.Method, apiPath + "/" + requestType.Name);
                    }
                    
                    // Register implicit AutoBatch APIs for HTTP POST/PUT/PATCH requests
                    if (verb.Any(HttpUtils.HasRequestBody) && operation.ResponseType != null && operation.ResponseType != typeof(byte[]) && operation.ResponseType != typeof(System.IO.Stream))
                    {
                        var batchBuilder = apis.MapMethods("/" + requestType.Name + "[]", autoBatchVerbs, (HttpResponse response, HttpContext httpContext) => 
                            httpContext.ProcessRequestAsync(ApiHandlers.JsonEndpointHandler(apiPath, httpContext.Request.Path), apiName:requestType.Name + "[]"));
                        var responseType = typeof(List<>).MakeGenericType(operation.ResponseType);
                        host.ConfigureOperationEndpoint(batchBuilder, operation, options, responseType:responseType)
                            .ExcludeFromDescription();
                        foreach (var handler in host.Options.RouteHandlerBuilders)
                        {
                            handler(batchBuilder, operation, operation.Method, apiPath + "/" + requestType.Name + "[]");
                        }
                    }
                }

                // Map /api/{Request}.{Format} routes
                foreach (var entry in appHost.ContentTypes.ContentTypeFormats)
                {
                    var serializer = appHost.ContentTypes.GetStreamSerializerAsync(entry.Value);
                    if (serializer == ContentTypes.UnknownContentTypeSerializer)
                        continue;
            
                    apis.MapGet("{name}." + entry.Key, (string name, HttpContext httpContext) => 
                            httpContext.ProcessRequestAsync(ApiHandlers.JsonEndpointHandler(apiPath, httpContext.Request.Path), apiName:name))
                        .WithMetadata<string>(contentType:entry.Value);
                    
                    apis.MapPost("{name}." + entry.Key, (string name, HttpContext httpContext) => 
                            httpContext.ProcessRequestAsync(ApiHandlers.JsonEndpointHandler(apiPath, httpContext.Request.Path), apiName:name))
                        .WithMetadata<string>(contentType:entry.Value);
                    
                    apis.MapPut("{name}." + entry.Key, (string name, HttpContext httpContext) => 
                            httpContext.ProcessRequestAsync(ApiHandlers.JsonEndpointHandler(apiPath, httpContext.Request.Path), apiName:name))
                        .WithMetadata<string>(contentType:entry.Value);
                    
                    apis.MapPatch("{name}." + entry.Key, (string name, HttpContext httpContext) => 
                            httpContext.ProcessRequestAsync(ApiHandlers.JsonEndpointHandler(apiPath, httpContext.Request.Path), apiName:name))
                        .WithMetadata<string>(contentType:entry.Value);
                    
                    apis.MapDelete("{name}." + entry.Key, (string name, HttpContext httpContext) => 
                            httpContext.ProcessRequestAsync(ApiHandlers.JsonEndpointHandler(apiPath, httpContext.Request.Path), apiName:name))
                        .WithMetadata<string>(contentType:entry.Value);
                }
            });
#endif
        }
    }

    public IHttpHandler GetHandler(IRequest req)
    {
        var pathParts = req.PathInfo.TrimStart('/').Split('/');
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