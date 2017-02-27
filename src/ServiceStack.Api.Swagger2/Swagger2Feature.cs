using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using ServiceStack.Host.Handlers;
using ServiceStack.IO;

namespace ServiceStack.Api.Swagger2
{
    public class Swagger2Feature : IPlugin, IPreInitPlugin
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

        public Action<Swagger2ApiDeclaration> ApiDeclarationFilter { get; set; }

        public Action<Swagger2Operation> OperationFilter { get; set; }

        //public Action<Swagger2Model> ModelFilter { get; set; }

        public Action<Swagger2Property> ModelPropertyFilter { get; set; }
        
        public Dictionary<string, string> RouteSummary { get; set; }

        public Swagger2Feature()
        {
            LogoUrl = "//raw.githubusercontent.com/ServiceStack/Assets/master/img/artwork/logo-24.png";
            RouteSummary = new Dictionary<string, string>();
        }

        public void Configure(IAppHost appHost)
        {
            appHost.Config.EmbeddedResourceSources.Add(typeof(Swagger2Feature).GetAssembly());
        }

        public void Register(IAppHost appHost)
        {
//            if (ResourceFilterPattern != null)
//                Swagger2ApiService.resourceFilterRegex = new Regex(ResourceFilterPattern, RegexOptions.Compiled);

            Swagger2ApiService.UseCamelCaseModelPropertyNames = UseCamelCaseModelPropertyNames;
            Swagger2ApiService.UseLowercaseUnderscoreModelPropertyNames = UseLowercaseUnderscoreModelPropertyNames;
            Swagger2ApiService.DisableAutoDtoInBodyParam = DisableAutoDtoInBodyParam;
            Swagger2ApiService.ApiDeclarationFilter = ApiDeclarationFilter;
            Swagger2ApiService.OperationFilter = OperationFilter;
            //Swagger2ApiService.ModelFilter = ModelFilter;
            Swagger2ApiService.ModelPropertyFilter = ModelPropertyFilter;

            appHost.RegisterService(typeof(Swagger2ApiService), new[] { "/swagger2-api" });

            var swaggerUrl = UseBootstrapTheme
                ? "swagger2-ui-bootstrap/"
                : "swagger2-ui/";

            appHost.GetPlugin<MetadataFeature>()
                .AddPluginLink(swaggerUrl, "Swagger2 UI");

            appHost.CatchAllHandlers.Add((httpMethod, pathInfo, filePath) =>
            {
                IVirtualFile indexFile;
                IVirtualFile patchFile = null;
                switch (pathInfo)
                {
                    case "/swagger2-ui":
                    case "/swagger2-ui/":
                    case "/swagger2-ui/default.html":
                        indexFile = appHost.VirtualFileSources.GetFile("/swagger2-ui/index.html");
                        patchFile = appHost.VirtualFileSources.GetFile("/swagger2-ui/patch.js");
                        break;
                    case "/swagger2-ui-bootstrap":
                    case "/swagger2-ui-bootstrap/":
                    case "/swagger2-ui-bootstrap/index.html":
                        indexFile = appHost.VirtualFileSources.GetFile("/swagger2-ui-bootstrap/index.html");
                        break;
                    default:
                        indexFile = null;
                        break;
                }
                if (indexFile != null)
                {
                    var html = indexFile.ReadAllText();
                    var injectJs = patchFile != null
                        ? patchFile.ReadAllText()
                        : null;

                    return new CustomResponseHandler((req, res) =>
                    {
                        res.ContentType = MimeTypes.Html;
                        var resourcesUrl = req.ResolveAbsoluteUrl("~/swagger2-api");
                        html = html.Replace("http://petstore.swagger.io/v2/swagger.json", resourcesUrl)
                            .Replace("ApiDocs", HostContext.ServiceName)
                            .Replace("{LogoUrl}", LogoUrl);

                        if (injectJs != null)
                        {
                            html = html.Replace("</body>",
                                "<script type='text/javascript'>" + injectJs + "</script></body>");
                        }

                        return html;
                    });
                }
                return pathInfo.StartsWith("/swagger2-ui") ? new StaticFileHandler() : null;
            });
        }

        public static bool IsEnabled
        {
            get { return HostContext.HasPlugin<Swagger2Feature>(); }
        }
    }
}