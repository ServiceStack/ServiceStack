using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using ServiceStack.Host;
using ServiceStack.Web;

namespace ServiceStack.Api.Swagger
{
    using ServiceStack.Api.Swagger.Support;

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
        [DataMember(Name = "description")]
        public string Description { get; set; }
        [DataMember(Name = "properties")]
        public OrderedDictionary<string, ModelProperty> Properties { get; set; }
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
        [DataMember(Name = "required")]
        public bool Required { get; set; }
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

    [AddHeader(DefaultContentType = MimeTypes.Json)]
    [DefaultRequest(typeof(ResourceRequest))]
    [Restrict(VisibilityTo = RequestAttributes.None)]
    public class SwaggerApiService : Service
    {
        internal static bool UseCamelCaseModelPropertyNames { get; set; }
        internal static bool UseLowercaseUnderscoreModelPropertyNames { get; set; }
        internal static bool DisableAutoDtoInBodyParam { get; set; }

        internal static Action<SwaggerModel> ModelFilter { get; set; }
        internal static Action<ModelProperty> ModelPropertyFilter { get; set; }

        private readonly Regex nicknameCleanerRegex = new Regex(@"[\{\}\*\-_/]*", RegexOptions.Compiled);

        public object Get(ResourceRequest request)
        {
            var httpReq = Request;
            var path = "/" + request.Name;
            var map = HostContext.ServiceController.RestPathMap;
            var paths = new List<RestPath>();

            var basePath = HostContext.Config.WebHostUrl 
                ?? httpReq.GetParentPathUrl().NormalizeScheme();

            if (basePath.EndsWith(SwaggerResourcesService.RESOURCE_PATH, StringComparison.OrdinalIgnoreCase))
            {
                basePath = basePath.Substring(0, basePath.LastIndexOf(SwaggerResourcesService.RESOURCE_PATH, StringComparison.OrdinalIgnoreCase));
            }
            var meta = HostContext.Metadata;
            foreach (var key in map.Keys)
            {
                paths.AddRange(map[key].Where(x => (x.Path == path || x.Path.StartsWith(path + "/") && meta.IsVisible(Request, Format.Json, x.RequestType.Name))));
            }

            var models = new Dictionary<string, SwaggerModel>();
            foreach (var restPath in paths.SelectMany(x => x.Verbs.Select(y => new {Value = x, Verb = y})))
            {
                ParseModel(models, restPath.Value.RequestType, restPath.Value.Path, restPath.Verb);
            }

            var apis = paths.Select(p => FormateMethodDescription(p, models))
                .ToArray().OrderBy(md => md.Path).ToList();

