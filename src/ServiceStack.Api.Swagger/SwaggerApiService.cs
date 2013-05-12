using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using ServiceStack.Common;
using ServiceStack.Common.Extensions;
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
        [DataMember(Name = "models")]
        public Dictionary<string, SwaggerModel> Models { get; set; }
    }

    [DataContract]
    public class SwaggerModel
    {
        [DataMember(Name = "id")]
        public string Id { get; set; }
        [DataMember(Name = "properties")]
        public Dictionary<string, ModelProperty> Properties { get; set; }
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
        [DataMember(Name = "responseClass")]
        public string ResponseClass { get; set; }
        [DataMember(Name = "errorResponses")]
        public List<ErrorResponseStatus> ErrorResponses { get; set; }
    }

    [DataContract]
    public class ErrorResponseStatus
    {
        [DataMember(Name = "code")]
        public int StatusCode { get; set; }
        [DataMember(Name = "reason")]
        public string Reason { get; set; }
    }

    [DataContract]
    public class ModelProperty
    {
        [DataMember(Name = "description")]
        public string Description { get; set; }
        [DataMember(Name = "type")]
        public string Type { get; set; }
        [DataMember(Name = "items")]
        public Dictionary<string, string> Items { get; set; }
        [DataMember(Name = "allowableValues")]
        public ParameterAllowableValues AllowableValues { get; set; }
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
        [DataMember(Name = "allowableValues")]
        public ParameterAllowableValues AllowableValues { get; set; }
    }

	[DataContract]
	public class ParameterAllowableValues
	{
		[DataMember(Name = "valueType")]
		public string ValueType { get; set; }

		[DataMember(Name = "values")]
		public string[] Values { get; set; }

		[DataMember(Name = "min")]
		public int? Min { get; set; }

		[DataMember(Name = "max")]
		public int? Max { get; set; }
	}

    [DefaultRequest(typeof(ResourceRequest))]
    public class SwaggerApiService : ServiceInterface.Service
    {
        internal static bool UseCamelCaseModelPropertyNames { get; set; }
        internal static bool UseLowercaseUnderscoreModelPropertyNames { get; set; }
        private readonly Regex nicknameCleanerRegex = new Regex(@"[\{\}\*\-_/]*", RegexOptions.Compiled);

        public object Get(ResourceRequest request)
        {
            var httpReq = RequestContext.Get<IHttpRequest>();
            var path = "/" + request.Name;
            var map = EndpointHost.ServiceManager.ServiceController.RestPathMap;
            var paths = new List<RestPath>();

            var basePath = EndpointHost.Config.UseHttpsLinks ? httpReq.GetParentPathUrl().ToHttps() : httpReq.GetParentPathUrl();

			if (basePath.ToLower().EndsWith(SwaggerResourcesService.RESOURCE_PATH))
			{
				basePath = basePath.Substring(0, basePath.ToLower().LastIndexOf(SwaggerResourcesService.RESOURCE_PATH));
			}

            foreach (var key in map.Keys)
            {
                paths.AddRange(map[key].Where(x => x.Path == path || x.Path.StartsWith(path + "/")));
            }

            var models = new Dictionary<string, SwaggerModel>();
            foreach (var restPath in paths)
            {
                ParseModel(models, restPath.RequestType);
            }

            return new ResourceResponse {
                ResourcePath = path,
                BasePath = basePath,
                Apis = new List<MethodDescription>(paths.Select(p => FormateMethodDescription(p, models)).ToArray().OrderBy(md => md.Path)),
                Models = models
            };
        }

        private static readonly Dictionary<string, string> ClrTypesToSwaggerScalarTypes = new Dictionary<string, string> {
                                                                                                  {"int32", "int"},
                                                                                                  {"int64", "int"},
                                                                                                  {"int", "int"},
                                                                                                  {"byte", "byte"},
                                                                                                  {"bool", "bool"},
                                                                                                  {"boolean", "bool"},
                                                                                                  {"string", "string"},
                                                                                                  {"datetime", "datetime"}
                                                                                              };

        private static bool IsSwaggerScalarType(Type type)
        {
            return ClrTypesToSwaggerScalarTypes.ContainsKey(type.Name.ToLowerInvariant()) || type.IsEnum;
        }

        private static string GetSwaggerTypeName(Type type)
        {
            return ClrTypesToSwaggerScalarTypes.ContainsKey(type.Name.ToLowerInvariant())
                ? ClrTypesToSwaggerScalarTypes[type.Name.ToLowerInvariant()]
                : type.Name;
        }

        private static Type GetListElementType(Type type)
        {
            if (type.IsArray) return type.GetElementType();

            if (!type.IsGenericType) return null;
            var genericType = type.GetGenericTypeDefinition();
            if (genericType == typeof(List<>) || genericType == typeof(IList<>) || genericType == typeof(IEnumerable<>))
                return type.GetGenericArguments()[0];
            return null;
        }

        private static bool IsListType(Type type)
        {
            return GetListElementType(type) != null;
        }

        private static void ParseModel(IDictionary<string, SwaggerModel> models, Type modelType)
        {
            if (IsSwaggerScalarType(modelType)) return;

            var modelId = modelType.Name;
            if (models.ContainsKey(modelId)) return;

            var model = new SwaggerModel {
                                Id = modelId,
                                Properties = new Dictionary<string, ModelProperty>()
                            };
            models[model.Id] = model;

            foreach (var prop in modelType.GetProperties())
            {
                var allApiDocAttributes = prop
                    .GetCustomAttributes(typeof(ApiMemberAttribute), true)
                    .OfType<ApiMemberAttribute>()
                    .Where(attr => prop.Name.Equals(attr.Name, StringComparison.InvariantCultureIgnoreCase))
                    .ToList();
                var apiDoc = allApiDocAttributes.FirstOrDefault(attr => attr.ParameterType == "body");

                if (allApiDocAttributes.Any() && apiDoc == null) continue;

                var propertyType = prop.PropertyType;
                var modelProp = new ModelProperty { Type = GetSwaggerTypeName(propertyType) };

                if (IsListType(propertyType))
                {
                    modelProp.Type = "Array";
                    var listItemType = GetListElementType(propertyType);
                    modelProp.Items = new Dictionary<string, string> {
                                              {IsSwaggerScalarType(listItemType) ? "type" : "$ref", GetSwaggerTypeName(listItemType)}
                                          };
                    ParseModel(models, listItemType);
                }
                else if (propertyType.IsEnum)
                {
                    modelProp.Type = "string";
                    modelProp.AllowableValues = new ParameterAllowableValues {
                                                        Values = Enum.GetNames(propertyType),
                                                        ValueType = "LIST"
                                                    };
                }
                else
                {
                    ParseModel(models, propertyType);
                }

                var descriptionAttr = prop.GetCustomAttributes(typeof(DescriptionAttribute), true).OfType<DescriptionAttribute>().FirstOrDefault();
                if (descriptionAttr != null)
                    modelProp.Description = descriptionAttr.Description;

                if (apiDoc != null)
                    modelProp.Description = apiDoc.Description;

                var allowableValues = prop.GetCustomAttributes(typeof(ApiAllowableValuesAttribute), true).OfType<ApiAllowableValuesAttribute>().FirstOrDefault();
                if (allowableValues != null)
                    modelProp.AllowableValues = GetAllowableValue(allowableValues);

                model.Properties[GetModelPropertyName(prop)] = modelProp;
            }
        }

        private static string GetModelPropertyName(PropertyInfo prop)
        {
            return UseCamelCaseModelPropertyNames
                ? (UseLowercaseUnderscoreModelPropertyNames ? prop.Name.ToLowercaseUnderscore() : prop.Name.ToCamelCase())
                : prop.Name;
        }

        private static string GetResponseClass(IRestPath restPath, IDictionary<string, SwaggerModel> models)
        {
            // Given: class MyDto : IReturn<X>. Determine the type X.
            foreach (var i in restPath.RequestType.GetInterfaces())
            {
                if (i.IsGenericType && i.GetGenericTypeDefinition() == typeof (IReturn<>))
                {
                    var returnType = i.GetGenericArguments()[0];
                    // Handle IReturn<List<SomeClass>> or IReturn<SomeClass[]>
                    if (IsListType(returnType))
                    {
                        var listItemType = GetListElementType(returnType);
                        ParseModel(models, listItemType);
                        return string.Format("List[{0}]", GetSwaggerTypeName(listItemType));
                    }
                    ParseModel(models, returnType);
                    return GetSwaggerTypeName(i.GetGenericArguments()[0]);
                }
            }

            return null;
        }

        private static List<ErrorResponseStatus> GetMethodResponseCodes(Type requestType)
        {
            return requestType
                .GetCustomAttributes(typeof (ApiResponseAttribute), true)
                .OfType<ApiResponseAttribute>()
                .Select(x => new ErrorResponseStatus {
                    StatusCode = (int)x.StatusCode,
                    Reason = x.Description
                }).ToList();
        }

        private MethodDescription FormateMethodDescription(RestPath restPath, Dictionary<string, SwaggerModel> models)
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
                        Parameters = ParseParameters(verb, restPath.RequestType),
                        ResponseClass = GetResponseClass(restPath, models),
                        ErrorResponses = GetMethodResponseCodes(restPath.RequestType)
                    }).ToList()
            };
            return md;
        }

		private static ParameterAllowableValues GetAllowableValue(ApiAllowableValuesAttribute attr)
		{
			if (attr != null)
			{
				return new ParameterAllowableValues() {
					ValueType = attr.Type,
					Values = attr.Values,
					Max = attr.Max,
					Min = attr.Min
				};
			}
			return null;
		}

        private static List<MethodOperationParameter> ParseParameters(string verb, Type operationType)
        {
            var properties = operationType.GetProperties();
            var paramAttrs = new Dictionary<string, ApiMemberAttribute[]>();
			var allowableParams = new List<ApiAllowableValuesAttribute>();

			foreach (var property in properties)
			{
                paramAttrs[property.Name] = (ApiMemberAttribute[])property.GetCustomAttributes(typeof(ApiMemberAttribute), true);
				allowableParams.AddRange(property.GetCustomAttributes(typeof(ApiAllowableValuesAttribute), true).Cast<ApiAllowableValuesAttribute>().ToArray());
			}

            var methodOperationParameters = new List<MethodOperationParameter>();
            foreach (var k in paramAttrs.Keys)
            {
                var value = paramAttrs[k];
                methodOperationParameters.AddRange(
                    from ApiMemberAttribute p in value
                    where p.Verb == null || string.Compare(p.Verb, verb, StringComparison.InvariantCultureIgnoreCase) == 0
                    select new MethodOperationParameter {
                        DataType = p.DataType,
                        AllowMultiple = p.AllowMultiple,
                        Description = p.Description,
                        Name = p.Name ?? k,
                        ParamType = p.ParameterType,
                        Required = p.IsRequired,
						AllowableValues = GetAllowableValue(allowableParams.FirstOrDefault(attr => attr.Name == p.Name))
                    });
            }
            return methodOperationParameters;
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
