using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using ServiceStack.Text;

namespace ServiceStack.Api.Swagger2
{
/*    [DataContract]
    public class Swagger2Resources : IReturn<Swagger2ResourcesResponse>
    {
        [DataMember(Name = "apiKey")]
        public string ApiKey { get; set; }
    }

    [DataContract]
    public class Swagger2ResourcesResponse
    {
        [DataMember(Name = "swaggerVersion")]
        public string SwaggerVersion
        {
            get { return "2.10"; }
        }
        [DataMember(Name = "apis")]
        public List<Swagger2ResourceRef> Apis { get; set; }
        [DataMember(Name = "apiVersion")]
        public string ApiVersion { get; set; }
        [DataMember(Name = "basePath")]
        public string BasePath { get; set; }
        [DataMember(Name = "info")]
        public Swagger2Info Info { get; set; }
    }


    [DataContract]
    public class Swagger2ResourceRef
    {
        [DataMember(Name = "path")]
        public string Path { get; set; }
        [DataMember(Name = "description")]
        public string Description { get; set; }
    }

    [AddHeader(DefaultContentType = MimeTypes.Json)]
    [DefaultRequest(typeof(Swagger2Resources))]
    [Restrict(VisibilityTo = RequestAttributes.None)]
    public class Swagger2ResourcesService : Service
    {
        private readonly Regex resourcePathCleanerRegex = new Regex(@"/[^\/\{]*", RegexOptions.Compiled);
        internal static Regex resourceFilterRegex;

        internal static Action<Swagger2ResourcesResponse> ResourcesResponseFilter { get; set; }

        internal const string RESOURCE_PATH = "/resource";

        public object Get(Swagger2Resources request)
        {
            var basePath = base.Request.GetBaseUrl();

            var result = new Swagger2ResourcesResponse
            {
                BasePath = basePath,
                Apis = new List<Swagger2ResourceRef>(),
                ApiVersion = HostContext.Config.ApiVersion,
                Info = new Swagger2Info
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
                if (operationType == typeof(Swagger2Resources) || operationType == typeof(Swagger2Resource))
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

        protected void CreateRestPaths(List<Swagger2ResourceRef> apis, Type operationType, string operationName)
        {
            var map = HostContext.ServiceController.RestPathMap;
            var feature = HostContext.GetPlugin<Swagger2Feature>();

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

                    apis.Add(new Swagger2ResourceRef
                    {
                        Path = string.Concat(RESOURCE_PATH, "/" + basePath),
                        Description = summary ?? operationType.GetDescription()
                    });
                }
            }
        }
    }
    */
}
