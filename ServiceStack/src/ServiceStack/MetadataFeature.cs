﻿#if NETFRAMEWORK        
using System.Web;
#endif

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using ServiceStack.Host;
using ServiceStack.Host.Handlers;
using ServiceStack.HtmlModules;
using ServiceStack.Metadata;
using ServiceStack.NativeTypes;
using ServiceStack.Web;

#if NET8_0_OR_GREATER
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
#endif

namespace ServiceStack;

public class MetadataFeature : IPlugin, IConfigureServices, Model.IHasStringId, IPreInitPlugin
{
    public string Id { get; set; } = Plugins.Metadata;
    public string PluginLinksTitle { get; set; } = "Plugin Links:";
    public string PluginLinksStyle { get; set; } = "font-size:16px;";
    public Dictionary<string, string> PluginLinks { get; set; } = new();

    public string DebugLinksTitle { get; set; } = "Debug Info:";
    public string DebugLinksStyle { get; set; } = "";
    public Dictionary<string, string> DebugLinks { get; set; } = new()
    {
        ["operations/metadata"] = "Operations Metadata",
    };

    public Action<IndexOperationsControl> IndexPageFilter { get; set; }
    public Action<OperationControl> DetailPageFilter { get; set; }
        
    public List<Action<AppMetadata>> AppMetadataFilters { get; } = [];
    public List<Action<IRequest,AppMetadata>> AfterAppMetadataFilters { get; } = [
        MetadataUtils.LocalizeMetadata
    ];

    public bool ShowResponseStatusInMetadataPages { get; set; }
        
    /// <summary>
    /// Export built-in Types so they're available from /metadata/app
    /// </summary>
    public List<Type> ExportTypes { get; } = [ typeof(AuditBase) ];
        
    public Dictionary<Type, string[]> ServiceRoutes { get; set; } = new() {
        [typeof(MetadataAppService)] = [
            "/" + "metadata".Localize() + "/" + "app".Localize()
        ],
        [typeof(MetadataNavService)] = [
            "/" + "metadata".Localize() + "/" + "nav".Localize(),
            "/" + "metadata".Localize() + "/" + "nav".Localize() + "/{Name}"
        ],
    };

    public HtmlModule HtmlModule { get; set; } = new("/modules/ui", "/ui") {
        DynamicPageQueryStrings = { nameof(MetadataApp.IncludeTypes) }
    };

    public bool EnableNav
    {
        get => ServiceRoutes.ContainsKey(typeof(MetadataNavService));
        set
        {
            if (!value)
                ServiceRoutes.Remove(typeof(MetadataNavService));
        }
    }
        
    public bool EnableAppMetadata
    {
        get => ServiceRoutes.ContainsKey(typeof(MetadataAppService));
        set
        {
            if (!value)
                ServiceRoutes.Remove(typeof(MetadataAppService));
        }
    }

    public Func<string,string> TagFilter { get; set; }

    public void BeforePluginsLoaded(IAppHost appHost)
    {
        if (HtmlModule != null)
        {
            appHost.ConfigurePlugin<UiFeature>(feature =>
            {
                feature.HtmlModules.Add(HtmlModule);
                HtmlModule.OnConfigure.Add((_, module) => {
                    module.LineTransformers = FilesTransformer.HtmlModuleLineTransformers.ToList();
                });
            });
        }
    }

    public void Configure(IServiceCollection services)
    {
        services.RegisterServices(ServiceRoutes);
    }

