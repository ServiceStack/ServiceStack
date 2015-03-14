using System;
using System.Text.RegularExpressions;
using ServiceStack.Host.Handlers;
using ServiceStack.IO;

namespace ServiceStack.Api.Swagger
{
    public class SwaggerFeature : IPlugin, IPreInitPlugin
    {
        /// <summary>
        /// Gets or sets <see cref="Regex"/> pattern to filter available resources. 
        /// </summary>
        public string ResourceFilterPattern { get; set; }

        public bool UseCamelCaseModelPropertyNames { get; set; }

        public bool UseLowercaseUnderscoreModelPropertyNames { get; set; }

        public bool DisableAutoDtoInBodyParam { get; set; }

        public bool UseBootstrapTheme { get; set; }

        public string LogoUrl { get; set; }

        public Action<SwaggerModel> ModelFilter { get; set; }

        public Action<ModelProperty> ModelPropertyFilter { get; set; }

        public SwaggerFeature()
        {
            LogoUrl = "//raw.githubusercontent.com/ServiceStack/Assets/master/img/artwork/logo-24.png";
        }

        public void Configure(IAppHost appHost)
        {
            appHost.Config.EmbeddedResourceSources.Add(typeof(SwaggerFeature).Assembly);
        }

        public void Register(IAppHost appHost)
        {
            if (ResourceFilterPattern != null)
                SwaggerResourcesService.resourceFilterRegex = new Regex(ResourceFilterPattern, RegexOptions.Compiled);

            SwaggerApiService.UseCamelCaseModelPropertyNames = UseCamelCaseModelPropertyNames;
            SwaggerApiService.UseLowercaseUnderscoreModelPropertyNames = UseLowercaseUnderscoreModelPropertyNames;
            SwaggerApiService.DisableAutoDtoInBodyParam = DisableAutoDtoInBodyParam;
            SwaggerApiService.ModelFilter = ModelFilter;
            SwaggerApiService.ModelPropertyFilter = ModelPropertyFilter;

            appHost.RegisterService(typeof(SwaggerResourcesService), new[] { "/resources" });
            appHost.RegisterService(typeof(SwaggerApiService), new[] { SwaggerResourcesService.RESOURCE_PATH + "/{Name*}" });

            var swaggerUrl = UseBootstrapTheme
                ? "swagger-ui-bootstrap/"
                : "swagger-ui/";

            appHost.GetPlugin<MetadataFeature>()
                .AddPluginLink(swaggerUrl, "Swagger UI");

            appHost.CatchAllHandlers.Add((httpMethod, pathInfo, filePath) =>
            {
                IVirtualFile indexFile;
                switch (pathInfo)
                {
                    case "/swagger-ui":
                    case "/swagger-ui/":
                    case "/swagger-ui/default.html":
                        indexFile = appHost.VirtualPathProvider.GetFile("/swagger-ui/index.html");
                        break;
                    case "/swagger-ui-bootstrap":
                    case "/swagger-ui-bootstrap/":
                    case "/swagger-ui-bootstrap/index.html":
                        indexFile = appHost.VirtualPathProvider.GetFile("/swagger-ui-bootstrap/index.html");
                        break;
                    default:
                        indexFile = null;
                        break;
                }
                if (indexFile != null)
                {
                    var html = indexFile.ReadAllText();

                    return new CustomResponseHandler((req, res) =>
                    {
                        res.ContentType = MimeTypes.Html;
                        var resourcesUrl = req.ResolveAbsoluteUrl("~/resources");
                        html = html.Replace("http://petstore.swagger.io/v2/swagger.json", resourcesUrl)
                            .Replace("ApiDocs", HostContext.ServiceName)
                            .Replace("{LogoUrl}", LogoUrl);
                        return html;
                    });
                }
                return pathInfo.StartsWith("/swagger-ui") ? new StaticFileHandler() : null;
            });
        }

        public static bool IsEnabled
        {
            get { return HostContext.HasPlugin<SwaggerFeature>(); }
        }
    }
}