            return new ResourceResponse
            {
                ApiVersion = HostContext.Config.ApiVersion,
                ResourcePath = path,
                BasePath = basePath,
                Apis = apis,
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

        private static string GetSwaggerTypeName(Type type, string route = null, string verb = null)
        {
            var lookupType = Nullable.GetUnderlyingType(type) ?? type;

            return ClrTypesToSwaggerScalarTypes.ContainsKey(lookupType)
                ? ClrTypesToSwaggerScalarTypes[lookupType]
                : GetModelTypeName(lookupType, route, verb);
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

		private static string GetModelTypeName(Type modelType, string path = null, string verb = null)
		{
		    verb = string.IsNullOrEmpty(verb) ? "" : verb + "_";
		    if (!modelType.IsGenericType)
		        return verb + modelType.Name + (path ?? "");

			var modelTypeName = modelType.FullName.Replace("`1[[", "`").Replace(modelType.Namespace + ".", "");
			var index = modelTypeName.IndexOf(",", StringComparison.Ordinal);
			var genericNamespace = modelType.GenericTypeArguments()[0].Namespace + ".";

		    return verb + modelTypeName.Substring(0, index).Replace(genericNamespace, "") + "`" + (path ?? "");
		}

        private void ParseResponseModel(IDictionary<string, SwaggerModel> models, Type modelType)
        {
            ParseModel(models, modelType, null, null);
        }

        private void ParseModel(IDictionary<string, SwaggerModel> models, Type modelType, string route, string verb)
        {
            if (IsSwaggerScalarType(modelType)) return;

            var modelId = GetModelTypeName(modelType, route, verb);
            if (models.ContainsKey(modelId)) return;

            var modelTypeName = GetModelTypeName(modelType);
            var model = new SwaggerModel
            {
                Id = modelId,
                Description = modelTypeName,
                Properties = new OrderedDictionary<string, ModelProperty>()
            };
            models[model.Id] = model;
            models[modelTypeName] = new SwaggerModel
            {
                Id = modelTypeName,
                Description = modelTypeName,
                Properties = model.Properties,
            };

            var properties = modelType.GetProperties();

            // Order model properties by DataMember.Order if [DataContract] and [DataMember](s) defined
            // Ordering defined by: http://msdn.microsoft.com/en-us/library/ms729813.aspx
            var dataContractAttr = modelType.GetCustomAttributes(typeof(DataContractAttribute), true).OfType<DataContractAttribute>().FirstOrDefault();
            if (dataContractAttr != null && properties.Any(prop => prop.IsDefined(typeof(DataMemberAttribute), true)))
            {
                var typeOrder = new List<Type> { modelType };
                var baseType = modelType.BaseType;
                while (baseType != null)
                {
                    typeOrder.Add(baseType);
                    baseType = baseType.BaseType;
                }              
                
                var propsWithDataMember = properties.Where(prop => prop.IsDefined(typeof(DataMemberAttribute), true));
                var propDataMemberAttrs = properties.ToDictionary(prop => prop, prop => prop.FirstAttribute<DataMemberAttribute>());

                properties = propsWithDataMember
                    .OrderBy(prop => propDataMemberAttrs[prop].Order)                // Order by DataMember.Order
                    .ThenByDescending(prop => typeOrder.IndexOf(prop.DeclaringType)) // Then by BaseTypes First
                    .ThenBy(prop =>                                                  // Then by [DataMember].Name / prop.Name
                    {
                        var name = propDataMemberAttrs[prop].Name;
                        return name.IsNullOrEmpty() ? prop.Name : name;
                    }).ToArray();
            }

            foreach (var prop in properties)
            {
                var allApiDocAttributes = prop
                    .AllAttributes<ApiMemberAttribute>()
                    .Where(attr => prop.Name.Equals(attr.Name, StringComparison.InvariantCultureIgnoreCase))
                    .OrderByDescending(attr => attr.Route)
                    .ToList();
                var apiDoc = allApiDocAttributes
                    .Where(attr => string.IsNullOrEmpty(verb) || string.IsNullOrEmpty(attr.Verb) || (verb ?? "").Equals(attr.Verb))
                    .Where(attr => string.IsNullOrEmpty(route) || string.IsNullOrEmpty(attr.Route) || (route ?? "").StartsWith(attr.Route))
                    .FirstOrDefault(attr => attr.ParameterType == "body" || attr.ParameterType == "model");

                if (allApiDocAttributes.Any(x => !string.IsNullOrEmpty(x.Verb) 
                    || !string.IsNullOrEmpty(x.Route)) 
                    && apiDoc == null) 
                    continue;

                var propertyType = prop.PropertyType;
                var modelProp = new ModelProperty { Type = GetSwaggerTypeName(propertyType, route, verb), Required = !IsNullable(propertyType) };

                if (IsListType(propertyType))
                {
                    modelProp.Type = SwaggerType.Array;
                    var listItemType = GetListElementType(propertyType);
                    modelProp.Items = new Dictionary<string, string> {
                        { IsSwaggerScalarType(listItemType) ? "type" : "$ref", GetSwaggerTypeName(listItemType, route, verb) }
                    };
                    ParseModel(models, listItemType, route, verb);
                }
                else if ((Nullable.GetUnderlyingType(propertyType) ?? propertyType).IsEnum)
                {
                    var enumType = Nullable.GetUnderlyingType(propertyType) ?? propertyType;
                    if (enumType.IsNumericType())
                    {
                        var underlyingType = Enum.GetUnderlyingType(enumType);
                        modelProp.Type = GetSwaggerTypeName(underlyingType, route, verb);
                        modelProp.AllowableValues = new ParameterAllowableValues
                        {
                            Values = GetNumericValues(enumType, underlyingType).ToArray(),
                            ValueType = "LIST"
                        };
                    }
                    else
                    {
                        modelProp.Type = SwaggerType.String;
                        modelProp.AllowableValues = new ParameterAllowableValues
                        {
                            Values = Enum.GetNames(enumType),
                            ValueType = "LIST"
                        };
                    } 
                }
                else
                {
                    ParseModel(models, propertyType, route, verb);
                }

                modelProp.Description = prop.GetDescription();

                if (apiDoc != null && modelProp.Description == null)
                    modelProp.Description = apiDoc.Description;

                var allowableValues = prop.FirstAttribute<ApiAllowableValuesAttribute>();
                if (allowableValues != null)
                    modelProp.AllowableValues = GetAllowableValue(allowableValues);

                if (ModelPropertyFilter != null)
                {
                    ModelPropertyFilter(modelProp);
                }

                model.Properties[GetModelPropertyName(prop)] = modelProp;
            }

            if (ModelFilter != null)
            {
                ModelFilter(model);
            }
        }

        private static string GetModelPropertyName(PropertyInfo prop)
        {
            var dataMemberAttr = prop.FirstAttribute<DataMemberAttribute>();
            if (dataMemberAttr != null && !dataMemberAttr.Name.IsNullOrEmpty()) 
                return dataMemberAttr.Name;
            
            return UseCamelCaseModelPropertyNames
                ? (UseLowercaseUnderscoreModelPropertyNames ? prop.Name.ToLowercaseUnderscore() : prop.Name.ToCamelCase())
                : prop.Name;
        }

        private static IEnumerable<string> GetNumericValues(Type propertyType, Type underlyingType)
        {
            var values = Enum.GetValues(propertyType)
                .Map(x => "{0} ({1})".Fmt(Convert.ChangeType(x, underlyingType), x));

            return values;
        }

        private string GetResponseClass(IRestPath restPath, IDictionary<string, SwaggerModel> models)
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
                        ParseResponseModel(models, listItemType);
                        return string.Format("List[{0}]", GetSwaggerTypeName(listItemType));
                    }
                    ParseResponseModel(models, returnType);
                    return GetSwaggerTypeName(i.GetGenericArguments()[0]);
                }
            }