    public void Register(IAppHost appHost)
    {
        if (EnableNav)
        {
            ViewUtils.Load(appHost.AppSettings);
        }

        appHost.CatchAllHandlers.Add(GetHandler);
        
#if NET8_0_OR_GREATER
        (appHost as IAppHostNetCore).MapEndpoints(routeBuilder =>
        {
            var tag = GetType().Name;
            routeBuilder.MapGet("/metadata", httpContext => httpContext.ProcessRequestAsync(new IndexMetadataHandler()))
                .WithMetadata<string>(name:nameof(IndexMetadataHandler), tag:tag, contentType:MimeTypes.Html);
            
            routeBuilder.MapGet("/json/metadata", httpContext => httpContext.ProcessRequestAsync(new JsonMetadataHandler()))
                .WithMetadata<string>(name:nameof(JsonMetadataHandler), tag:tag, contentType:MimeTypes.Html);
            
            routeBuilder.MapGet("/xml/metadata", httpContext => httpContext.ProcessRequestAsync(new XmlMetadataHandler()))
                .WithMetadata<string>(name:nameof(XmlMetadataHandler), tag:tag, contentType:MimeTypes.Html);
            
            routeBuilder.MapGet("/jsv/metadata", httpContext => httpContext.ProcessRequestAsync(new JsvMetadataHandler()))
                .WithMetadata<string>(name:nameof(JsvMetadataHandler), tag:tag, contentType:MimeTypes.Html);

            var operationsHandler = new CustomResponseHandler((httpReq, httpRes) =>
                HostContext.AppHost.HasAccessToMetadata(httpReq, httpRes)
                    ? HostContext.Metadata.GetOperationDtos()
                    : null, "Operations Metadata");
            
            routeBuilder.MapGet("/operations/metadata", httpContext => httpContext.ProcessRequestAsync(operationsHandler))
                .WithMetadata<List<OperationDto>>(name:"Operations Metadata", tag:tag, contentType:MimeTypes.Json);
            routeBuilder.MapGet("/operations/metadata.{format}", (string format, HttpContext httpContext) => {
                    return httpContext.ProcessRequestAsync(operationsHandler, 
                        configure:req => req.ResponseContentType = HostContext.ContentTypes.GetFormatContentType(format));
                })
                .WithMetadata<string>(name:"Operations Metadata format", tag:tag);
            
            routeBuilder.MapGet("/{format}/metadata", (string format, HttpContext httpContext) =>
                {
                    if (format.IndexOf(' ') >= 0)
                        format = format.Replace(' ', '+'); //Convert 'x-custom csv' -> 'x-custom+csv'
                    if (appHost.ContentTypes.ContentTypeFormats.TryGetValue(format, out var contentType))
                    {
                        format = ContentFormat.GetContentFormat(contentType);
                        return httpContext.ProcessRequestAsync(new CustomMetadataHandler(contentType, format));
                    }
                    return Task.CompletedTask;
                })
                .WithMetadata<string>(name:nameof(CustomMetadataHandler), tag:tag, contentType:MimeTypes.Html);
        });
#endif
        
    }

    public virtual IHttpHandler GetHandler(IRequest req)
    {
        var pathParts = req.PathInfo.TrimStart('/').Split('/');
        if (pathParts.Length == 0) return null;
        return GetHandlerForPathParts(pathParts);
    }

    private IHttpHandler GetHandlerForPathParts(string[] pathParts)
    {
        var pathController = pathParts[0].ToLowerInvariant();
        if (pathParts.Length == 1)
        {
            if (pathController == "metadata")
                return new IndexMetadataHandler();

            return null;
        }

        var pathAction = pathParts[1].ToLowerInvariant();
#if NETFRAMEWORK
            if (pathAction == "wsdl")
            {
                if (pathController == "soap11")
                    return new Soap11WsdlMetadataHandler();
                if (pathController == "soap12")
                    return new Soap12WsdlMetadataHandler();
            }
#endif

        if (pathAction != "metadata") return null;

        switch (pathController)
        {
            case "json":
                return new JsonMetadataHandler();

            case "xml":
                return new XmlMetadataHandler();

            case "jsv":
                return new JsvMetadataHandler();
#if NETFRAMEWORK
                case "soap11":
                    return new Soap11MetadataHandler();

                case "soap12":
                    return new Soap12MetadataHandler();
#endif

            case "operations":
                return new CustomResponseHandler((httpReq, httpRes) =>
                    HostContext.AppHost.HasAccessToMetadata(httpReq, httpRes)
                        ? HostContext.Metadata.GetOperationDtos()
                        : null, "Operations");

            default:
                if (pathController.IndexOf(' ') >= 0)
                    pathController = pathController.Replace(' ', '+'); //Convert 'x-custom csv' -> 'x-custom+csv'
                if (HostContext.ContentTypes
                    .ContentTypeFormats.TryGetValue(pathController, out var contentType))
                {
                    var format = ContentFormat.GetContentFormat(contentType);
                    return new CustomMetadataHandler(contentType, format);
                }
                break;
        }
        return null;
    }
}

