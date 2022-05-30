#if NETCORE        
using ServiceStack.Host;
#else
using System.Web;
#endif

using System;
using System.Collections.Generic;
using System.Linq;
using ServiceStack.Host;
using ServiceStack.Host.Handlers;
using ServiceStack.Metadata;
using ServiceStack.NativeTypes;
using ServiceStack.Web;

namespace ServiceStack
{
    public class MetadataFeature : IPlugin, Model.IHasStringId, IPreInitPlugin
    {
        public string Id { get; set; } = Plugins.Metadata;
        public string PluginLinksTitle { get; set; }
        public Dictionary<string, string> PluginLinks { get; set; }

        public string DebugLinksTitle { get; set; }
        public Dictionary<string, string> DebugLinks { get; set; }

        public Action<IndexOperationsControl> IndexPageFilter { get; set; }
        public Action<OperationControl> DetailPageFilter { get; set; }
        
        public List<Action<AppMetadata>> AppMetadataFilters { get; } = new();
        public List<Action<AppMetadata>> AfterAppMetadataFilters { get; } = new();

        public bool ShowResponseStatusInMetadataPages { get; set; }
        
        /// <summary>
        /// Export built-in Types so they're available from /metadata/app
        /// </summary>
        public List<Type> ExportTypes { get; } = new() {
            typeof(AuditBase),
        };
        
        public Dictionary<Type, string[]> ServiceRoutes { get; set; } = new() {
            { typeof(MetadataAppService), new[]
            {
                "/" + "metadata".Localize() + "/" + "app".Localize(),
            } },
            { typeof(MetadataNavService), new[] {
                "/" + "metadata".Localize() + "/" + "nav".Localize(),
                "/" + "metadata".Localize() + "/" + "nav".Localize() + "/{Name}",
            } },
        };

        public HtmlModule HtmlModule { get; set; } = new("/modules/ui", "/ui");

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

        public MetadataFeature()
        {
            PluginLinksTitle = "Plugin Links:";
            PluginLinks = new Dictionary<string, string>();

            DebugLinksTitle = "Debug Info:";
            DebugLinks = new Dictionary<string, string> {
                {"operations/metadata", "Operations Metadata"},
            };
        }

        public void BeforePluginsLoaded(IAppHost appHost)
        {
            appHost.ConfigurePlugin<UiFeature>(feature => 
                feature.HtmlModules.Add(HtmlModule));
        }

        public void Register(IAppHost appHost)
        {
            appHost.CatchAllHandlers.Add(ProcessRequest);

            if (EnableNav)
            {
                ViewUtils.Load(appHost.AppSettings);
            }

            appHost.RegisterServices(ServiceRoutes);
        }

        public virtual IHttpHandler ProcessRequest(string httpMethod, string pathInfo, string filePath)
        {
            var pathParts = pathInfo.TrimStart('/').Split('/');
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
#if !NETCORE
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
#if !NETCORE
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
    public class MetadataAppService : Service
    {
        public INativeTypesMetadata NativeTypesMetadata { get; set; }
        public AppMetadata Any(MetadataApp request) => NativeTypesMetadata.ToAppMetadata(Request);
    }

    public static class MetadataFeatureExtensions
    {
        public static AppMetadata ToAppMetadata(this INativeTypesMetadata nativeTypesMetadata, IRequest req)
        {
            var feature = HostContext.AssertPlugin<MetadataFeature>();
            var typesConfig = nativeTypesMetadata.GetConfig(new TypesMetadata());
            feature.ExportTypes.Each(x => typesConfig.ExportTypes.Add(x));
            var metadataTypes = NativeTypesService.ResolveMetadataTypes(typesConfig, nativeTypesMetadata, req);
            metadataTypes.Config = null;
            var uiFeature = HostContext.GetPlugin<UiFeature>();

            var appHost = HostContext.AssertAppHost();
            var config = appHost.Config;
            var response = new AppMetadata
            {
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

            if (response.App.BaseUrl == null)
                response.App.BaseUrl = req.GetBaseUrl();
            if (response.App.ServiceName == null)
                response.App.ServiceName = appHost.ServiceName;
            if (response.App.JsTextCase == null)
                response.App.JsTextCase = $"{Text.JsConfig.TextCase}";

            var view = req.Dto is MetadataApp dto ? dto.View?.ToLower() : null;
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
                fn(response);
            }

            return response;
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
}