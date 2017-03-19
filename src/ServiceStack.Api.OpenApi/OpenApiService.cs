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
using ServiceStack.Api.OpenApi.Support;
using ServiceStack.Api.OpenApi.Specification;

namespace ServiceStack.Api.OpenApi
{
    [DataContract]
    [Exclude(Feature.Soap)]
    public class Swagger2Resources : IReturn<OpenApiDeclaration>
    {
        [DataMember(Name = "apiKey")]
        public string ApiKey { get; set; }
    }

    [AddHeader(DefaultContentType = MimeTypes.Json)]
    [DefaultRequest(typeof(Swagger2Resources))]
    [Restrict(VisibilityTo = RequestAttributes.None)]
    public class OpenApiService : Service
    {
        internal static bool UseCamelCaseModelPropertyNames { get; set; }
        internal static bool UseLowercaseUnderscoreModelPropertyNames { get; set; }
        internal static bool DisableAutoDtoInBodyParam { get; set; }

        internal static Regex resourceFilterRegex;

        internal static Action<OpenApiDeclaration> ApiDeclarationFilter { get; set; }
        internal static Action<string, OpenApiOperation> OperationFilter { get; set; }
        internal static Action<OpenApiSchema> ModelFilter { get; set; }
        internal static Action<OpenApiProperty> ModelPropertyFilter { get; set; }

        public object Get(Swagger2Resources request)
        {
            var map = HostContext.ServiceController.RestPathMap;
            var paths = new List<RestPath>();

            var basePath = new Uri(base.Request.GetBaseUrl());

            var meta = HostContext.Metadata;


            var operations = HostContext.Metadata;
            var allTypes = operations.GetAllOperationTypes();
            var allOperationNames = operations.GetAllOperationNames();


            foreach (var key in map.Keys)
            {
                var restPaths = map[key];
                var visiblePaths = restPaths.Where(x => meta.IsVisible(Request, Format.Json, x.RequestType.Name));
                paths.AddRange(visiblePaths);
            }

            var definitions = new Dictionary<string, OpenApiSchema>() {
                { "Object",  new OpenApiSchema() {Description = "Object", Type = OpenApiType.Object, Properties = new OrderedDictionary<string, OpenApiProperty>() } }
            };

            foreach (var restPath in paths.SelectMany(x => x.Verbs.Select(y => new { Value = x, Verb = y })))
            {
                ParseDefinitions(definitions, restPath.Value.RequestType, restPath.Value.Path, restPath.Verb);
            }

            var tags = new List<OpenApiTag>();
            var apiPaths = ParseOperations(paths, definitions, tags);

            var result = new OpenApiDeclaration
            {
                Info = new OpenApiInfo()
                {
                    Title = HostContext.ServiceName,
                    Version = HostContext.Config.ApiVersion,
                },
                Paths = apiPaths,
                BasePath = basePath.AbsolutePath,
                Schemes = new List<string> { basePath.Scheme }, //TODO: get https from config
                Host = basePath.Authority,
                Consumes = new List<string>() { "application/json" },
                Definitions = definitions,
                Tags = tags.OrderBy(t => t.Name).ToList()
            };


            if (OperationFilter != null)
                apiPaths.Each(x => GetOperations(x.Value).Each(o => OperationFilter(o.Item1, o.Item2)));
                
            ApiDeclarationFilter?.Invoke(result);

            return new HttpResult(result)
            {
                ResultScope = () => JsConfig.With(includeNullValues: false)
            };
        }

        private IEnumerable<Tuple<string, OpenApiOperation>> GetOperations(OpenApiPath value)
        {
            if (value.Get != null) yield return new Tuple<string, OpenApiOperation>("GET", value.Get);
            if (value.Post != null) yield return new Tuple<string, OpenApiOperation>("POST", value.Post);
            if (value.Put != null) yield return new Tuple<string, OpenApiOperation>("PUT", value.Put);
            if (value.Patch != null) yield return new Tuple<string, OpenApiOperation>("PATCH", value.Patch);
            if (value.Delete != null) yield return new Tuple<string, OpenApiOperation>("DELETE", value.Delete);
            if (value.Head != null) yield return new Tuple<string, OpenApiOperation>("HEAD", value.Head);
            if (value.Options != null) yield return new Tuple<string, OpenApiOperation>("OPTIONS", value.Options);
        }