[DefaultRequest(typeof(GetNavItems))]
[Restrict(VisibilityTo = RequestAttributes.None)]
public class MetadataNavService : Service
{
    public object Get(GetNavItems request)
    {
        return request.Name != null
            ? new GetNavItemsResponse {
                BaseUrl = Request.GetBaseUrl(),
                Results = ViewUtils.NavItemsMap.TryGetValue(request.Name, out var navItems)
                    ? navItems
                    : TypeConstants<NavItem>.EmptyList,
            }
            : new GetNavItemsResponse {
                BaseUrl = Request.GetBaseUrl(),
                Results = ViewUtils.NavItems,
                NavItemsMap = ViewUtils.NavItemsMap,
            };
    }
}

[DefaultRequest(typeof(MetadataApp))]
[Restrict(VisibilityTo = RequestAttributes.None)]
public class MetadataAppService(INativeTypesMetadata nativeTypesMetadata) : Service
{
    public AppMetadata Any(MetadataApp request) => nativeTypesMetadata.ToAppMetadata(Request);
}

public static class MetadataUtils
{
    public static AppMetadata ToAppMetadata(this INativeTypesMetadata nativeTypesMetadata, IRequest req)
    {
        var feature = HostContext.AssertPlugin<MetadataFeature>();
        var view = req.Dto is MetadataApp dto ? dto.View?.ToLower() : null;

        var pluginsOnly = view?.StartsWith("plugins") == true; 
        var metadataTypes = new MetadataTypes
        {
            Config = new(),
            Namespaces = [],
            Operations = [],
            Types = [],
        };
            
        if (!pluginsOnly)
        {
            var typesConfig = nativeTypesMetadata.GetConfig(new TypesMetadata());
            var includeTypes = req.QueryString["IncludeTypes"];
            if (includeTypes != null)
                typesConfig.IncludeTypes = includeTypes.FromJsv<List<string>>();
            
            feature.ExportTypes.Each(x => typesConfig.ExportTypes.Add(x));
            metadataTypes = NativeTypesService.ResolveMetadataTypes(typesConfig, nativeTypesMetadata, req);
            
            if (includeTypes != null)
                metadataTypes.RemoveIgnoredTypes(typesConfig);

            metadataTypes.Config = null;
        }
            
        var uiFeature = HostContext.GetPlugin<UiFeature>();

        var appHost = HostContext.AssertAppHost();
        var config = appHost.Config;
        var response = new AppMetadata
        {
            Date = DateTime.UtcNow,
            App = config.AppInfo ?? new AppInfo(),
            Ui = uiFeature?.Info,
            Config = new ConfigInfo {
                DebugMode = config.DebugMode,
            },
            ContentTypeFormats = appHost.ContentTypes.ContentTypeFormats,
            HttpHandlers = new Dictionary<string, string>(),
            Plugins = new PluginInfo
            {
                Loaded = appHost.GetMetadataPluginIds(),
            },
            CustomPlugins = new Dictionary<string, CustomPluginInfo>(),
            Api = metadataTypes,
        };

        response.App.BaseUrl ??= req.GetBaseUrl();
        response.App.ServiceName ??= appHost.ServiceName;
        response.App.ApiVersion ??= config.ApiVersion;
        response.App.JsTextCase ??= $"{Text.JsConfig.TextCase}";
        
#if NET8_0_OR_GREATER        
        if (appHost is AppHostBase host)
        {
            response.App.UseSystemJson = $"{host.Options.UseSystemJson}";
            var endpointRouting = new List<string>();
            if (host.Options.MapEndpointRouting)
                endpointRouting.Add("map");
            if (host.Options.UseEndpointRouting)
                endpointRouting.Add("use");
            if (host.Options.DisableServiceStackRouting)
                endpointRouting.Add("force");
            if (endpointRouting.Count > 0)
                response.App.EndpointRouting = endpointRouting;
        }
#endif

        if (uiFeature?.PreserveAttributesNamed != null && view is "locode" or "explorer" or "admin-ui")
        {
            var preserveAttrs = uiFeature.PreserveAttributesNamed.Map(x => x.LastLeftPart("Attribute"));
            var allTypes = response.Api.GetAllTypes().ToList(); 
            allTypes.ForEach(type => {
                if (type.Attributes?.Count > 0)
                {
                    type.Attributes = type.Attributes.Where(x => preserveAttrs.Contains(x.Name)).ToList().NullIfEmpty();;
                }
                foreach (var prop in type.Properties.Safe())
                {
                    if (prop.Attributes?.Count > 0)
                        prop.Attributes = prop.Attributes.Where(x => preserveAttrs.Contains(x.Name)).ToList().NullIfEmpty();
                    if (prop.Namespace == nameof(System))
                        prop.Namespace = null;
                    prop.DataMember = null;
                }
            });
        }

        foreach (var fn in feature.AppMetadataFilters)
        {
            fn(response);
        }

        if (feature.TagFilter != null)
        {
            foreach (var op in response.Api.Operations)
            {
                if (op.Tags != null && feature.TagFilter != null)
                    op.Tags = op.Tags.Map(feature.TagFilter);
            }
        }

        foreach (var fn in feature.AfterAppMetadataFilters)
        {
            fn(req,response);
        }

        if (pluginsOnly)
        {
            response.App = null;
            response.Ui = null;
            response.Config = null;
            response.ContentTypeFormats = null;
            response.HttpHandlers = null;

            var plugins = view.IndexOf(':') >= 0 ? view.RightPart(':').Split(',').ToSet(StringComparer.OrdinalIgnoreCase) : null;
            if (plugins != null)
            {
                var obj = response.Plugins.ToObjectDictionary();
                var to = new Dictionary<string, object>();
                foreach (var entry in obj)
                {
                    if (!plugins.Contains(entry.Key))
                        continue;
                    to[entry.Key] = entry.Value;
                }
                response.Plugins = to.FromObjectDictionary<PluginInfo>();
            }
        }

        return response;
    }