            return null;
        }

        private static List<ErrorResponseStatus> GetMethodResponseCodes(Type requestType)
        {
            return requestType
                .AllAttributes<IApiResponseDescription>()
                .Select(x => new ErrorResponseStatus {
                    StatusCode = x.StatusCode,
                    Reason = x.Description
                })
                .OrderBy(x => x.StatusCode)
                .ToList();
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
                        Parameters = ParseParameters(verb, restPath.RequestType, models, restPath.Path),
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
                return new ParameterAllowableValues
                {
                    ValueType = attr.Type,
                    Values = attr.Values,
                    Max = attr.Max,
                    Min = attr.Min
                };
            }
            return null;
        }

        private List<MethodOperationParameter> ParseParameters(string verb, Type operationType, IDictionary<string, SwaggerModel> models, string route)
        {
            var hasDataContract = operationType.HasAttribute<DataContractAttribute>();

            var properties = operationType.GetProperties();
            var paramAttrs = new Dictionary<string, ApiMemberAttribute[]>();
            var allowableParams = new List<ApiAllowableValuesAttribute>();

            foreach (var property in properties)
            {
                var propertyName = property.Name;
                if (hasDataContract)
                {
                    var dataMemberAttr = property.FirstAttribute<DataMemberAttribute>();
                    if (dataMemberAttr != null && dataMemberAttr.Name != null)
                    {
                        propertyName = dataMemberAttr.Name;
                    }
                }
                paramAttrs[propertyName] = property.AllAttributes<ApiMemberAttribute>();
                allowableParams.AddRange(property.AllAttributes<ApiAllowableValuesAttribute>());
            }

            var methodOperationParameters = new List<MethodOperationParameter>();
            foreach (var key in paramAttrs.Keys)
            {
                var value = paramAttrs[key];
                methodOperationParameters.AddRange(
                    from ApiMemberAttribute member in value
                    where member.Verb == null || string.Compare(member.Verb, verb, StringComparison.InvariantCultureIgnoreCase) == 0
                    where member.Route == null || (route ?? "").StartsWith(member.Route)
                    where !string.Equals(member.ParameterType, "model") 
                    select new MethodOperationParameter
                    {
                        DataType = member.DataType ?? SwaggerType.String,
                        AllowMultiple = member.AllowMultiple,
                        Description = member.Description,
                        Name = member.Name ?? key,
                        ParamType = member.GetParamType(operationType, member.Verb ?? verb),
                        Required = member.IsRequired,
                        AllowableValues = GetAllowableValue(allowableParams.FirstOrDefault(attr => attr.Name == member.Name))
                    });
            }

            if (!DisableAutoDtoInBodyParam)
            {
                if (!HttpMethods.Get.Equals(verb, StringComparison.OrdinalIgnoreCase) 
                    && !methodOperationParameters.Any(p => "body".EqualsIgnoreCase(p.ParamType)))
                {
                    ParseModel(models, operationType, route, verb);
                    methodOperationParameters.Add(new MethodOperationParameter
                    {
                        DataType = GetSwaggerTypeName(operationType, route, verb),
                        ParamType = "body",
                        Name = GetSwaggerTypeName(operationType)
                    });
                }
            }
            return methodOperationParameters;
        }

    }
}