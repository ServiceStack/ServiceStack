using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using ServiceStack.Host;

namespace ServiceStack.Api.Swagger
{
    [DataContract]
    public class Resources
    {
        [DataMember(Name = "apiKey")]
        public string ApiKey { get; set; }
    }

    [DataContract]
    public class ResourcesResponse
    {
        [DataMember(Name = "swaggerVersion")]
        public string SwaggerVersion { get; set; }
        [DataMember(Name = "apiVersion")]
        public string ApiVersion { get; set; }
        [DataMember(Name = "basePath")]
        public string BasePath { get; set; }
        [DataMember(Name = "apis")]
        public List<RestService> Apis { get; set; }
    }

    [DataContract]
    public class RestService
    {
        [DataMember(Name = "path")]
        public string Path { get; set; }
        [DataMember(Name = "description")]
        public string Description { get; set; }
    }

    [AddHeader(DefaultContentType = MimeTypes.Json)]
    [DefaultRequest(typeof(Resources))]
    [Restrict(VisibilityTo = RequestAttributes.None)]
    public class SwaggerResourcesService : Service
    {
        private readonly Regex resourcePathCleanerRegex = new Regex(@"/[^\/\{]*", RegexOptions.Compiled);
        internal static Regex resourceFilterRegex;

        internal const string RESOURCE_PATH = "/resource";

        public object Get(Resources request)
        {
            var basePath = HostContext.Config.WebHostUrl 
                ?? Request.GetParentPathUrl().NormalizeScheme();

            var result = new ResourcesResponse
            {
                SwaggerVersion = "1.1",
                BasePath = basePath,
                Apis = new List<RestService>(),
                ApiVersion = HostContext.Config.ApiVersion
            };
            var operations = HostContext.Metadata;
            var allTypes = operations.GetAllTypes();
            var allOperationNames = operations.GetAllOperationNames();
            foreach (var operationName in allOperationNames)
            {
                if (resourceFilterRegex != null && !resourceFilterRegex.IsMatch(operationName)) continue;
                var name = operationName;
                var operationType = allTypes.FirstOrDefault(x => x.Name == name);
                if (operationType == null) continue;
                if (operationType == typeof(Resources) || operationType == typeof(ResourceRequest))
                    continue;
                if (!operations.IsVisible(Request, Format.Json, operationName)) continue;

                CreateRestPaths(result.Apis, operationType, operationName);
            }

            result.Apis = result.Apis.OrderBy(a => a.Path).ToList();
            return result;
        }

        protected void CreateRestPaths(List<RestService> apis, Type operationType, String operationName)
        {
            var map = HostContext.ServiceController.RestPathMap;
            var paths = new List<string>();
            foreach (var key in map.Keys)
            {
                paths.AddRange(map[key].Where(x => x.RequestType == operationType).Select(t => resourcePathCleanerRegex.Match(t.Path).Value));
            }

            if (paths.Count == 0) return;

            var basePaths = paths.Select(t => string.IsNullOrEmpty(t) ? null : t.Split('/'))
                .Where(t => t != null && t.Length > 1)
                .Select(t => t[1]);

            foreach (var bp in basePaths)
            {
                if (string.IsNullOrEmpty(bp)) continue;
                if (apis.All(a => a.Path != string.Concat(RESOURCE_PATH, "/" + bp)))
                {
                    apis.Add(new RestService
                    {
                        Path = string.Concat(RESOURCE_PATH, "/" + bp),
                        Description = operationType.GetDescription()
                    });
                }
            }
        }
    }
}
