using System;
using System.Text.RegularExpressions;
using ServiceStack.Host.Handlers;

namespace ServiceStack.Api.Swagger
{
    public class SwaggerFeature : IPlugin
    {
        /// <summary>
        /// Gets or sets <see cref="Regex"/> pattern to filter available resources. 
        /// </summary>
        public string ResourceFilterPattern { get; set; }

        public bool UseCamelCaseModelPropertyNames { get; set; }

        public bool UseLowercaseUnderscoreModelPropertyNames { get; set; }

        public bool DisableAutoDtoInBodyParam { get; set; }

        public Action<SwaggerModel> ModelFilter { get; set; }

        public Action<ModelProperty> ModelPropertyFilter { get; set; }

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

            appHost.GetPlugin<MetadataFeature>()
                .AddPluginLink("swagger-ui/", "Swagger UI");

            appHost.CatchAllHandlers.Add((httpMethod, pathInfo, filePath) =>
            {
                if (pathInfo == "/swagger-ui" || pathInfo == "/swagger-ui/" || pathInfo == "/swagger-ui/default.html")
                {
                    var indexFile = appHost.VirtualPathProvider.GetFile("/swagger-ui/index.html");
                    if (indexFile != null)
                    {
                        var html = indexFile.ReadAllText();

                        return new CustomResponseHandler((req, res) =>
                        {
                            res.ContentType = MimeTypes.Html;
                            var resourcesUrl = req.ResolveAbsoluteUrl("~/resources");
                            html = html.Replace("http://petstore.swagger.wordnik.com/api/api-docs", resourcesUrl);
                            return html;
                        });
                    }
                }
                return null;
            });
        }

        public static bool IsEnabled
        {
            get { return HostContext.HasPlugin<SwaggerFeature>(); }
        }
    }
}