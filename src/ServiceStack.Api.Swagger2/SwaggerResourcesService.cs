using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using ServiceStack.Text;

namespace ServiceStack.Api.Swagger
{
    [DataContract]
    public class SwaggerResources : IReturn<SwaggerResourcesResponse>
    {
        [DataMember(Name = "apiKey")]
        public string ApiKey { get; set; }
    }

    [DataContract]
    public class SwaggerResourcesResponse
    {
        [DataMember(Name = "swaggerVersion")]
        public string SwaggerVersion
        {
            get { return "1.2"; }
        }
        [DataMember(Name = "apis")]
        public List<SwaggerResourceRef> Apis { get; set; }
        [DataMember(Name = "apiVersion")]
        public string ApiVersion { get; set; }
        [DataMember(Name = "basePath")]
        public string BasePath { get; set; }
        [DataMember(Name = "info")]
        public SwaggerInfo Info { get; set; }
    }

    [DataContract]
    public class SwaggerInfo
    {
        [DataMember(Name = "title")]
        public string Title { get; set; }
        [DataMember(Name = "description")]
        public string Description { get; set; }
        [DataMember(Name = "termsOfServiceUrl")]
        public string TermsOfServiceUrl { get; set; }
        [DataMember(Name = "contact")]
        public string Contact { get; set; }
        [DataMember(Name = "license")]
        public string License { get; set; }
        [DataMember(Name = "licenseUrl")]
        public string LicenseUrl { get; set; }
    }

    [DataContract]
    public class SwaggerResourceRef
    {
        [DataMember(Name = "path")]
        public string Path { get; set; }
        [DataMember(Name = "description")]
        public string Description { get; set; }
    }

    [AddHeader(DefaultContentType = MimeTypes.Json)]
    [DefaultRequest(typeof(SwaggerResources))]
    [Restrict(VisibilityTo = RequestAttributes.None)]
    public class SwaggerResourcesService : Service
    {
        private readonly Regex resourcePathCleanerRegex = new Regex(@"/[^\/\{]*", RegexOptions.Compiled);
        internal static Regex resourceFilterRegex;

        internal static Action<SwaggerResourcesResponse> ResourcesResponseFilter { get; set; }

        internal const string RESOURCE_PATH = "/resource";

        public object Get(SwaggerResources request)
        {
            var basePath = base.Request.GetBaseUrl();

            var result = new SwaggerResourcesResponse
            {
                BasePath = basePath,
                Apis = new List<SwaggerResourceRef>(),
                ApiVersion = HostContext.Config.ApiVersion,
                Info = new SwaggerInfo
                {
                    Title = HostContext.ServiceName,
                }
            };
            var operations = HostContext.Metadata;
            var allTypes = operations.GetAllOperationTypes();
            var allOperationNames = operations.GetAllOperationNames();
            foreach (var operationName in allOperationNames)
            {
                if (resourceFilterRegex != null && !resourceFilterRegex.IsMatch(operationName)) continue;
                var name = operationName;
                var operationType = allTypes.FirstOrDefault(x => x.Name == name);
                if (operationType == null) continue;
                if (operationType == typeof(SwaggerResources) || operationType == typeof(SwaggerResource))
                    continue;
                if (!operations.IsVisible(Request, Format.Json, operationName)) continue;

                CreateRestPaths(result.Apis, operationType, operationName);
            }

            result.Apis = result.Apis.OrderBy(a => a.Path).ToList();

            if (ResourcesResponseFilter != null)
                ResourcesResponseFilter(result);

            return new HttpResult(result) {
                ResultScope = () => JsConfig.With(includeNullValues:false)
            };
        }

        protected void CreateRestPaths(List<SwaggerResourceRef> apis, Type operationType, string operationName)
        {
            var map = HostContext.ServiceController.RestPathMap;
            var feature = HostContext.GetPlugin<SwaggerFeature>();

            var paths = new List<string>();

            foreach (var key in map.Keys)
            {
                paths.AddRange(map[key].Where(x => x.RequestType == operationType).Select(t => resourcePathCleanerRegex.Match(t.Path).Value));
            }

            if (paths.Count == 0)
                return;

            var basePaths = paths.Select(t => string.IsNullOrEmpty(t) ? null : t.Split('/'))
                .Where(t => t != null && t.Length > 1)
                .Select(t => t[1]);

            foreach (var basePath in basePaths)
            {
                if (string.IsNullOrEmpty(basePath))
                    continue;

                if (apis.All(a => a.Path != string.Concat(RESOURCE_PATH, "/" + basePath)))
                {
                    string summary;
                    feature.RouteSummary.TryGetValue("/" + basePath, out summary);

                    apis.Add(new SwaggerResourceRef
                    {
                        Path = string.Concat(RESOURCE_PATH, "/" + basePath),
                        Description = summary ?? operationType.GetDescription()
                    });
                }
            }
        }
    }
}
