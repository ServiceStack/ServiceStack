using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using ServiceStack.ServiceHost;
using ServiceStack.WebHost.Endpoints;

namespace ServiceStack.Api.Swagger
{
    public class ResourceRequest
    {
        public string ApiKey { get; set; }
        public string Name { get; set; }
    }

    public class ResourceResponse
    {
        public string ApiVersion { get; set; }
        public string BasePath { get; set; }
        public string ResourcePath { get; set; }
        public List<MethodDescription> Apis { get; set; }
    }

    public class MethodDescription
    {
        public string Path { get; set; }
        public string Description { get; set; }
        public List<MethodOperation> Operations { get; set; }
    }

    public class MethodOperation
    {
        public string HttpMethod { get; set; }
        public string Nickname { get; set; }
        public string Summary { get; set; }
        public string Notes { get; set; }
        public List<MethodOperationParameter> Parameters { get; set; }
    }

    public class MethodOperationParameter
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string ParamType { get; set; }
        public bool AllowMultiple { get; set; }
        public bool Required { get; set; }
        public string DataType { get; set; }
    }

    [DefaultRequest(typeof(ResourceRequest))]
    public class SwaggerApiService : ServiceInterface.Service
    {
        private readonly Regex nicknameCleanerRegex = new Regex(@"[\{\}\*\-_/]*", RegexOptions.Compiled);

        public object Get(ResourceRequest request)
        {
            var httpReq = RequestContext.Get<IHttpRequest>();
            var path = "/" + request.Name;
            var map = EndpointHost.ServiceManager.ServiceController.RestPathMap;
            var paths = new List<RestPath>();
            var basePath = httpReq.GetApplicationUrl();
            foreach (var key in map.Keys)
            {
                paths.AddRange(map[key].Where(x => x.Path == path || x.Path.StartsWith(path + "/")));
            }

            return new ResourceResponse {
                ResourcePath = path,
                BasePath = basePath,
                Apis = new List<MethodDescription>(paths.Select(FormateMethodDescription).ToArray())
            };
        }

        private MethodDescription FormateMethodDescription(RestPath restPath)
        {
            var verbs = new List<string>();
            var summary = restPath.Summary;
            var notes = restPath.Notes;

            if (restPath.AllowsAllVerbs)
            {
                verbs.AddRange(new[] { "GET", "POST", "PUT", "DELETE" });
            }
            else
                verbs.AddRange(restPath.AllowedVerbs.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries));

            var nickName = nicknameCleanerRegex.Replace(restPath.Path, "");

            var md = new MethodDescription {
                Path = restPath.Path,
                Description = summary,
                Operations = verbs.Select(verb =>
                    new MethodOperation {
                        HttpMethod = verb,
                        Nickname = verb.ToLowerInvariant() + nickName,
                        Summary = summary,
                        Notes = notes,
                        Parameters = ParseParameters(verb, restPath.RequestType)
                    }).ToList()
            };
            return md;
        }

        private static List<MethodOperationParameter> ParseParameters(string verb, Type operationType)
        {
            var properties = operationType.GetProperties();
            var paramAttrs = new List<object>();
            foreach (var property in properties)
                paramAttrs.AddRange(property.GetCustomAttributes(typeof(ApiMemberAttribute), true));

            return (from ApiMemberAttribute p in paramAttrs
                    where p.Verb == null || string.Compare(p.Verb, verb, StringComparison.InvariantCultureIgnoreCase) == 0
                    select new MethodOperationParameter {
                        DataType = p.DataType,
                        AllowMultiple = p.AllowMultiple,
                        Description = p.Description,
                        Name = p.Name,
                        ParamType = p.ParameterType,
                        Required = p.IsRequired
                    }).ToList();
        }

        /*
        public class SwaggerResourceService : HttpHandlerBase, IServiceStackHttpHandler
        {
            const string RestDescriptor = "/resource";

            public override void Execute(HttpContext context)
            {
                var writer = new HtmlTextWriter(context.Response.Output);
                context.Response.ContentType = "text/html";

                ProcessOperations(writer, new HttpRequestWrapper(GetType().Name, context.Request));
            }

            public virtual void ProcessRequest(IHttpRequest httpReq, IHttpResponse httpRes, string operationName)
            {
                using (var sw = new StreamWriter(httpRes.OutputStream))
                {
                    var writer = new HtmlTextWriter(sw);
                    httpRes.ContentType = "text/html";
                    ProcessOperations(writer, httpReq);
                }
            }

            protected virtual void ProcessOperations(HtmlTextWriter writer, IHttpRequest httpReq)
            {
                EndpointHost.Config.AssertFeatures(Feature.Metadata);

                var operations = EndpointHost.ServiceOperations;
                var operationName = httpReq.QueryString["op"];
                if (operationName != null)
                {
                    var allTypes = operations.AllOperations.Types;
                    var operationType = allTypes.Single(x => x.Name == operationName);
                    string responseMessage = null;
                }
            }

         */
    }
}