        private static readonly Dictionary<Type, string> ClrTypesToSwaggerScalarTypes = new Dictionary<Type, string> {
            {typeof(byte[]), OpenApiType.String},
            {typeof(sbyte[]), OpenApiType.String},
            {typeof(byte), OpenApiType.Integer},
            {typeof(sbyte), OpenApiType.Integer},
            {typeof(bool), OpenApiType.Boolean},
            {typeof(short), OpenApiType.Integer},
            {typeof(ushort), OpenApiType.Integer},
            {typeof(int), OpenApiType.Integer},
            {typeof(uint), OpenApiType.Integer},
            {typeof(long), OpenApiType.Integer},
            {typeof(ulong), OpenApiType.Integer},
            {typeof(float), OpenApiType.Number},
            {typeof(double), OpenApiType.Number},
            {typeof(decimal), OpenApiType.Number},
            {typeof(string), OpenApiType.String},
            {typeof(DateTime), OpenApiType.String}
        };

        private static readonly Dictionary<Type, string> ClrTypesToSwaggerScalarFormats = new Dictionary<Type, string> {
            {typeof(byte[]), OpenApiTypeFormat.Byte},
            {typeof(sbyte[]), OpenApiTypeFormat.Byte},
            {typeof(byte), OpenApiTypeFormat.Int},
            {typeof(sbyte), OpenApiTypeFormat.Int},
            {typeof(short), OpenApiTypeFormat.Int},
            {typeof(ushort), OpenApiTypeFormat.Int},
            {typeof(int), OpenApiTypeFormat.Int},
            {typeof(uint), OpenApiTypeFormat.Int},
            {typeof(long), OpenApiTypeFormat.Long},
            {typeof(ulong), OpenApiTypeFormat.Long},
            {typeof(float), OpenApiTypeFormat.Float},
            {typeof(double), OpenApiTypeFormat.Double},
            {typeof(decimal), OpenApiTypeFormat.Double},
            {typeof(DateTime), OpenApiTypeFormat.DateTime}
        };


        private static bool IsSwaggerScalarType(Type type)
        {
            return ClrTypesToSwaggerScalarTypes.ContainsKey(type) 
                || (Nullable.GetUnderlyingType(type) ?? type).IsEnum()
                || (type.IsValueType() && !IsKeyValuePairType(type))
                || type.IsNullableType();
        }

        private static string GetSwaggerTypeName(Type type)
        {
            var lookupType = Nullable.GetUnderlyingType(type) ?? type;

            return ClrTypesToSwaggerScalarTypes.ContainsKey(lookupType)
                ? ClrTypesToSwaggerScalarTypes[lookupType]
                : GetModelTypeName(lookupType);
        }

        private static string GetSwaggerTypeFormat(Type type, string route = null, string verb = null)
        {
            var lookupType = Nullable.GetUnderlyingType(type) ?? type;

            string format = null;

            //special case for response types byte[]. If byte[] is in response
            //then we should use `binary` swagger type, because it's octet-encoded
            //otherwise we use `byte` swagger type for base64-encoded input
            if (route == null && verb == null && type == typeof(byte[]))
                return OpenApiTypeFormat.Binary;

            ClrTypesToSwaggerScalarFormats.TryGetValue(lookupType, out format);
            return format;
        }

        private static Type GetListElementType(Type type)
        {
            if (type.IsArray) return type.GetElementType();

            if (!type.IsGenericType()) return null;
            var genericType = type.GetGenericTypeDefinition();
            if (genericType == typeof(List<>) || genericType == typeof(IList<>) || genericType == typeof(IEnumerable<>))
                return type.GetGenericArguments()[0];
            return null;
        }

        private static bool IsListType(Type type)
        {
            //Swagger2 specification has a special data format for type byte[] ('byte', 'binary' or 'file'), so it's not a list
            if (type == typeof(byte[]))
                return false;

            return GetListElementType(type) != null;
        }

        private static bool IsDictionaryType(Type type)
        {
            if (!type.IsGenericType()) return false;

            var genericType = type.GetGenericTypeDefinition();
            if (genericType == typeof(Dictionary<,>)
                || genericType == typeof(IDictionary<,>)
                || genericType == typeof(IReadOnlyDictionary<,>)
                || genericType == typeof(SortedDictionary<,>))
            {
                return true;
            }

            return false;
        }

