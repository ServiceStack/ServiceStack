using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using ServiceStack.ServiceHost;
using ServiceStack.WebHost.Endpoints;
using ServiceStack.WebHost.Endpoints.Metadata;

namespace ServiceStack.ServiceInterface.Swagger
{
    public class ResourcesRequest
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

    public class SwaggerResourcesService : RestServiceBase<ResourcesRequest>
    {
        private readonly Regex resourcePathCleanerRegex = new Regex(@"/[^\/\{]*", RegexOptions.Compiled);

        public override object OnGet(ResourcesRequest request)
        {
            var httpReq = RequestContext.Get<IHttpRequest>();
            var result = new ResourcesResponse
                             {
                                 SwaggerVersion = "1.1",
                                 BasePath = httpReq.GetApplicationUrl(), 
                                 Apis = new List<RestService>()
                             };
            var operations = EndpointHost.ServiceOperations;
            var allTypes = operations.AllOperations.Types;
            for (var i = 0; i < operations.AllOperations.Names.Count; i++)
            {
                var operationName = operations.AllOperations.Names[i];
				var operationType = allTypes.FirstOrDefault(x => x.Name == operationName);
                if (operationType == null) continue;
                if (operationType == typeof(ResourcesRequest) || operationType == typeof(ResourceRequest))
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

            apis.Add(new RestService
            {
                Path = string.Concat("/resource", minPath),
                Description = BaseMetadataHandler.GetDescriptionFromOperationType(operationType)
            });
        }
    }
}
