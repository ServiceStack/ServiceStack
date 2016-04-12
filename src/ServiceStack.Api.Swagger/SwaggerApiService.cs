using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using ServiceStack.DataAnnotations;
using ServiceStack.Host;
using ServiceStack.NativeTypes;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack.Api.Swagger
{
    using ServiceStack.Api.Swagger.Support;

    [DataContract]
    [Exclude(Feature.Soap)]
    public class SwaggerResource : IReturn<SwaggerApiDeclaration>
    {
        [DataMember(Name = "apiKey")]
        public string ApiKey { get; set; }
        [DataMember(Name = "name")]
        public string Name { get; set; }
    }

    [DataContract]
    public class SwaggerApiDeclaration
    {
        [DataMember(Name = "swaggerVersion")]
        public string SwaggerVersion
        {
            get { return "1.2"; }
        }
        [DataMember(Name = "apiVersion")]
        public string ApiVersion { get; set; }
        [DataMember(Name = "basePath")]
        public string BasePath { get; set; }
        [DataMember(Name = "resourcePath")]
        public string ResourcePath { get; set; }
        [DataMember(Name = "apis")]
        public List<SwaggerApi> Apis { get; set; }
        [DataMember(Name = "models")]
        public Dictionary<string, SwaggerModel> Models { get; set; }
        [DataMember(Name = "produces")]
        public List<string> Produces { get; set; }
        [DataMember(Name = "consumes")]
        public List<string> Consumes { get; set; }
    }

    [DataContract]
    public class SwaggerModel
    {
        [DataMember(Name = "id")]
        public string Id { get; set; }
        [DataMember(Name = "description")]
        public string Description { get; set; }
        [DataMember(Name = "required")]
        public List<string> Required { get; set; }
        [DataMember(Name = "properties")]
        public OrderedDictionary<string, SwaggerProperty> Properties { get; set; }
        [DataMember(Name = "subTypes")]
        public List<string> SubTypes { get; set; }
        [DataMember(Name = "discriminator")]
        public string Discriminator { get; set; }
    }

    [DataContract]
    public class SwaggerApi
    {
        [DataMember(Name = "path")]
        public string Path { get; set; }
        [DataMember(Name = "description")]
        public string Description { get; set; }
        [DataMember(Name = "operations")]
        public List<SwaggerOperation> Operations { get; set; }
    }

    [DataContract]
    public class SwaggerOperation
    {
        [DataMember(Name = "method")]
        public string Method { get; set; }
        [DataMember(Name = "summary")]
        public string Summary { get; set; }
        [DataMember(Name = "notes")]
        public string Notes { get; set; }
        [DataMember(Name = "nickname")]
        public string Nickname { get; set; }
        [DataMember(Name = "parameters")]
        public List<SwaggerParameter> Parameters { get; set; }
        [DataMember(Name = "responseMessages")]
        public List<SwaggerResponseMessage> ResponseMessages { get; set; }
        [DataMember(Name = "produces")]
        public List<string> Produces { get; set; }
        [DataMember(Name = "consumes")]
        public List<string> Consumes { get; set; }
        [DataMember(Name = "deprecated")]
        public string Deprecated { get; set; }
        [DataMember(Name = "responseClass")]
        public string ResponseClass { get; set; }
        [DataMember(Name = "errorResponses")]
        public List<ErrorResponseStatus> ErrorResponses { get; set; }
    }

    [DataContract]
    public class SwaggerResponseMessage
    {
        [DataMember(Name = "code")]
        public int Code { get; set; }
        [DataMember(Name = "message")]
        public string Message { get; set; }
        [DataMember(Name = "responseModel")]
        public string ResponseModel { get; set; }
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
    public abstract class SwaggerDataTypeFields
    {
        [DataMember(Name = "type")]
        public string Type { get; set; }
        [DataMember(Name = "format")]
        public string Format { get; set; }
        [DataMember(Name = "defaultValue")]
        public string DefaultValue { get; set; }
        [DataMember(Name = "enum")]
        public List<string> Enum { get; set; }
        [DataMember(Name = "minimum")]
        public string Minimum { get; set; }
        [DataMember(Name = "maximum")]
        public string Maximum { get; set; }
        [DataMember(Name = "items")]
        public Dictionary<string, string> Items { get; set; }
        [DataMember(Name = "uniqueItems")]
        public bool? UniqueItems { get; set; }
    }

    [DataContract]
    public class SwaggerProperty : SwaggerDataTypeFields
    {
        [DataMember(Name = "description")]
        public string Description { get; set; }
    }

    [DataContract]
    public class SwaggerParameter : SwaggerDataTypeFields
    {
        [DataMember(Name = "paramType")]
        public string ParamType { get; set; }
        [DataMember(Name = "name")]
        public string Name { get; set; }
        [DataMember(Name = "description")]
        public string Description { get; set; }
        [DataMember(Name = "required")]
        public bool Required { get; set; }
        [DataMember(Name = "allowMultiple")]
        public bool AllowMultiple { get; set; }
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
    [DefaultRequest(typeof(SwaggerResource))]
    [Restrict(VisibilityTo = RequestAttributes.None)]
    public class SwaggerApiService : Service
    {
        internal static bool UseCamelCaseModelPropertyNames { get; set; }
        internal static bool UseLowercaseUnderscoreModelPropertyNames { get; set; }
        internal static bool DisableAutoDtoInBodyParam { get; set; }

        internal static Action<SwaggerApiDeclaration> ApiDeclarationFilter { get; set; }
        internal static Action<SwaggerOperation> OperationFilter { get; set; }
        internal static Action<SwaggerModel> ModelFilter { get; set; }
        internal static Action<SwaggerProperty> ModelPropertyFilter { get; set; }

        public object Get(SwaggerResource request)
        {
            var path = "/" + request.Name;
            var map = HostContext.ServiceController.RestPathMap;
            var paths = new List<RestPath>();

            var basePath = base.Request.ResolveBaseUrl();

            var meta = HostContext.Metadata;
            foreach (var key in map.Keys)
            {
                var restPaths = map[key];
                var selectedPaths = restPaths.Where( x => x.Path == path || x.Path.StartsWith(path + "/"));
                var visiblePaths = selectedPaths.Where(x => meta.IsVisible(Request, Format.Json, x.RequestType.Name));
                paths.AddRange(visiblePaths);
            }

            var models = new Dictionary<string, SwaggerModel>();
            foreach (var restPath in paths.SelectMany(x => x.Verbs.Select(y => new {Value = x, Verb = y})))
            {
                ParseModel(models, restPath.Value.RequestType, restPath.Value.Path, restPath.Verb);
            }

            var apis = paths.Select(p => FormatMethodDescription(p, models))
                .ToArray().OrderBy(md => md.Path).ToList();

            var result = new SwaggerApiDeclaration
            {
                ApiVersion = HostContext.Config.ApiVersion,
                ResourcePath = path,
                BasePath = basePath,
                Apis = apis,
                Models = models
            };

            if (OperationFilter != null)
                apis.Each(x => x.Operations.Each(OperationFilter));

            if (ApiDeclarationFilter != null)
                ApiDeclarationFilter(result);

            return new HttpResult(result)
            {
                ResultScope = () => JsConfig.With(includeNullValues: false)
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
            return ClrTypesToSwaggerScalarTypes.ContainsKey(type) 
                || (Nullable.GetUnderlyingType(type) ?? type).IsEnum
                || type.IsValueType
                || type.IsNullableType();
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
		    if (modelType.IsValueType || modelType.IsNullableType())
		        return SwaggerType.String;

		    if (!modelType.IsGenericType)
		        return modelType.Name;

            var typeName = modelType.ToPrettyName();
		    return typeName;
		}

        private void ParseResponseModel(IDictionary<string, SwaggerModel> models, Type modelType)
        {
            ParseModel(models, modelType, null, null);
        }

        private void ParseModel(IDictionary<string, SwaggerModel> models, Type modelType, string route, string verb)
        {
            if (IsSwaggerScalarType(modelType) || modelType.ExcludesFeature(Feature.Metadata)) return;

            var modelId = GetModelTypeName(modelType, route, verb);
            if (models.ContainsKey(modelId)) return;

            var modelTypeName = GetModelTypeName(modelType);
            var model = new SwaggerModel
            {
                Id = modelId,
                Description = modelType.GetDescription() ?? modelTypeName,
                Properties = new OrderedDictionary<string, SwaggerProperty>()
            };
            models[model.Id] = model;

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

            var parseProperties = modelType.IsUserType();
            if (parseProperties)
            {
                foreach (var prop in properties)
                {
                    if (prop.HasAttribute<IgnoreDataMemberAttribute>())
                        continue;

                    var apiMembers = prop
                        .AllAttributes<ApiMemberAttribute>()
                        .OrderByDescending(attr => attr.Route)
                        .ToList();
                    var apiDoc = apiMembers
                        .Where(attr => string.IsNullOrEmpty(verb) || string.IsNullOrEmpty(attr.Verb) || (verb ?? "").Equals(attr.Verb))
                        .Where(attr => string.IsNullOrEmpty(route) || string.IsNullOrEmpty(attr.Route) || (route ?? "").StartsWith(attr.Route))
                        .FirstOrDefault(attr => attr.ParameterType == "body" || attr.ParameterType == "model");

                    if (apiMembers.Any(x => x.ExcludeInSchema))
                        continue;

                    var propertyType = prop.PropertyType;
                    var modelProp = new SwaggerProperty
                    {
                        Type = GetSwaggerTypeName(propertyType, route, verb),
                        Description = prop.GetDescription(),
                    };

                    if ((propertyType.IsValueType && !IsNullable(propertyType)) || apiMembers.Any(x => x.IsRequired))
                    {
                        if (model.Required == null)
                            model.Required = new List<string>();

                        model.Required.Add(prop.Name);
                    }

                    if (IsListType(propertyType))
                    {
                        modelProp.Type = SwaggerType.Array;
                        var listItemType = GetListElementType(propertyType);
                        modelProp.Items = new Dictionary<string, string> {
                            { IsSwaggerScalarType(listItemType) 
                                ? "type" 
                                : "$ref", GetSwaggerTypeName(listItemType, route, verb) }
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
                            modelProp.Enum = GetNumericValues(enumType, underlyingType).ToList();
                        }
                        else
                        {
                            modelProp.Type = SwaggerType.String;
                            modelProp.Enum = Enum.GetNames(enumType).ToList();
                        }
                    }
                    else
                    {
                        ParseModel(models, propertyType, route, verb);

                        var propAttr = prop.FirstAttribute<ApiMemberAttribute>();
                        if (propAttr != null && propAttr.DataType != null)
                            modelProp.Type = propAttr.DataType;
                    }

                    if (apiDoc != null && modelProp.Description == null)
                        modelProp.Description = apiDoc.Description;

                    var allowableValues = prop.FirstAttribute<ApiAllowableValuesAttribute>();
                    if (allowableValues != null)
                        modelProp.Enum = GetEnumValues(allowableValues);

                    if (ModelPropertyFilter != null)
                    {
                        ModelPropertyFilter(modelProp);
                    }

                    model.Properties[GetModelPropertyName(prop)] = modelProp;
                }
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

        private SwaggerApi FormatMethodDescription(RestPath restPath, Dictionary<string, SwaggerModel> models)
        {
            var verbs = new List<string>();
            var summary = restPath.Summary ?? restPath.RequestType.GetDescription();
            var notes = restPath.Notes;

            verbs.AddRange(restPath.AllowsAllVerbs
                ? new[] {"GET", "POST", "PUT", "DELETE"}
                : restPath.AllowedVerbs.Split(new[] {',', ' '}, StringSplitOptions.RemoveEmptyEntries));

            var routePath = restPath.Path.Replace("*","");
            var requestType = restPath.RequestType;

            var md = new SwaggerApi
            {
                Path = routePath,
                Description = summary,
                Operations = verbs.Map(verb => new SwaggerOperation
                {
                    Method = verb,
                    Nickname = requestType.Name,
                    Summary = summary,
                    Notes = notes,
                    Parameters = ParseParameters(verb, requestType, models, routePath),
                    ResponseClass = GetResponseClass(restPath, models),
                    ErrorResponses = GetMethodResponseCodes(requestType)
                })
            };
            return md;
        }

        private static List<string> GetEnumValues(ApiAllowableValuesAttribute attr)
        {
            return attr != null && attr.Values != null ? attr.Values.ToList() : null;
        }

        private List<SwaggerParameter> ParseParameters(string verb, Type operationType, IDictionary<string, SwaggerModel> models, string route)
        {
            var hasDataContract = operationType.HasAttribute<DataContractAttribute>();

            var properties = operationType.GetProperties();
            var paramAttrs = new Dictionary<string, ApiMemberAttribute[]>();
            var allowableParams = new List<ApiAllowableValuesAttribute>();
            var defaultOperationParameters = new List<SwaggerParameter>();

            var hasApiMembers = false;

            foreach (var property in properties)
            {
                if (property.HasAttribute<IgnoreDataMemberAttribute>())
                    continue;

                var attr = hasDataContract
                    ? property.FirstAttribute<DataMemberAttribute>()
                    : null;
                
                var propertyName = attr != null && attr.Name != null
                    ? attr.Name
                    : property.Name;

                var apiMembers = property.AllAttributes<ApiMemberAttribute>();
                if (apiMembers.Length > 0)
                    hasApiMembers = true;

                paramAttrs[propertyName] = apiMembers;
                var allowableValuesAttrs = property.AllAttributes<ApiAllowableValuesAttribute>();
                allowableParams.AddRange(allowableValuesAttrs);

                if (hasDataContract && attr == null)
                    continue;

                var inPath = (route ?? "").ToLower().Contains("{" + propertyName.ToLower() + "}");
                var paramType = inPath
                    ? "path" 
                    : verb == HttpMethods.Post || verb == HttpMethods.Put 
                        ? "form" 
                        : "query";

                defaultOperationParameters.Add(new SwaggerParameter {
                    Type = GetSwaggerTypeName(property.PropertyType),
                    AllowMultiple = false,
                    Description = property.PropertyType.GetDescription(),
                    Name = propertyName,
                    ParamType = paramType,
                    Required = paramType == "path",
                    Enum = GetEnumValues(allowableValuesAttrs.FirstOrDefault()),
                });
            }

            var methodOperationParameters = defaultOperationParameters;
            if (hasApiMembers)
            {
                methodOperationParameters = new List<SwaggerParameter>();
                foreach (var key in paramAttrs.Keys)
                {
                    var apiMembers = paramAttrs[key];
                    foreach (var member in apiMembers)
                    {
                        if ((member.Verb == null || string.Compare(member.Verb, verb, StringComparison.InvariantCultureIgnoreCase) == 0)
                            && (member.Route == null || (route ?? "").StartsWith(member.Route))
                            && !string.Equals(member.ParameterType, "model")
                            && methodOperationParameters.All(x => x.Name != (member.Name ?? key)))
                        {
                            methodOperationParameters.Add(new SwaggerParameter
                            {
                                Type = member.DataType ?? SwaggerType.String,
                                AllowMultiple = member.AllowMultiple,
                                Description = member.Description,
                                Name = member.Name ?? key,
                                ParamType = member.GetParamType(operationType, member.Verb ?? verb),
                                Required = member.IsRequired,
                                Enum = GetEnumValues(allowableParams.FirstOrDefault(attr => attr.Name == (member.Name ?? key)))
                            });
                        }
                    }
                }
            }

            if (!DisableAutoDtoInBodyParam)
            {
                if (!HttpMethods.Get.EqualsIgnoreCase(verb) && !HttpMethods.Delete.EqualsIgnoreCase(verb) 
                    && !methodOperationParameters.Any(p => "body".EqualsIgnoreCase(p.ParamType)))
                {
                    ParseModel(models, operationType, route, verb);
                    methodOperationParameters.Add(new SwaggerParameter
                    {
                        ParamType = "body",
                        Name = "body",
                        Type = GetSwaggerTypeName(operationType, route, verb),
                    });
                }
            }
            return methodOperationParameters;
        }

    }
}