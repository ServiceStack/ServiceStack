using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using ServiceStack.ServiceHost;
using ServiceStack.WebHost.Endpoints;

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

	[DefaultRequest(typeof(Resources))]
	public class SwaggerResourcesService : ServiceInterface.Service
	{
		private readonly Regex resourcePathCleanerRegex = new Regex(@"/[^\/\{]*", RegexOptions.Compiled);
		internal static Regex resourceFilterRegex;

		internal const string RESOURCE_PATH = "/resource";

		public object Get(Resources request)
		{
			var result = new ResourcesResponse
			{
				SwaggerVersion = "1.1",
				BasePath = Request.GetParentPathUrl(),
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

		    var basePaths = paths.Select(t => string.IsNullOrEmpty(t) ? null : t.Split('/'))
		        .Where(t => t != null && t.Length > 1)
		        .Select(t => t[1]);

            foreach (var bp in basePaths)
            {
                if (string.IsNullOrEmpty(bp)) return;
                if (!apis.Any(a => a.Path == string.Concat(RESOURCE_PATH, "/" + bp)))
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
