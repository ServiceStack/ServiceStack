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
    public class ResourceRequest
    {
        [DataMember(Name = "apiKey")]
        public string ApiKey { get; set; }
        [DataMember(Name = "name")]
        public string Name { get; set; }
    }

    [DataContract]
    public class ResourceResponse
    {
        [DataMember(Name = "apiVersion")]
        public string ApiVersion { get; set; }
        [DataMember(Name = "basePath")]
        public string BasePath { get; set; }
        [DataMember(Name = "resourcePath")]
        public string ResourcePath { get; set; }
        [DataMember(Name = "apis")]
        public List<MethodDescription> Apis { get; set; }
    }

    [DataContract]
    public class MethodDescription
    {
        [DataMember(Name = "path")]
        public string Path { get; set; }
        [DataMember(Name = "description")]
        public string Description { get; set; }
        [DataMember(Name = "operations")]
        public List<MethodOperation> Operations { get; set; }
    }

    [DataContract]
    public class MethodOperation
    {
        [DataMember(Name = "httpMethod")]
        public string HttpMethod { get; set; }
        [DataMember(Name = "nickname")]
        public string Nickname { get; set; }
        [DataMember(Name = "summary")]
        public string Summary { get; set; }
        [DataMember(Name = "notes")]
        public string Notes { get; set; }
        [DataMember(Name = "parameters")]
        public List<MethodOperationParameter> Parameters { get; set; }
    }

    [DataContract]
    public class MethodOperationParameter
    {
        [DataMember(Name = "name")]
        public string Name { get; set; }
        [DataMember(Name = "description")]
        public string Description { get; set; }
        [DataMember(Name = "paramType")]
        public string ParamType { get; set; }
        [DataMember(Name = "allowMultiple")]
        public bool AllowMultiple { get; set; }
        [DataMember(Name = "required")]
        public bool Required { get; set; }
        [DataMember(Name = "dataType")]
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