        private OpenApiSchema GetDictionaryModel(IDictionary<string, OpenApiSchema> models, Type modelType, string route, string verb)
        {
            if (!IsDictionaryType(modelType))
                return null;

            var valueType = modelType.GetTypeGenericArguments()[1];

            ParseDefinitions(models, valueType, route, verb);

            return new OpenApiSchema()
            {
                Type = OpenApiType.Object,
                Description = modelType.GetDescription() ?? GetModelTypeName(modelType),
                AdditionalProperties = GetSwaggerProperty(models, valueType, route, verb)
            };
        }

        private static bool IsKeyValuePairType(Type type)
        {
            return type.IsGenericType() && type.GetGenericTypeDefinition() == typeof(KeyValuePair<,>);
        }

        private OpenApiSchema GetKeyValuePairModel(IDictionary<string, OpenApiSchema> models, Type modelType, string route, string verb)
        {
            if (!IsKeyValuePairType(modelType))
                return null;

            var keyType = modelType.GetTypeGenericArguments()[0];
            var valueType = modelType.GetTypeGenericArguments()[1];

            return new OpenApiSchema()
            {
                Type = OpenApiType.Object,
                Description = modelType.GetDescription() ?? GetModelTypeName(modelType),
                Properties = new OrderedDictionary<string, OpenApiProperty>()
                {
                    { "Key", GetSwaggerProperty(models, keyType, route, verb) },
                    { "Value", GetSwaggerProperty(models, valueType, route, verb) }
                }
            };
        }

        private static bool IsRequiredType(Type type)
        {
            return !type.IsNullableType() && type != typeof(string);
        }

        private static string GetModelTypeName(Type modelType)
		{
		    if ((!IsKeyValuePairType(modelType) && modelType.IsValueType()) || modelType.IsNullableType())
		        return OpenApiType.String;

		    if (!modelType.IsGenericType())
		        return modelType.Name;

            var typeName = modelType.ToPrettyName();
		    return typeName;
		}

        private OpenApiProperty GetSwaggerProperty(IDictionary<string, OpenApiSchema> models, Type propertyType, string route, string verb)
        {
            var modelProp = new OpenApiProperty();

            if (IsKeyValuePairType(propertyType))
            {
                ParseDefinitions(models, propertyType, route, verb);
                modelProp.Ref = "#/definitions/" + GetModelTypeName(propertyType);
            }
            else if (IsListType(propertyType))
            {
                modelProp.Type = OpenApiType.Array;
                var listItemType = GetListElementType(propertyType);
                if (IsSwaggerScalarType(listItemType))
                {
                    modelProp.Items = new Dictionary<string, object>
                        {
                            { "type", GetSwaggerTypeName(listItemType) },
                            { "format", GetSwaggerTypeFormat(listItemType, route, verb) }
                        };
                    if (IsRequiredType(listItemType))
                    {
                        modelProp.Items.Add("x-nullable", false);
                        //modelProp.Items.Add("required", "true");
                    }
                }
                else
                {
                    modelProp.Items = new Dictionary<string, object> { { "$ref", "#/definitions/" + GetModelTypeName(listItemType) } };
                }
                ParseDefinitions(models, listItemType, route, verb);
            }
            else if ((Nullable.GetUnderlyingType(propertyType) ?? propertyType).IsEnum())
            {
                var enumType = Nullable.GetUnderlyingType(propertyType) ?? propertyType;
                if (enumType.IsNumericType())
                {
                    var underlyingType = Enum.GetUnderlyingType(enumType);
                    modelProp.Type = GetSwaggerTypeName(underlyingType);
                    modelProp.Format = GetSwaggerTypeFormat(underlyingType, route, verb);
                    modelProp.Enum = GetNumericValues(enumType, underlyingType).ToList();
                }
                else
                {
                    modelProp.Type = OpenApiType.String;
                    modelProp.Enum = Enum.GetNames(enumType).ToList();
                }
            }
            else if (IsSwaggerScalarType(propertyType))
            {
                modelProp.Type = GetSwaggerTypeName(propertyType);
                modelProp.Format = GetSwaggerTypeFormat(propertyType, route, verb);
                modelProp.Nullable = IsRequiredType(propertyType) ? false: (bool?)null;
                //modelProp.Required = IsRequiredType(propertyType) ? true : (bool?)null;
            }
            else
            {
                ParseDefinitions(models, propertyType, route, verb);
                modelProp.Ref = "#/definitions/" + GetModelTypeName(propertyType);
            }

            return modelProp;
        }

