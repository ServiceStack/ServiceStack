using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using ServiceStack.ServiceHost;
using ServiceStack.WebHost.Endpoints;

namespace ServiceStack.Swagger
{
    public class Resources
    {
        public string ApiKey { get; set; }
    }

    public class ResourcesResponse
    {
        public string SwaggerVersion { get; set; }
        public string ApiVersion { get; set; }
        public string BasePath { get; set; }
        public List<RestService> Apis { get; set; }
    }

    public class RestService
    {
        public string Path { get; set; }
        public string Description { get; set; }
    }

    [DefaultRequest(typeof(Resources))] 
    public class SwaggerResourcesService : ServiceInterface.Service
    {
        private readonly Regex resourcePathCleanerRegex = new Regex(@"/[^\/\{]*", RegexOptions.Compiled);
        internal static Regex resourceFilterRegex;

        public object Get(Resources request)
        {
            var result = new ResourcesResponse {
                SwaggerVersion = "1.1",
                BasePath = Request.GetApplicationUrl(),
                Apis = new List<RestService>()
            };
            var operations = EndpointHost.Metadata;
            var allTypes = operations.GetAllTypes();
            var allOperationNames = operations.GetAllOperationNames();
            for (var i = 0; i < allOperationNames.Count; i++)
            {
                var operationName = allOperationNames[i];
                if (resourceFilterRegex != null && !resourceFilterRegex.IsMatch(operationName)) continue;
                var operationType = allTypes.FirstOrDefault(x => x.Name == operationName);
                if (operationType == null) continue;
                if (operationType == typeof(Resources) || operationType == typeof(ResourceRequest))
                    continue;

                CreateRestPaths(result.Apis, operationType, operationName);
            }
            return result;
        }

        protected void CreateRestPaths(List<RestService> apis, Type operationType, String operationName)
        {
            var map = EndpointHost.ServiceManager.ServiceController.RestPathMap;
            var paths = new List<string>();
            foreach (var key in map.Keys)
            {
                paths.AddRange(map[key].Where(x => x.RequestType == operationType).Select(t => resourcePathCleanerRegex.Match(t.Path).Value));
            }

            if (paths.Count == 0) return;

            var minPath = paths.Min();
            if (string.IsNullOrEmpty(minPath) || minPath == "/") return;

            apis.Add(new RestService {
                Path = string.Concat("/resource", minPath),
                Description = operationType.GetDescription()
            });
        }
    }
}
