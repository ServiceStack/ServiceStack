#if NET8_0_OR_GREATER

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using ServiceStack.Host;
using ServiceStack.Host.Handlers;

namespace ServiceStack;

public static class AppHostEndpointsExtensions
{
    private static readonly Dictionary<string, string[]> Verbs = new()
    {
        [HttpMethods.Get] = [HttpMethods.Get],
        [HttpMethods.Post] = [HttpMethods.Post],
        [HttpMethods.Put] = [HttpMethods.Put],
        [HttpMethods.Delete] = [HttpMethods.Delete],
        [HttpMethods.Patch] = [HttpMethods.Patch],
    };
    
    public static void MapEndpoints(this IEndpointRouteBuilder routeBuilder, AppHostBase appHost)
    {
        var feature = appHost.GetPlugin<PredefinedRoutesFeature>();
        var apiPath = feature?.JsonApiRoute != null 
            ? ApiHandlers.GetBaseApiPath(feature.JsonApiRoute)
            : "/api";
        
        if (appHost.Options.UseEndpointRouting)
        {
            appHost.IgnoreRequestHandler = appHost.ShouldUseEndpointRoute;
        }

        if (feature?.ApiIndex != null)
        {
            routeBuilder.MapGet(apiPath, (HttpResponse response, HttpContext httpContext) =>
            {
                var req = httpContext.ToRequest();
                var result = feature.ApiIndex(req);
                return Results.Json(result);
            })
            .ExcludeFromDescription()
            .Produces<Dictionary<string, List<ApiDescription>>>();
        }

        var authAttr = new AuthorizeAttribute { AuthenticationSchemes = appHost.Options.AuthenticationSchemes };

        void ConfigureOperation(RouteHandlerBuilder builder, Operation operation)
        {
            if (operation.ResponseType != null)
            {
                if (operation.ResponseType == typeof(byte[]) || operation.ResponseType == typeof(Stream))
                {
                    builder.Produces(200, responseType:operation.ResponseType, contentType:MimeTypes.Binary);
                }
                else
                {
                    builder.Produces(200, responseType:operation.ResponseType, contentType:MimeTypes.Json);
                }
            }
            else
            {
                builder.Produces(appHost.Config.Return204NoContentForEmptyResponse ? 204 : 200, responseType:null);
            }
            if (operation.RequiresAuthentication)
            {
                builder.RequireAuthorization(authAttr);
            }
            else
            {
                builder.AllowAnonymous();
            }
            
            if (operation.RequestType.ExcludesFeature(Feature.Metadata) || 
                operation.RequestType.ExcludesFeature(Feature.ApiExplorer))
                builder.ExcludeFromDescription();
        }
        
        var apis = routeBuilder.MapGroup(apiPath);
        foreach (var entry in appHost.Metadata.OperationsMap)
        {
            var requestType = entry.Key;
            var operation = entry.Value;
                
            if (!Verbs.TryGetValue(operation.Method, out var verb))
                continue;

            var builder = apis.MapMethods("/" + requestType.Name, verb, (HttpResponse response, HttpContext httpContext) =>
            {
                var req = httpContext.ToRequest();
                var handler = ApiHandlers.JsonEndpointHandler(apiPath, req);
                return handler.ProcessRequestAsync(req, req.Response, requestType.Name);
            });
            foreach (var handler in appHost.Options.RouteHandlerBuilders)
            {
                handler(builder, operation, operation.Method, apiPath + "/" + requestType.Name);
            }
            ConfigureOperation(builder, operation);

            foreach (var route in operation.Routes.Safe())
            {
                var routeVerbs = route.Verbs == null || (route.Verbs.Length == 1 && route.Verbs[0] == ActionContext.AnyAction) 
                    ? [operation.Method]
                    : route.Verbs;

                foreach (var routeVerb in routeVerbs)
                {
                    if (!Verbs.TryGetValue(routeVerb, out verb))
                        continue;

                    var pathBuilder = routeBuilder.MapMethods(route.Path, verb, (HttpResponse response, HttpContext httpContext) =>
                    {
                        var req = httpContext.ToRequest();
                        var restPath = RestHandler.FindMatchingRestPath(req, out var contentType);
                        var handler = restPath != null
                            ? (HttpAsyncTaskHandler)new RestHandler { RestPath = restPath, RequestName = restPath.RequestType.GetOperationName(), ResponseContentType = contentType }
                            : new NotFoundHttpHandler();
                        return handler.ProcessRequestAsync(req, req.Response, requestType.Name);
                    });
                    foreach (var handler in appHost.Options.RouteHandlerBuilders)
                    {
                        handler(pathBuilder, operation, routeVerb, route.Path);
                    }
                    ConfigureOperation(pathBuilder, operation);
                }
            }
        }

        foreach (var entry in appHost.ContentTypes.ContentTypeFormats)
        {
            var serializer = appHost.ContentTypes.GetStreamSerializerAsync(entry.Value);
            if (serializer == ContentTypes.UnknownContentTypeSerializer)
                continue;
            
            apis.MapGet("{name}." + entry.Key, (string name, HttpResponse response, HttpContext httpContext) =>
            {
                var req = httpContext.ToRequest();
                var handler = ApiHandlers.JsonEndpointHandler(apiPath, req);
                return handler.ProcessRequestAsync(req, req.Response, name);
            })
            .ExcludeFromDescription()
            .Produces(200, contentType:entry.Value);
        }

    }
}

#endif
