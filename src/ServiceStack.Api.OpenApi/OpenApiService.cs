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
        internal static bool UseCamelCaseSchemaPropertyNames { get; set; }
        internal static bool UseLowercaseUnderscoreSchemaPropertyNames { get; set; }
        internal static bool DisableAutoDtoInBodyParam { get; set; }

        internal static Regex resourceFilterRegex;

        internal static Action<OpenApiDeclaration> ApiDeclarationFilter { get; set; }
        internal static Action<string, OpenApiOperation> OperationFilter { get; set; }
        internal static Action<OpenApiSchema> SchemaFilter { get; set; }
        internal static Action<OpenApiProperty> SchemaPropertyFilter { get; set; }
        internal static string[] AnyRouteVerbs { get; set; }

        public object Get(Swagger2Resources request)
        {
            var map = HostContext.ServiceController.RestPathMap;
            var paths = new List<RestPath>();

            var basePath = new Uri(base.Request.GetBaseUrl());

            var meta = HostContext.Metadata;

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
                Info = new OpenApiInfo
                {
                    Title = HostContext.ServiceName,
                    Version = HostContext.Config.ApiVersion,
                },
                Paths = apiPaths,
                BasePath = basePath.AbsolutePath,
                Schemes = new List<string> { basePath.Scheme }, //TODO: get https from config
                Host = basePath.Authority,
                Consumes = new List<string> { "application/json" },
                Produces = new List<string> { "application/json" },
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
                : GetSchemaTypeName(lookupType);
        }

        private static string GetSwaggerTypeFormat(Type type, string route = null, string verb = null)
        {
            var lookupType = Nullable.GetUnderlyingType(type) ?? type;

            //special case for response types byte[]. If byte[] is in response
            //then we should use `binary` swagger type, because it's octet-encoded
            //otherwise we use `byte` swagger type for base64-encoded input
            if (route == null && verb == null && type == typeof(byte[]))
                return OpenApiTypeFormat.Binary;

            string format;
            return ClrTypesToSwaggerScalarFormats.TryGetValue(lookupType, out format) ? format : null;
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

        private Dictionary<string, object> GetOpenApiListItems(Type listItemType, string route, string verb)
        {
            var items = new Dictionary<string, object>();

            if (IsSwaggerScalarType(listItemType))
            {
                items.Add("type", GetSwaggerTypeName(listItemType));
                items.Add("format", GetSwaggerTypeFormat(listItemType, route, verb));
                if (IsRequiredType(listItemType))
                {
                    items.Add("x-nullable", false);
                }
            }
            else
            {
                items.Add("$ref", "#/definitions/" + GetSchemaTypeName(listItemType));
            }

            return items;
        }

        private OpenApiSchema GetListSchema(IDictionary<string, OpenApiSchema> schemas, Type schemaType, string route, string verb)
        {
            if (!IsListType(schemaType))
                return null;

            var listItemType = GetListElementType(schemaType);
            ParseDefinitions(schemas, listItemType, route, verb);

            return new OpenApiSchema
            {
                Type = SwaggerType.Array,
                Items = GetOpenApiListItems(listItemType, route, verb)
            };
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

        private OpenApiSchema GetDictionarySchema(IDictionary<string, OpenApiSchema> schemas, Type schemaType, string route, string verb)
        {
            if (!IsDictionaryType(schemaType))
                return null;

            var valueType = schemaType.GetTypeGenericArguments()[1];

            ParseDefinitions(schemas, valueType, route, verb);

            return new OpenApiSchema
            {
                Type = OpenApiType.Object,
                Description = schemaType.GetDescription() ?? GetSchemaTypeName(schemaType),
                AdditionalProperties = GetOpenApiProperty(schemas, valueType, route, verb)
            };
        }

        private static bool IsKeyValuePairType(Type type)
        {
            return type.IsGenericType() && type.GetGenericTypeDefinition() == typeof(KeyValuePair<,>);
        }

        private OpenApiSchema GetKeyValuePairSchema(IDictionary<string, OpenApiSchema> schemas, Type schemaType, string route, string verb)
        {
            if (!IsKeyValuePairType(schemaType))
                return null;

            var keyType = schemaType.GetTypeGenericArguments()[0];
            var valueType = schemaType.GetTypeGenericArguments()[1];

            return new OpenApiSchema
            {
                Type = OpenApiType.Object,
                Description = schemaType.GetDescription() ?? GetSchemaTypeName(schemaType),
                Properties = new OrderedDictionary<string, OpenApiProperty>()
                {
                    { "Key", GetOpenApiProperty(schemas, keyType, route, verb) },
                    { "Value", GetOpenApiProperty(schemas, valueType, route, verb) }
                }
            };
        }

        private static bool IsRequiredType(Type type)
        {
            return !type.IsNullableType() && type != typeof(string);
        }

        private static string GetSchemaTypeName(Type schemaType)
        {
            if ((!IsKeyValuePairType(schemaType) && schemaType.IsValueType()) || schemaType.IsNullableType())
                return OpenApiType.String;

            if (!schemaType.IsGenericType())
                return schemaType.Name;

            var typeName = schemaType.ToPrettyName();
            return typeName;
        }

        private OpenApiProperty GetOpenApiProperty(IDictionary<string, OpenApiSchema> schemas, Type propertyType, string route, string verb)
        {
            var schemaProp = new OpenApiProperty();

            if (IsKeyValuePairType(propertyType))
            {
                ParseDefinitions(schemas, propertyType, route, verb);
                schemaProp.Ref = "#/definitions/" + GetSchemaTypeName(propertyType);
            }
            else if (IsListType(propertyType))
            {
                schemaProp.Type = OpenApiType.Array;
                var listItemType = GetListElementType(propertyType);
                if (IsSwaggerScalarType(listItemType))
                {
                    schemaProp.Items = new Dictionary<string, object>
                        {
                            { "type", GetSwaggerTypeName(listItemType) },
                            { "format", GetSwaggerTypeFormat(listItemType, route, verb) }
                        };
                    if (IsRequiredType(listItemType))
                    {
                        schemaProp.Items.Add("x-nullable", false);
                        //schemaProp.Items.Add("required", "true");
                    }
                }
                else
                {
                    schemaProp.Items = new Dictionary<string, object> { { "$ref", "#/definitions/" + GetSchemaTypeName(listItemType) } };
                }
                ParseDefinitions(schemas, listItemType, route, verb);
            }
            else if ((Nullable.GetUnderlyingType(propertyType) ?? propertyType).IsEnum())
            {
                var enumType = Nullable.GetUnderlyingType(propertyType) ?? propertyType;
                if (enumType.IsNumericType())
                {
                    var underlyingType = Enum.GetUnderlyingType(enumType);
                    schemaProp.Type = GetSwaggerTypeName(underlyingType);
                    schemaProp.Format = GetSwaggerTypeFormat(underlyingType, route, verb);
                    schemaProp.Enum = GetNumericValues(enumType, underlyingType).ToList();
                }
                else
                {
                    schemaProp.Type = OpenApiType.String;
                    schemaProp.Enum = Enum.GetNames(enumType).ToList();
                }
            }
            else if (IsSwaggerScalarType(propertyType))
            {
                schemaProp.Type = GetSwaggerTypeName(propertyType);
                schemaProp.Format = GetSwaggerTypeFormat(propertyType, route, verb);
                schemaProp.Nullable = IsRequiredType(propertyType) ? false : (bool?)null;
                //schemaProp.Required = IsRequiredType(propertyType) ? true : (bool?)null;
            }
            else
            {
                ParseDefinitions(schemas, propertyType, route, verb);
                schemaProp.Ref = "#/definitions/" + GetSchemaTypeName(propertyType);
            }

            return schemaProp;
        }

        private void ParseDefinitions(IDictionary<string, OpenApiSchema> schemas, Type schemaType, string route, string verb)
        {
            if (IsSwaggerScalarType(schemaType) || schemaType.ExcludesFeature(Feature.Metadata)) return;

            var schemaId = GetSchemaTypeName(schemaType);
            if (schemas.ContainsKey(schemaId)) return;

            var schema = GetDictionarySchema(schemas, schemaType, route, verb)
                    ?? GetKeyValuePairSchema(schemas, schemaType, route, verb)
                    ?? GetListSchema(schemas, schemaType, route, verb);

            bool parseProperties = false;

            if (schema == null)
            {
                schema = new OpenApiSchema
                {
                    Type = OpenApiType.Object,
                    Description = schemaType.GetDescription() ?? GetSchemaTypeName(schemaType),
                    Properties = new OrderedDictionary<string, OpenApiProperty>()
                };
                parseProperties = schemaType.IsUserType();
            }
            schemas[schemaId] = schema;

            var properties = schemaType.GetProperties();

            // Order schema properties by DataMember.Order if [DataContract] and [DataMember](s) defined
            // Ordering defined by: http://msdn.microsoft.com/en-us/library/ms729813.aspx
            var dataContractAttr = schemaType.FirstAttribute<DataContractAttribute>();
            if (dataContractAttr != null && properties.Any(prop => prop.IsDefined(typeof(DataMemberAttribute), true)))
            {
                var typeOrder = new List<Type> { schemaType };
                var baseType = schemaType.BaseType();
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
                    var schemaProp = GetOpenApiProperty(schemas, prop.PropertyType, route, verb);

                    schemaProp.Description = prop.GetDescription() ?? apiDoc?.Description;

                    var propAttr = prop.FirstAttribute<ApiMemberAttribute>();
                    if (propAttr != null)
                    {
                        if (propAttr.DataType != null)
                            schemaProp.Type = propAttr.DataType;

                        if (propAttr.Format != null)
                            schemaProp.Format = propAttr.Format;
                    }

                    var allowableValues = prop.FirstAttribute<ApiAllowableValuesAttribute>();
                    if (allowableValues != null)
                        schemaProp.Enum = GetEnumValues(allowableValues);

                    SchemaPropertyFilter?.Invoke(schemaProp);

                    schema.Properties[GetSchemaPropertyName(prop)] = schemaProp;
                }
            }
        }

        private static string GetSchemaPropertyName(PropertyInfo prop)
        {
            var dataMemberAttr = prop.FirstAttribute<DataMemberAttribute>();
            if (dataMemberAttr != null && !dataMemberAttr.Name.IsNullOrEmpty())
                return dataMemberAttr.Name;

            return UseCamelCaseSchemaPropertyNames
                ? (UseLowercaseUnderscoreSchemaPropertyNames ? prop.Name.ToLowercaseUnderscore() : prop.Name.ToCamelCase())
                : prop.Name;
        }

        private static IEnumerable<string> GetNumericValues(Type propertyType, Type underlyingType)
        {
            var values = Enum.GetValues(propertyType)
                .Map(x => $"{Convert.ChangeType(x, underlyingType)} ({x})");

            return values;
        }

        private OpenApiSchema GetResponseSchema(IRestPath restPath, IDictionary<string, OpenApiSchema> schemas)
        {
            // Given: class MyDto : IReturn<X>. Determine the type X.
            foreach (var i in restPath.RequestType.GetInterfaces())
            {
                if (i.IsGenericType() && i.GetGenericTypeDefinition() == typeof(IReturn<>))
                {
                    var schemaType = i.GetGenericArguments()[0];
                    ParseDefinitions(schemas, schemaType, null, null);

                    var schema = GetDictionarySchema(schemas, schemaType, null, null)
                        ?? GetKeyValuePairSchema(schemas, schemaType, null, null)
                        ?? GetListSchema(schemas, schemaType, null, null)
                        ?? (IsSwaggerScalarType(schemaType)
                            ? new OpenApiSchema {
                                  Type = GetSwaggerTypeName(schemaType),
                                  Format = GetSwaggerTypeFormat(schemaType)
                              }
                            : new OpenApiSchema { Ref = "#/definitions/" + GetSchemaTypeName(schemaType) });

                    return schema;
                }
            }

            return new OpenApiSchema { Ref = "#/definitions/Object" };
        }

        private OrderedDictionary<string, OpenApiResponse> GetMethodResponseCodes(IRestPath restPath, IDictionary<string, OpenApiSchema> schemas, Type requestType)
        {
            var responses = new OrderedDictionary<string, OpenApiResponse>();

            var responseSchema = GetResponseSchema(restPath, schemas);

            responses.Add("default", new OpenApiResponse
            {
                Schema = responseSchema,
                Description = string.Empty //TODO: description
            });

            foreach (var attr in requestType.AllAttributes<ApiResponseAttribute>())
            {
                responses.Add(attr.StatusCode.ToString(), new OpenApiResponse {
                    Description = attr.Description,
                });
            }

            return responses;
        }

        private OrderedDictionary<string, OpenApiPath> ParseOperations(List<RestPath> restPaths, Dictionary<string, OpenApiSchema> schemas, List<OpenApiTag> tags)
        {
            var apiPaths = new OrderedDictionary<string, OpenApiPath>();

            foreach (var restPath in restPaths)
            {
                var verbs = new List<string>();
                var summary = restPath.Summary ?? restPath.RequestType.GetDescription();

                verbs.AddRange(restPath.AllowsAllVerbs
                    ? AnyRouteVerbs
                    : restPath.Verbs);

                var routePath = restPath.Path.Replace("*", "");
                var requestType = restPath.RequestType;

                OpenApiPath curPath;

                if (!apiPaths.TryGetValue(restPath.Path, out curPath))
                {
                    curPath = new OpenApiPath() { Parameters = new List<OpenApiParameter> { GetAcceptHeaderParameter() } };
                    apiPaths.Add(restPath.Path, curPath);

                    tags.Add(new OpenApiTag { Name = restPath.Path, Description = summary });
                }

                foreach (var verb in verbs)
                {
                    var operation = new OpenApiOperation
                    {
                        Summary = summary,
                        Description = restPath.Notes ?? summary,
                        OperationId = requestType.Name + GetOperationNamePostfix(verb),
                        Parameters = ParseParameters(schemas, requestType, verb, routePath),
                        Responses = GetMethodResponseCodes(restPath, schemas, requestType),
                        Consumes = new List<string> { "application/json" },
                        Produces = new List<string> { "application/json" },
                        Tags = new List<string> { restPath.Path },
                        Deprecated = requestType.HasAttribute<ObsoleteAttribute>()
                    };

                    switch (verb)
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
            string postfix;
            postfixes.TryGetValue(verb, out postfix);
            return postfix ?? string.Empty;
        }

        private static List<string> GetEnumValues(ApiAllowableValuesAttribute attr)
        {
            return attr?.Values?.ToList();
        }

        private List<OpenApiParameter> ParseParameters(IDictionary<string, OpenApiSchema> schemas, Type operationType, string route, string verb)
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

                var propertyName = attr?.Name ?? property.Name;

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


                var parameter = GetParameter(schemas, property.PropertyType,
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
                    ParseDefinitions(schemas, operationType, route, verb);

                    var parameter = GetParameter(schemas, operationType, route, verb, "body", "body");

                    methodOperationParameters.Add(parameter);
                }
            }

            return methodOperationParameters;
        }

        private OpenApiParameter GetParameter(IDictionary<string, OpenApiSchema> schemas, Type schemaType, string route, string verb, string paramName, string paramIn, ApiAllowableValuesAttribute allowableValueAttrs = null)
        {
            OpenApiParameter parameter;

            if (IsDictionaryType(schemaType))
            {
                parameter = new OpenApiParameter
                {
                    In = paramIn,
                    Name = paramName,
                    Schema = GetDictionarySchema(schemas, schemaType, route, verb)
                };
            }
            else if (IsListType(schemaType))
            {
                parameter = GetListParameter(schemas, schemaType, route, verb, paramName, paramIn);
            }
            else if (IsSwaggerScalarType(schemaType))
            {
                parameter = new OpenApiParameter
                {
                    In = paramIn,
                    Name = paramName,
                    Type = GetSwaggerTypeName(schemaType),
                    Format = GetSwaggerTypeFormat(schemaType, route, verb),
                    Enum = GetEnumValues(allowableValueAttrs),
                    Nullable = IsRequiredType(schemaType) ? false : (bool?)null
                };
            }
            else
            {
                parameter = new OpenApiParameter
                {
                    In = paramIn,
                    Name = paramName,
                    Schema = new OpenApiSchema { Ref = "#/definitions/" + GetSchemaTypeName(schemaType) }
                };
            }

            return parameter;
        }

        private OpenApiParameter GetListParameter(IDictionary<string, OpenApiSchema> schemas, Type listType, string route, string verb, string paramName, string paramIn)
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
            ParseDefinitions(schemas, listItemType, route, verb);
            parameter.Items = GetOpenApiListItems(listItemType, route, verb);

            return parameter;
        }

        private OpenApiParameter GetAcceptHeaderParameter()
        {
            return new OpenApiParameter
            {
                Type = OpenApiType.String,
                Name = "Accept",
                Description = "Accept Header",
                Enum = new List<string>() { "application/json"},
                In = "header",
                Required = true,
            };
        }
    }
}