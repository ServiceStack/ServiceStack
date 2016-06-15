using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using ServiceStack.Text;
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

    /// <summary>
    /// https://raw.githubusercontent.com/OAI/OpenAPI-Specification/master/schemas/v1.2/dataType.json
    /// https://raw.githubusercontent.com/OAI/OpenAPI-Specification/master/schemas/v1.2/modelsObject.json
    /// </summary>
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

    /// <summary>
    /// https://raw.githubusercontent.com/OAI/OpenAPI-Specification/master/schemas/v1.2/operationObject.json
    /// </summary>
    [DataContract]
    public class MethodOperation
    {
        [DataMember(Name = "method")]
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
        [DataMember(Name = "responseMessages")]
        public List<ResponseMessage> ResponseMessages { get; set; }
    }

    /// <summary>
    /// https://raw.githubusercontent.com/OAI/OpenAPI-Specification/master/schemas/v1.2/operationObject.json
    /// </summary>
    [DataContract]
    public class ResponseMessage
    {
        [DataMember(Name = "code")]
        public int StatusCode { get; set; }
        [DataMember(Name = "message")]
        public string Reason { get; set; }
    }

    /// <summary>
    /// https://raw.githubusercontent.com/OAI/OpenAPI-Specification/master/schemas/v1.2/dataType.json
    /// </summary>
    [DataContract]
    public class ModelProperty
    {
        [DataMember(Name = "description")]
        public string Description { get; set; }
        [DataMember(Name = "type")]
        public string Type { get; set; }
        [DataMember(Name = "items")]
        public Dictionary<string, string> Items { get; set; }
        [DataMember(Name = "enum")]
        public string[] AllowedValues { get; set; }
        [DataMember(Name = "required")]
        public bool Required { get; set; }
        [DataMember(Name = "minimum")]
        public string Min { get; set; }
        [DataMember(Name = "maximum")]
        public string Max { get; set; }
        public ApiAllowableValuesAttribute AllowableValues
        {
            set
            {
                Items.Add("type", value.Type);
                AllowedValues = value.Values;
                Max = value.Max.ToString();
                Min = value.Min.ToString();
            }
        }
    }

    /// <summary>
    /// https://raw.githubusercontent.com/OAI/OpenAPI-Specification/master/schemas/v1.2/parameterObject.json
    /// https://raw.githubusercontent.com/OAI/OpenAPI-Specification/master/schemas/v1.2/dataTypeBase.json
    /// </summary>
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
        [DataMember(Name = "type")]
        public string DataType { get; set; }
        [DataMember(Name = "enum")]
        public string[] AllowedValues { get; set; }
        [DataMember(Name = "minimum")]
        public int? Min { get; set; }
        [DataMember(Name = "maximum")]
        public int? Max { get; set; }
        [DataMember(Name = "items")]
        public ParameterAllowableValues Items { get; set; }

        public ApiAllowableValuesAttribute AllowableValues
        {
            set
            {
                if (value != null)
                {
                    Items = new ParameterAllowableValues {ValueType = value.Type};
                    AllowedValues = value.Values;
                    Max = value.Max;
                    Min = value.Min;
                }
            }
        }
    }
    
    /// <summary>
    /// https://raw.githubusercontent.com/OAI/OpenAPI-Specification/master/schemas/v1.2/dataTypeBase.json
    /// </summary>
    [DataContract]
    public class ParameterAllowableValues
    {
        [DataMember(Name = "type")]
        public string ValueType { get; set; }
    }

    [DefaultRequest(typeof(ResourceRequest))]
    public class SwaggerApiService : ServiceInterface.Service
    {
        internal static bool UseCamelCaseModelPropertyNames { get; set; }
        internal static bool UseLowercaseUnderscoreModelPropertyNames { get; set; }
        internal static bool DisableAutoDtoInBodyParam { get; set; }

        private readonly Regex nicknameCleanerRegex = new Regex(@"[\{\}\*\-_/]*", RegexOptions.Compiled);

        public object Get(ResourceRequest request)
        {
            var httpReq = RequestContext.Get<IHttpRequest>();
            var path = "/" + request.Name;
            var map = EndpointHost.ServiceManager.ServiceController.RestPathMap;
            var paths = new List<RestPath>();

            var basePath = EndpointHost.Config.WebHostUrl;
            if (basePath == null)
            {
                basePath = EndpointHost.Config.UseHttpsLinks
                    ? Common.StringExtensions.ToHttps(httpReq.GetParentPathUrl())
                    : httpReq.GetParentPathUrl();
            }

            if (basePath.EndsWith(SwaggerResourcesService.RESOURCE_PATH, StringComparison.OrdinalIgnoreCase))
            {
                basePath = basePath.Substring(0, basePath.LastIndexOf(SwaggerResourcesService.RESOURCE_PATH, StringComparison.OrdinalIgnoreCase));
            }
            var meta = EndpointHost.Metadata;
            foreach (var key in map.Keys)
            {
                paths.AddRange(map[key].Where(x => (x.Path == path || x.Path.StartsWith(path + "/") && meta.IsVisible(Request, Format.Json, x.RequestType.Name))));
            }

            var models = new Dictionary<string, SwaggerModel>();
            foreach (var restPath in paths)
            {
                ParseModel(models, restPath.RequestType);
            }

            return new ResourceResponse
            {
                ResourcePath = path,
                BasePath = basePath,
                Apis = new List<MethodDescription>(paths.Select(p => FormateMethodDescription(p, models)).ToArray().OrderBy(md => md.Path)),
                Models = models
            };
        }

        private static readonly Dictionary<Type, string> ClrTypesToSwaggerScalarTypes = new Dictionary<Type, string> {
            {typeof(byte), SwaggerType.Byte},
            {typeof(sbyte), SwaggerType.Byte},
            {typeof(bool), SwaggerType.Boolean},
            {typeof(short), SwaggerType.Int},
            {typeof(ushort), SwaggerType.Int},
            {typeof(int), SwaggerType.Int},
            {typeof(uint), SwaggerType.Int},
            {typeof(long), SwaggerType.Long},
            {typeof(ulong), SwaggerType.Long},
            {typeof(float), SwaggerType.Float},
            {typeof(double), SwaggerType.Double},
            {typeof(decimal), SwaggerType.Double},
            {typeof(string), SwaggerType.String},
            {typeof(DateTime), SwaggerType.Date}
        };

        private static bool IsSwaggerScalarType(Type type)
        {
            return ClrTypesToSwaggerScalarTypes.ContainsKey(type) || (Nullable.GetUnderlyingType(type) ?? type).IsEnum;
        }

        private static string GetSwaggerTypeName(Type type)
        {
            var lookupType = Nullable.GetUnderlyingType(type) ?? type;

            return ClrTypesToSwaggerScalarTypes.ContainsKey(lookupType)
                ? ClrTypesToSwaggerScalarTypes[lookupType]
                : lookupType.Name;
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

        private static bool IsNullable(Type type)
        {
            return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);
        }

        private static void ParseModel(IDictionary<string, SwaggerModel> models, Type modelType)
        {
            if (IsSwaggerScalarType(modelType)) return;

            var modelId = modelType.Name;
            if (models.ContainsKey(modelId)) return;

            var model = new SwaggerModel
            {
                Id = modelId,
                Properties = new Dictionary<string, ModelProperty>()
            };
            models[model.Id] = model;

            var hasDataContract = modelType.HasAttr<DataContractAttribute>();
            
            foreach (var prop in modelType.GetProperties())
            {
                DataMemberAttribute dataMemberAttribute = null;
                if (hasDataContract)
                {
                    dataMemberAttribute = prop.GetDataMember();
                    if (dataMemberAttribute == null)
                    {
                        continue;
                    }
                } 
                else if (prop.IsDefined(typeof(IgnoreDataMemberAttribute), true))
                {
                    continue;
                }

                var allApiDocAttributes = prop
                    .GetCustomAttributes(typeof(ApiMemberAttribute), true)
                    .OfType<ApiMemberAttribute>()
                    .Where(attr => prop.Name.Equals(attr.Name, StringComparison.InvariantCultureIgnoreCase))
                    .ToList();
                var apiDoc = allApiDocAttributes.FirstOrDefault(attr => attr.ParameterType == "body");

                if (allApiDocAttributes.Any() && apiDoc == null) continue;

                var propertyType = prop.PropertyType;
                
                var isRequired = dataMemberAttribute == null
                    ? !IsNullable(propertyType)
                    : dataMemberAttribute.IsRequired;

                var modelProp = new ModelProperty { Type = GetSwaggerTypeName(propertyType), Required = isRequired };

                if (IsListType(propertyType))
                {
                    modelProp.Type = SwaggerType.Array;
                    var listItemType = GetListElementType(propertyType);
                    modelProp.Items = new Dictionary<string, string> {
                        { IsSwaggerScalarType(listItemType) ? "type" : "$ref", GetSwaggerTypeName(listItemType) }
                    };
                    ParseModel(models, listItemType);
                }
                else if ((Nullable.GetUnderlyingType(propertyType) ?? propertyType).IsEnum)
                {
                    var enumType = Nullable.GetUnderlyingType(propertyType) ?? propertyType;
                    if (enumType.IsNumericType())
                    {
                        var underlyingType = Enum.GetUnderlyingType(enumType);
                        modelProp.Type = GetSwaggerTypeName(underlyingType);
                        modelProp.AllowedValues = GetNumericValues(enumType, underlyingType).ToArray();
                    }
                    else
                    {
                        modelProp.Type = SwaggerType.String;
                        modelProp.AllowedValues = Enum.GetNames(enumType);
                    }                 
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
                {
                    modelProp.AllowableValues = allowableValues;
                }

                model.Properties[GetModelPropertyName(prop, dataMemberAttribute)] = modelProp;
            }
        }

        private static IEnumerable<string> GetNumericValues(Type propertyType, Type underlyingType)
        {
            var values = Enum.GetValues(propertyType);
            foreach (var value in values)
            {
                yield return string.Format("{0} ({1})", Convert.ChangeType(value, underlyingType), value);                
            }            
        }

        private static string GetModelPropertyName(PropertyInfo prop, DataMemberAttribute dataMemberAttribute)
        {
            var name = dataMemberAttribute == null ? prop.Name : (dataMemberAttribute.Name ?? prop.Name);

            return UseCamelCaseModelPropertyNames
                ? (UseLowercaseUnderscoreModelPropertyNames ? name.ToLowercaseUnderscore() : name.ToCamelCase())
                : name;
        }

        private static string GetResponseClass(IRestPath restPath, IDictionary<string, SwaggerModel> models)
        {
            // Given: class MyDto : IReturn<X>. Determine the type X.
            foreach (var i in restPath.RequestType.GetInterfaces())
            {
                if (i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IReturn<>))
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

        private static List<ResponseMessage> GetMethodResponseCodes(Type requestType)
        {
            return requestType
                .GetCustomAttributes(typeof(IApiResponseDescription), true)
                .OfType<IApiResponseDescription>()
                .Select(x => new ResponseMessage
                {
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

            var md = new MethodDescription
            {
                Path = restPath.Path,
                Description = summary,
                Operations = verbs.Select(verb =>
                    new MethodOperation
                    {
                        HttpMethod = verb,
                        Nickname = verb.ToLowerInvariant() + nickName,
                        Summary = summary,
                        Notes = notes,
                        Parameters = ParseParameters(verb, restPath.RequestType, models),
                        ResponseClass = GetResponseClass(restPath, models),
                        ResponseMessages = GetMethodResponseCodes(restPath.RequestType)
                    }).ToList()
            };
            return md;
        }

        private static List<MethodOperationParameter> ParseParameters(string verb, Type operationType, IDictionary<string, SwaggerModel> models)
        {
            var hasDataContract = operationType.GetCustomAttributes(typeof(DataContractAttribute), inherit: true).Length > 0;

            var properties = operationType.GetProperties();
            var paramAttrs = new Dictionary<string, ApiMemberAttribute[]>();
            var allowableParams = new List<ApiAllowableValuesAttribute>();

            foreach (var property in properties)
            {
                var propertyName = property.Name;
                if (hasDataContract)
                {
                    var dataMemberAttr = property.GetCustomAttributes(typeof(DataMemberAttribute), inherit: true)
                        .FirstOrDefault() as DataMemberAttribute;
                    if (dataMemberAttr != null && dataMemberAttr.Name != null)
                    {
                        propertyName = dataMemberAttr.Name;
                    }
                }
                paramAttrs[propertyName] = (ApiMemberAttribute[])property.GetCustomAttributes(typeof(ApiMemberAttribute), true);
                allowableParams.AddRange(property.GetCustomAttributes(typeof(ApiAllowableValuesAttribute), true).Cast<ApiAllowableValuesAttribute>().ToArray());
            }

            var methodOperationParameters = new List<MethodOperationParameter>();
            foreach (var key in paramAttrs.Keys)
            {
                var value = paramAttrs[key];
                methodOperationParameters.AddRange(
                    from ApiMemberAttribute member in value
                    where member.Verb == null || string.Compare(member.Verb, verb, StringComparison.InvariantCultureIgnoreCase) == 0
                    select new MethodOperationParameter
                    {
                        DataType = member.DataType,
                        AllowMultiple = member.AllowMultiple,
                        Description = member.Description,
                        Name = member.Name ?? key,
                        ParamType = member.ParameterType,
                        Required = member.IsRequired,
                        AllowableValues = allowableParams.FirstOrDefault(attr => attr.Name == member.Name)
                    });
            }

            if (!DisableAutoDtoInBodyParam)
            {
                if (!ServiceStack.Common.Web.HttpMethods.Get.Equals(verb, StringComparison.OrdinalIgnoreCase) && !methodOperationParameters.Any(p => p.ParamType.Equals("body", StringComparison.OrdinalIgnoreCase)))
                {
                    ParseModel(models, operationType);
                    var param = new MethodOperationParameter()
                    {
                        DataType = GetSwaggerTypeName(operationType),
                        ParamType = "body"
                    };
                    param.Name = param.DataType;
                    methodOperationParameters.Add(param);
                }
            }
            return methodOperationParameters;
        }

    }
}