    public static void LocalizeMetadata(IRequest req, AppMetadata response)
    {
        var appHost = HostContext.AssertAppHost();
        string localize(string text) => text != null
            ? appHost.ResolveLocalizedString(text, req)
            : null;

        void localizeInput(InputInfo input)
        {
            if (input == null)
                return;
            if (input.Label != null)
                input.Label = appHost.ResolveLocalizedString(input.Label, req);
            if (input.Help != null)
                input.Help = appHost.ResolveLocalizedString(input.Help, req);
            if (input.Placeholder != null)
                input.Placeholder = appHost.ResolveLocalizedString(input.Placeholder, req);
            if (input.Title != null)
                input.Title = appHost.ResolveLocalizedString(input.Placeholder, req);
        }

        response.App.ServiceName = localize(response.App.ServiceName);
        response.App.ServiceDescription = localize(response.App.ServiceDescription);
        var ui = response.Ui;
        if (ui != null)
        {
            ui.AdminLinks.Each(x => x.Label = localize(x.Label));
        }

        var plugins = response.Plugins;
        if (plugins != null)
        {
            plugins.Auth?.AuthProviders.Each(x =>
            {
                x.Label = localize(x.Label);
                x.FormLayout.Each(localizeInput);
                if (x.NavItem != null)
                    x.NavItem.Label = localize(x.NavItem.Label);
            });
            plugins.Auth?.RoleLinks.Each(entry => entry.Value.Each(x => x.Label = localize(x.Label)));
            plugins.AdminUsers?.UserAuth?.Properties.Each(x => localizeInput(x.Input));
            plugins.AdminUsers?.FormLayout.Each(localizeInput);
        }

        if (response.Api != null)
        {
            var types = response.Api.Types;
            var requests = response.Api.Operations.Select(x => x.Request);
            var responses = response.Api.Operations.Where(x => x.Response != null).Select(x => x.Response);
            var allTypes = new[] { types, requests, responses }.SelectMany(x => x);
            foreach (var type in allTypes)
            {
                type.Description = localize(type.Description);
                type.Notes = localize(type.Notes);
                type.Properties.Each(x => localizeInput(x.Input));
            }
        }
    }

    public static MetadataFeature AddPluginLink(this MetadataFeature metadata, string href, string title)
    {
        if (metadata != null)
        {
            metadata.PluginLinks[href] = title;
        }
        return metadata;
    }

    public static MetadataFeature RemovePluginLink(this MetadataFeature metadata, string href)
    {
        metadata.PluginLinks.Remove(href);
        return metadata;
    }

    public static MetadataFeature AddDebugLink(this MetadataFeature metadata, string href, string title)
    {
        if (metadata != null)
        {
            metadata.DebugLinks[href] = title;
        }
        return metadata;
    }

    public static MetadataFeature RemoveDebugLink(this MetadataFeature metadata, string href)
    {
        metadata.DebugLinks.Remove(href);
        return metadata;
    }
}