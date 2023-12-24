using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using ServiceStack.Host.Handlers;
using ServiceStack.IO;
using ServiceStack.Api.OpenApi.Specification;
using ServiceStack.Auth;

namespace ServiceStack.Api.OpenApi
{
    public class OpenApiFeature : IPlugin, IPreInitPlugin, Model.IHasStringId
    {
        public string Id { get; set; } = Plugins.OpenApi;
        /// <summary>
        /// Gets or sets <see cref="Regex"/> pattern to filter available resources. 
        /// </summary>
        public string ResourceFilterPattern { get; set; }

        public bool UseCamelCaseSchemaPropertyNames { get; set; }

        public bool UseLowercaseUnderscoreSchemaPropertyNames { get; set; }

        public bool DisableAutoDtoInBodyParam { get; set; }

        public string LogoUrl { get; set; }

        public string LogoHref { get; set; }

        public Action<OpenApiDeclaration> ApiDeclarationFilter { get; set; }

        /// <summary>
        /// Operation filter. Action takes a verb and operation as parameters
        /// </summary>
        public Action<string, OpenApiOperation> OperationFilter { get; set; }

        public Action<OpenApiSchema> SchemaFilter { get; set; }

        public Action<OpenApiProperty> SchemaPropertyFilter { get; set; }
        
        public List<OpenApiTag> Tags { get; set; }

        public List<string> AnyRouteVerbs { get; set; }

        public List<string> InlineSchemaTypesInNamespaces { get; set; }

        public bool DisableSwaggerUI { get; set; }

        public Dictionary<string, OpenApiSecuritySchema> SecurityDefinitions { get; set; }

        public Dictionary<string, List<string>> OperationSecurity { get; set; }

        public bool UseBearerSecurity
        {
            set
            {
                SecurityDefinitions = new Dictionary<string, OpenApiSecuritySchema> {
                    { "Bearer", new OpenApiSecuritySchema {
                        Type = "apiKey",
                        Name = "Authorization",
                        In = "header",
                    } }
                };
                OperationSecurity = new Dictionary<string, List<string>> {
                    { "Bearer", new List<string>() }
                };
            }
        }

        public bool UseBasicSecurity
        {
            set
            {
                SecurityDefinitions = new Dictionary<string, OpenApiSecuritySchema> {
                    { "basic", new OpenApiSecuritySchema { Type = "basic" } }
                };
                OperationSecurity = new Dictionary<string, List<string>> {
                    { "basic", new List<string>() }
                };
            }
        }

        public OpenApiFeature()
        {
            Tags = new List<OpenApiTag>();
            AnyRouteVerbs = new List<string> { HttpMethods.Get, HttpMethods.Post, HttpMethods.Put, HttpMethods.Delete };
            InlineSchemaTypesInNamespaces = new List<string>();
        }

        public void BeforePluginsLoaded(IAppHost appHost)
        {
            appHost.Config.EmbeddedResourceSources.Add(typeof(OpenApiFeature).Assembly);

            if (!DisableSwaggerUI)
            {
                appHost.ConfigurePlugin<MetadataFeature>(
                    feature => feature.AddPluginLink("swagger-ui/", "Swagger UI"));
            }
        }

        public void Register(IAppHost appHost)
        {
            if (ResourceFilterPattern != null)
                OpenApiService.ResourceFilterRegex = new Regex(ResourceFilterPattern, RegexOptions.Compiled);

            if (SecurityDefinitions == null && OperationSecurity == null)
            {
                var useBasicAuth = appHost.GetPlugin<AuthFeature>()?.AuthProviders
                   ?.Any(x => x.Provider == AuthenticateService.BasicProvider) == true;
                if (!useBasicAuth)
                    UseBearerSecurity = true;
                else
                    UseBasicSecurity = true;
            }

            OpenApiService.UseCamelCaseSchemaPropertyNames = UseCamelCaseSchemaPropertyNames;
            OpenApiService.UseLowercaseUnderscoreSchemaPropertyNames = UseLowercaseUnderscoreSchemaPropertyNames;
            OpenApiService.DisableAutoDtoInBodyParam = DisableAutoDtoInBodyParam;
            OpenApiService.ApiDeclarationFilter = ApiDeclarationFilter;
            OpenApiService.OperationFilter = OperationFilter;
            OpenApiService.SchemaFilter = SchemaFilter;
            OpenApiService.SchemaPropertyFilter = SchemaPropertyFilter;
            OpenApiService.AnyRouteVerbs = AnyRouteVerbs.ToArray();
            OpenApiService.InlineSchemaTypesInNamespaces = InlineSchemaTypesInNamespaces.ToArray();
            OpenApiService.SecurityDefinitions = SecurityDefinitions;
            OpenApiService.OperationSecurity = OperationSecurity;

            appHost.RegisterService(typeof(OpenApiService), "/openapi");

            if (!DisableSwaggerUI)
            {
                appHost.CatchAllHandlers.Add(req =>
                {
                    var pathInfo = req.PathInfo;
                    IVirtualFile indexFile;
                    IVirtualFile patchFile = null;
                    IVirtualFile patchPreLoadFile = null;
                    pathInfo = pathInfo.TrimStart('/');
                    switch (pathInfo)
                    {
                        case "swagger-ui/":
                        case "swagger-ui/default.html":
                            indexFile = appHost.VirtualFileSources.GetFile("/swagger-ui/index.html");
                            patchFile = appHost.VirtualFileSources.GetFile("/swagger-ui/patch.js");
                            patchPreLoadFile = appHost.VirtualFileSources.GetFile("/swagger-ui/patch-preload.js");
                            break;
                        default:
                            indexFile = null;
                            break;
                    }
                    if (indexFile != null)
                    {
                        var html = indexFile.ReadAllText();
                        var injectJs = patchFile?.ReadAllText();
                        var injectPreloadJs = patchPreLoadFile?.ReadAllText();

                        return new CustomResponseHandler((req, res) =>
                        {
                            res.ContentType = MimeTypes.HtmlUtf8; //use alt HTML ContentType so it's not overridden when Feature.Html is removed
                            var resourcesUrl = req.ResolveAbsoluteUrl("~/openapi");
                            html = html.Replace("http://petstore.swagger.io/v2/swagger.json", resourcesUrl)
                                .Replace("ApiDocs", HostContext.ServiceName)
                                .Replace("<span class=\"logo__title\">swagger</span>", $"<span class=\"logo__title\">{HostContext.ServiceName}</span>")
                                .Replace("http://swagger.io", LogoHref ?? "./");

                            if (LogoUrl != null)
                                html = html.Replace("images/logo_small.png", LogoUrl);

                            if (injectPreloadJs != null)
                            {
                                html = html.Replace("window.swaggerUi.load();", injectPreloadJs + "\n\n      window.swaggerUi.load();");
                            }

                            if (injectJs != null)
                            {
                                html = html.Replace("</body>",
                                    "<script type='text/javascript'>" + injectJs + "</script></body>");
                            }

                            return html;
                        });
                    }
                    return pathInfo.StartsWith("/swagger-ui/") 
                        ? new StaticFileHandler() 
                        : null;
                });
            }
        }

        public static bool IsEnabled => HostContext.HasPlugin<OpenApiFeature>();
    }
}