        private void ParseResponseModel(IDictionary<string, OpenApiSchema> models, Type modelType)
        {
            ParseDefinitions(models, modelType, null, null);
        }

        private void ParseDefinitions(IDictionary<string, OpenApiSchema> models, Type modelType, string route, string verb)
        {
            if (IsSwaggerScalarType(modelType) || modelType.ExcludesFeature(Feature.Metadata)) return;

            var modelId = GetModelTypeName(modelType);
            if (models.ContainsKey(modelId)) return;

            var model = GetDictionaryModel(models, modelType, route, verb) 
                ?? GetKeyValuePairModel(models, modelType, route, verb)
                ?? new OpenApiSchema
                {   
                    Type = OpenApiType.Object,
                    Description = modelType.GetDescription() ?? GetModelTypeName(modelType),
                    Properties = new OrderedDictionary<string, OpenApiProperty>()
                };

            models[modelId] = model;

            var properties = modelType.GetProperties();

            // Order model properties by DataMember.Order if [DataContract] and [DataMember](s) defined
            // Ordering defined by: http://msdn.microsoft.com/en-us/library/ms729813.aspx
            var dataContractAttr = modelType.FirstAttribute<DataContractAttribute>();
            if (dataContractAttr != null && properties.Any(prop => prop.IsDefined(typeof(DataMemberAttribute), true)))
            {
                var typeOrder = new List<Type> { modelType };
                var baseType = modelType.BaseType();
                while (baseType != null)
                {
                    typeOrder.Add(baseType);
                    baseType = baseType.BaseType();
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

                    var modelProp = GetSwaggerProperty(models, prop.PropertyType, route, verb);

                    modelProp.Description = prop.GetDescription() ?? apiDoc?.Description;

                    //TODO: Maybe need to add new attributes for swagger2 'Type' and 'Format' properties
                    //var propAttr = prop.FirstAttribute<ApiMemberAttribute>();
                    //if (propAttr?.DataType != null)
                    //    modelProp.Format = propAttr.DataType;     //modelProp.Type = propAttr.DataType;

                    var allowableValues = prop.FirstAttribute<ApiAllowableValuesAttribute>();
                    if (allowableValues != null)
                        modelProp.Enum = GetEnumValues(allowableValues);

                    ModelPropertyFilter?.Invoke(modelProp);

                    model.Properties[GetModelPropertyName(prop)] = modelProp;
                }
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

        private OpenApiSchema GetResponseSchema(IRestPath restPath, IDictionary<string, OpenApiSchema> models)
        {
            // Given: class MyDto : IReturn<X>. Determine the type X.
            foreach (var i in restPath.RequestType.GetInterfaces())
            {
                if (i.IsGenericType() && i.GetGenericTypeDefinition() == typeof(IReturn<>))
                {
                    var returnType = i.GetGenericArguments()[0];
                    ParseResponseModel(models, returnType);

                    if (IsSwaggerScalarType(returnType))
                    {
                        return new OpenApiSchema()
                        {
                            Type = GetSwaggerTypeName(returnType),
                            Format = GetSwaggerTypeFormat(returnType)
                        };
                    }

                    // Handle IReturn<Dictionary<string, SomeClass>> or IReturn<IDictionary<string,SomeClass>>
                    if (IsDictionaryType(returnType))
                    {
                        var schema = GetDictionaryModel(models, returnType, null, null);
                        if (schema != null)
                            return schema;
                    }

                    // Handle IReturn<List<SomeClass>> or IReturn<SomeClass[]>
                    if (IsListType(returnType))
                    {
                        var schema = new OpenApiSchema()
                        {
                            Type = SwaggerType.Array,
                        };
                        var listItemType = GetListElementType(returnType);
                        ParseResponseModel(models, listItemType);
                        if (IsSwaggerScalarType(listItemType))
                        {
                            schema.Items = new Dictionary<string, object>
                            {
                                { "type", GetSwaggerTypeName(listItemType) },
                                { "format", GetSwaggerTypeFormat(listItemType) }
                            };

                        }
                        else
                        {
                            schema.Items = new Dictionary<string, object> { { "$ref", "#/definitions/" + GetModelTypeName(listItemType) } };
                        }

                        return schema;
                    }

                    return new OpenApiSchema()
                    {
                        Ref = "#/definitions/" + GetModelTypeName(returnType)
                    };
                }
            }

            return new OpenApiSchema() { Ref = "#/definitions/Object" };
        }

        private OrderedDictionary<string, OpenApiResponse> GetMethodResponseCodes(IRestPath restPath, IDictionary<string, OpenApiSchema> models, Type requestType)
        {
            var responses = new OrderedDictionary<string, OpenApiResponse>();

            var responseSchema = GetResponseSchema(restPath, models);

            responses.Add("default", new OpenApiResponse()
            {
                Schema = responseSchema,
                Description = String.Empty //TODO: description
            });
                
            foreach (var attr in requestType.AllAttributes<ApiResponseAttribute>())
            {
                responses.Add(attr.StatusCode.ToString(), new OpenApiResponse()
                {
                    Description = attr.Description,
                });
            }

            return responses;
        }

        private OrderedDictionary<string, OpenApiPath> ParseOperations(List<RestPath> restPaths, Dictionary<string, OpenApiSchema> models, List<OpenApiTag> tags)
        {
            var apiPaths = new OrderedDictionary<string, OpenApiPath>();

            foreach (var restPath in restPaths)
            {
                var verbs = new List<string>();
                var summary = restPath.Summary ?? restPath.RequestType.GetDescription();
                var notes = restPath.Notes;

                verbs.AddRange(restPath.AllowsAllVerbs
                    ? new[] { "GET", "POST", "PUT", "DELETE" }
                    : restPath.AllowedVerbs.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries));

                var routePath = restPath.Path.Replace("*", "");
                var requestType = restPath.RequestType;

                OpenApiPath curPath;

                if (!apiPaths.TryGetValue(restPath.Path, out curPath))
                {
                    curPath = new OpenApiPath()
                    {
                        Parameters = new List<OpenApiParameter>() { GetFormatJsonParameter() }
                    };
                    apiPaths.Add(restPath.Path, curPath);

                    tags.Add(new OpenApiTag() { Name = restPath.Path, Description = summary });
                }

                foreach (var verb in verbs)
                {
                    var operation = new OpenApiOperation()
                    {
                        Summary = summary,
                        Description = summary,
                        OperationId = requestType.Name + GetOperationNamePostfix(verb),
                        Parameters = ParseParameters(verb, requestType, models, routePath),
                        Responses = GetMethodResponseCodes(restPath, models, requestType),
                        Consumes = new List<string>() { "application/json" },
                        Produces = new List<string>() { "application/json" },
                        Tags = new List<string>() { restPath.Path }
                    };

                    switch(verb)
                    {
                        case "GET": curPath.Get = operation; break;
                        case "POST": curPath.Post = operation; break;
                        case "PUT": curPath.Put = operation; break;
                        case "DELETE": curPath.Delete = operation; break;
                        case "PATCH": curPath.Patch = operation; break;
                        case "HEAD": curPath.Head = operation; break;
                        case "OPTIONS": curPath.Options = operation; break;
                    }
                }
            }

            return apiPaths;
        }


        static readonly Dictionary<string, string> postfixes = new Dictionary<string, string>()
        {
            { "GET", "_Get" },      //'Get' or 'List' to pass Autorest validation
            { "PUT", "_Create" },   //'Create' to pass Autorest validation
            { "POST", "_Post" },
            { "PATCH", "_Update" }, //'Update' to pass Autorest validation
            { "DELETE", "_Delete" } //'Delete' to pass Autorest validation
        };
            
        /// Returns operation postfix to make operationId unique and swagger json be validable
        private static string GetOperationNamePostfix(string verb)
        {
            string postfix = null;

            postfixes.TryGetValue(verb, out postfix);

            return postfix ?? String.Empty;
        }

        private static List<string> GetEnumValues(ApiAllowableValuesAttribute attr)
        {
            return attr != null && attr.Values != null ? attr.Values.ToList() : null;
        }
        
        private List<OpenApiParameter> ParseParameters(string verb, Type operationType, IDictionary<string, OpenApiSchema> models, string route)
        {
            var hasDataContract = operationType.HasAttribute<DataContractAttribute>();

            var properties = operationType.GetProperties();
            var paramAttrs = new Dictionary<string, ApiMemberAttribute[]>();
            var allowableParams = new List<ApiAllowableValuesAttribute>();
            var defaultOperationParameters = new List<OpenApiParameter>();

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
                        ? "formData"
                        : "query";


                var parameter = GetParameter(models, property.PropertyType,
                    route, verb,
                    propertyName, paramType,
                    allowableValuesAttrs.FirstOrDefault());
                    
                defaultOperationParameters.Add(parameter);
            }

            var methodOperationParameters = defaultOperationParameters;
            if (hasApiMembers)
            {
                methodOperationParameters = new List<OpenApiParameter>();
                foreach (var key in paramAttrs.Keys)
                {
                    var apiMembers = paramAttrs[key];
                    foreach (var member in apiMembers)
                    {
                        if ((member.Verb == null || string.Compare(member.Verb, verb, StringComparison.OrdinalIgnoreCase) == 0)
                            && (member.Route == null || (route ?? "").StartsWith(member.Route))
                            && !string.Equals(member.ParameterType, "model")
                            && methodOperationParameters.All(x => x.Name != (member.Name ?? key)))
                        {
                            methodOperationParameters.Add(new OpenApiParameter
                            {
                                Type = member.DataType ?? SwaggerType.String,
                                //AllowMultiple = member.AllowMultiple,
                                Description = member.Description,
                                Name = member.Name ?? key,
                                In = member.GetParamType(operationType, member.Verb ?? verb),
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
                    && !methodOperationParameters.Any(p => "body".EqualsIgnoreCase(p.In)))
                {
                    ParseDefinitions(models, operationType, route, verb);

                    OpenApiParameter parameter = GetParameter(models, operationType, route, verb, "body", "body");

                    methodOperationParameters.Add(parameter);
                }
            }

            return methodOperationParameters;
        }

        private OpenApiParameter GetParameter(IDictionary<string, OpenApiSchema> models, Type modelType, string route, string verb, string paramName, string paramIn, ApiAllowableValuesAttribute allowableValueAttrs = null)
        {
            OpenApiParameter parameter;

            if (IsDictionaryType(modelType))
            {
                parameter = new OpenApiParameter
                {
                    In = paramIn,
                    Name = paramName,
                    Schema = GetDictionaryModel(models, modelType, route, verb)
                };
            }
            else if (IsListType(modelType))
            {
                parameter = GetListParameter(models, modelType, route, verb, paramName, paramIn);
            }
            else if (IsSwaggerScalarType(modelType))
            {
                parameter = new OpenApiParameter
                {
                    In = paramIn,
                    Name = paramName,
                    Type = GetSwaggerTypeName(modelType),
                    Format = GetSwaggerTypeFormat(modelType, route, verb),
                    Enum = GetEnumValues(allowableValueAttrs),
                    Nullable = IsRequiredType(modelType) ? false : (bool?)null
                };
            }
            else
            {
                parameter = new OpenApiParameter
                {
                    In = paramIn,
                    Name = paramName,
                    Schema = new OpenApiSchema() { Ref = "#/definitions/" + GetModelTypeName(modelType) }
                };
            }

            return parameter;
        }

        private OpenApiParameter GetListParameter(IDictionary<string, OpenApiSchema> models, Type listType, string route, string verb, string paramName, string paramIn)
        {
            if (!IsListType(listType))
                return null;

            var parameter = new OpenApiParameter
            {
                Type = OpenApiType.Array,
                CollectionFormat = "multi",
                Description = listType.GetDescription(),
                Name = paramName,
                In = paramIn,
                Required = paramIn == "path"
            };

            var listItemType = GetListElementType(listType);
            if (IsSwaggerScalarType(listItemType))
            {
                parameter.Items = new Dictionary<string, object>
                        {
                            { "type", GetSwaggerTypeName(listItemType) },
                            { "format", GetSwaggerTypeFormat(listItemType, route, verb) }
                        };
                if (IsRequiredType(listItemType))
                {
                    parameter.Items.Add("x-nullable", false);
                }
            }
            else
            {
                parameter.Items = new Dictionary<string, object> { { "$ref", "#/definitions/" + GetModelTypeName(listItemType) } };
            }

            ParseDefinitions(models, listItemType, route, verb);

            return parameter;
        }

        private OpenApiParameter GetFormatJsonParameter()
        {
            return new OpenApiParameter()
            {
                Type = OpenApiType.String,
                Name = "format",
                Description = "Specifies response output format",
                Default = "json",
                In = "query",
            };
        }

    }
}