using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using ServiceStack.Host.Handlers;
using ServiceStack.IO;

namespace ServiceStack.Api.Swagger
{
    [Obsolete("Consider using newer OpenApiFeature, see: https://docs.servicestack.net/openapi")]
    public class SwaggerFeature : IPlugin, IPreInitPlugin, Model.IHasStringId
    {
        public string Id { get; set; } = Plugins.Swagger;
        /// <summary>
        /// Gets or sets <see cref="Regex"/> pattern to filter available resources. 
        /// </summary>
        public string ResourceFilterPattern { get; set; }

        public bool UseCamelCaseModelPropertyNames { get; set; }

        public bool UseLowercaseUnderscoreModelPropertyNames { get; set; }

        public bool DisableAutoDtoInBodyParam { get; set; }

        public bool UseBootstrapTheme { get; set; }

        public string LogoUrl { get; set; }

        public string LogoHref { get; set; }

        public Action<SwaggerResourcesResponse> ResourcesResponseFilter { get; set; }

        public Action<SwaggerApiDeclaration> ApiDeclarationFilter { get; set; }

        public Action<SwaggerOperation> OperationFilter { get; set; }

        public Action<SwaggerModel> ModelFilter { get; set; }

        public Action<SwaggerProperty> ModelPropertyFilter { get; set; }

        public Dictionary<string, string> RouteSummary { get; set; }

        public List<string> AnyRouteVerbs { get; set; }

        public SwaggerFeature()
        {
            RouteSummary = new Dictionary<string, string>();
            AnyRouteVerbs = new List<string> { HttpMethods.Get, HttpMethods.Post, HttpMethods.Put, HttpMethods.Delete };
        }

        public void BeforePluginsLoaded(IAppHost appHost)
        {
            appHost.Config.EmbeddedResourceSources.Add(typeof(SwaggerFeature).Assembly);
        }

        public void Register(IAppHost appHost)
        {
            if (ResourceFilterPattern != null)
                SwaggerResourcesService.resourceFilterRegex = new Regex(ResourceFilterPattern, RegexOptions.Compiled);

            SwaggerResourcesService.ResourcesResponseFilter = ResourcesResponseFilter;

            SwaggerApiService.UseCamelCaseModelPropertyNames = UseCamelCaseModelPropertyNames;
            SwaggerApiService.UseLowercaseUnderscoreModelPropertyNames = UseLowercaseUnderscoreModelPropertyNames;
            SwaggerApiService.DisableAutoDtoInBodyParam = DisableAutoDtoInBodyParam;
            SwaggerApiService.ApiDeclarationFilter = ApiDeclarationFilter;
            SwaggerApiService.OperationFilter = OperationFilter;
            SwaggerApiService.ModelFilter = ModelFilter;
            SwaggerApiService.ModelPropertyFilter = ModelPropertyFilter;
            SwaggerApiService.AnyRouteVerbs = AnyRouteVerbs.ToArray();

            appHost.RegisterService(typeof(SwaggerResourcesService), "/resources");
            appHost.RegisterService(typeof(SwaggerApiService), SwaggerResourcesService.RESOURCE_PATH + "/{Name*}");

            var swaggerUrl = UseBootstrapTheme
                ? "swagger-ui-bootstrap/"
                : "swagger-ui/";

            appHost.GetPlugin<MetadataFeature>()
                .AddPluginLink(swaggerUrl, "Swagger UI");

            appHost.CatchAllHandlers.Add((httpMethod, pathInfo, filePath) =>
            {
                IVirtualFile indexFile;
                IVirtualFile patchFile = null;
                switch (pathInfo)
                {
                    case "/swagger-ui":
                    case "/swagger-ui/":
                    case "/swagger-ui/default.html":
                        indexFile = appHost.VirtualFileSources.GetFile("/swagger-ui/index.html");
                        patchFile = appHost.VirtualFileSources.GetFile("/swagger-ui/patch.js");
                        break;
                    case "/swagger-ui-bootstrap":
                    case "/swagger-ui-bootstrap/":
                    case "/swagger-ui-bootstrap/index.html":
                        indexFile = appHost.VirtualFileSources.GetFile("/swagger-ui-bootstrap/index.html");
                        break;
                    default:
                        indexFile = null;
                        break;
                }
                if (indexFile != null)
                {
                    var html = indexFile.ReadAllText();
                    var injectJs = patchFile?.ReadAllText();

                    return new CustomResponseHandler((req, res) =>
                    {
                        res.ContentType = MimeTypes.HtmlUtf8; //use alt HTML ContentType so it's not overridden when Feature.Html is removed
                        var resourcesUrl = req.ResolveAbsoluteUrl("~/resources");
                        var logoHref = LogoHref ?? "./";
                        html = html.Replace("http://petstore.swagger.io/v2/swagger.json", resourcesUrl)
                            .Replace("ApiDocs", HostContext.ServiceName)
                            .Replace("<a id=\"logo\" href=\"http://swagger.io\">swagger</a>", $"<a id=\"logo\" href=\"{logoHref}\">{HostContext.ServiceName}</a>");

                        if (LogoUrl != null)
                        {
                            html = html.Replace("{LogoUrl}", LogoUrl);
                        }

                        if (injectJs != null)
                        {
                            html = html.Replace("</body>",
                                "<script type='text/javascript'>" + injectJs + "</script></body>");
                        }

                        return html;
                    });
                }
                return pathInfo.StartsWith("/swagger-ui") ? new StaticFileHandler() : null;
            });
        }

        public static bool IsEnabled => HostContext.HasPlugin<SwaggerFeature>();
    }
}