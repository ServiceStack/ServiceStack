using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
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
    [ExcludeMetadata]
    public class OpenApiSpecification : IReturn<OpenApiDeclaration>
    {
        [DataMember(Name = "apiKey")]
        public string ApiKey { get; set; }
    }

    [AddHeader(DefaultContentType = MimeTypes.Json)]
    [DefaultRequest(typeof(OpenApiSpecification))]
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
        internal static string[] InlineSchemaTypesInNamespaces { get; set; }
        
        public static Dictionary<string, OpenApiSecuritySchema> SecurityDefinitions { get; set; }
        public static Dictionary<string, List<string>> OperationSecurity { get; set; }

        public object Get(OpenApiSpecification request)
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

            var definitions = new Dictionary<string, OpenApiSchema>
            {
                { "Object", new OpenApiSchema { Description = "Object", Type = OpenApiType.Object, Properties = new OrderedDictionary<string, OpenApiProperty>() } },
            };

            foreach (var restPath in paths.SelectMany(x => x.Verbs.Select(y => new { Value = x, Verb = y })))
            {
                ParseDefinitions(definitions, restPath.Value.RequestType, restPath.Value.Path, restPath.Verb);
            }

            var tags = new Dictionary<string, OpenApiTag>();
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
                Definitions = definitions.Where(x => !SchemaIdToClrType.ContainsKey(x.Key) || !IsInlineSchema(SchemaIdToClrType[x.Key])).ToDictionary(x => x.Key, x => x.Value),
                Tags = tags.Values.OrderBy(x => x.Name).ToList(),
                Parameters = new Dictionary<string, OpenApiParameter> { { "Accept", GetAcceptHeaderParameter() } },
                SecurityDefinitions = SecurityDefinitions,
            };

            if (SchemaFilter != null)
            {
                result.Parameters.Each(x => {
                    if (x.Value.Schema != null)
                        SchemaFilter(x.Value.Schema);
                });
                result.Definitions.Each(x => {
                    if (x.Value.AllOf != null)
                        SchemaFilter(x.Value.AllOf);
                    SchemaFilter(x.Value);
                });
                result.Responses.Each(x => {
                    if (x.Value.Schema != null)
                        SchemaFilter(x.Value.Schema);
                });
            }

            if (OperationFilter != null)
                apiPaths.Each(x => GetOperations(x.Value).Each(o => OperationFilter(o.Item1, o.Item2)));

            ApiDeclarationFilter?.Invoke(result);

            return new HttpResult(result)
            {
                ResultScope = () => JsConfig.With(new Config
                {
                    IncludeNullValues = false, 
                    IncludeNullValuesInDictionaries = false,
                    IncludeTypeInfo = false, 
                    ExcludeTypeInfo = true,
                })
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
            {typeof(DateTime), OpenApiType.String},
            {typeof(DateTimeOffset), OpenApiType.String},
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
            {typeof(DateTime), OpenApiTypeFormat.DateTime},
            {typeof(DateTimeOffset), OpenApiTypeFormat.DateTime},
        };

        private static bool IsSwaggerScalarType(Type type)
        {
            return ClrTypesToSwaggerScalarTypes.ContainsKey(type)
                || (Nullable.GetUnderlyingType(type) ?? type).IsEnum
                || (type.IsValueType && !IsKeyValuePairType(type))
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

            return ClrTypesToSwaggerScalarFormats.TryGetValue(lookupType, out var format) ? format : null;
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
            //Swagger2 specification has a special data format for type byte[] ('byte', 'binary' or 'file'), so it's not a list
            if (type == typeof(byte[]))
                return false;

            return GetListElementType(type) != null;
        }

        private Dictionary<string, object> GetOpenApiListItems(Type listItemType, string route, string verb, 
            string[] enumValues = null)
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
                if (enumValues?.Length > 0)
                {
                    items.Add("enum", enumValues);
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
                Title = GetSchemaTypeName(schemaType),
                Type = OpenApiType.Array,
                Items = GetOpenApiListItems(listItemType, route, verb)
            };
        }

        private static bool IsDictionaryType(Type type)
        {
            if (!type.IsGenericType) return false;

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

            var valueType = schemaType.GetGenericArguments()[1];

            ParseDefinitions(schemas, valueType, route, verb);

            return new OpenApiSchema
            {
                Title = GetSchemaTypeName(schemaType),
                Type = OpenApiType.Object,
                Description = schemaType.GetDescription() ?? GetSchemaTypeName(schemaType),
                AdditionalProperties = GetOpenApiProperty(schemas, valueType, route, verb)
            };
        }

        private static bool IsKeyValuePairType(Type type)
        {
            return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(KeyValuePair<,>);
        }

        private OpenApiSchema GetKeyValuePairSchema(IDictionary<string, OpenApiSchema> schemas, Type schemaType, string route, string verb)
        {
            if (!IsKeyValuePairType(schemaType))
                return null;

            var keyType = schemaType.GetGenericArguments()[0];
            var valueType = schemaType.GetGenericArguments()[1];

            return new OpenApiSchema
            {
                Type = OpenApiType.Object,
                Title = GetSchemaTypeName(schemaType),
                Description = schemaType.GetDescription() ?? GetSchemaTypeName(schemaType),
                Properties = new OrderedDictionary<string, OpenApiProperty>
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
            if ((!IsKeyValuePairType(schemaType) && schemaType.IsValueType) || schemaType.IsNullableType())
                return OpenApiType.String;

            if (!schemaType.IsGenericType)
                return schemaType.Name;

            var typeName = schemaType.ToPrettyName();
            return typeName;
        }

        private static string GetSchemaDefinitionRef(Type schemaType) =>
            swaggerRefRegex.Replace(GetSchemaTypeName(schemaType), "_");

        private static readonly Regex swaggerRefRegex = new Regex("[^A-Za-z0-9\\.\\-_]", RegexOptions.Compiled);

        private OpenApiProperty GetOpenApiProperty(IDictionary<string, OpenApiSchema> schemas, PropertyInfo pi, string route, string verb)
        {
            var ret = GetOpenApiProperty(schemas, pi.PropertyType, route, verb);
            ret.PropertyInfo = pi;
            return ret;
        }
        
        private OpenApiProperty GetOpenApiProperty(IDictionary<string, OpenApiSchema> schemas, Type propertyType, string route, string verb)
        {
            var schemaProp = new OpenApiProperty {
                PropertyType = propertyType,
            };

            if (IsKeyValuePairType(propertyType))
            {
                if (IsInlineSchema(propertyType))
                {
                    ParseDefinitions(schemas, propertyType, route, verb);
                    InlineSchema(schemas[GetSchemaTypeName(propertyType)], schemaProp);
                }
                else
                {
                    ParseDefinitions(schemas, propertyType, route, verb);
                    schemaProp.Ref = "#/definitions/" + GetSchemaDefinitionRef(propertyType);
                }
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
                    ParseDefinitions(schemas, listItemType, route, verb);
                }
                else if (IsInlineSchema(listItemType))
                {
                    ParseDefinitions(schemas, listItemType, route, verb);
                    InlineSchema(schemas[GetSchemaTypeName(listItemType)], schemaProp);
                }
                else
                {
                    schemaProp.Items = new Dictionary<string, object> { { "$ref", "#/definitions/" + GetSchemaDefinitionRef(listItemType) } };
                    ParseDefinitions(schemas, listItemType, route, verb);
                }
            }
            else if ((Nullable.GetUnderlyingType(propertyType) ?? propertyType).IsEnum)
            {
                var enumType = Nullable.GetUnderlyingType(propertyType) ?? propertyType;
                if (enumType.IsNumericType())
                {
                    var underlyingType = Enum.GetUnderlyingType(enumType);
                    schemaProp.Type = GetSwaggerTypeName(underlyingType);
                    schemaProp.Format = GetSwaggerTypeFormat(underlyingType, route, verb);
                    schemaProp.Enum = GetNumericValues(enumType, underlyingType).ToArray();
                }
                else
                {
                    schemaProp.Type = OpenApiType.String;
                    schemaProp.Enum = Enum.GetNames(enumType).ToArray();
                }
            }
            else if (IsSwaggerScalarType(propertyType))
            {
                schemaProp.Type = GetSwaggerTypeName(propertyType);
                schemaProp.Format = GetSwaggerTypeFormat(propertyType, route, verb);
                schemaProp.Nullable = IsRequiredType(propertyType) ? false : (bool?)null;
                //schemaProp.Required = IsRequiredType(propertyType) ? true : (bool?)null;
            }
            else if (IsInlineSchema(propertyType))
            {
                ParseDefinitions(schemas, propertyType, route, verb);
                InlineSchema(schemas[GetSchemaTypeName(propertyType)], schemaProp);
            }
            else
            {
                ParseDefinitions(schemas, propertyType, route, verb);
                schemaProp.Ref = "#/definitions/" + GetSchemaDefinitionRef(propertyType);
            }

            return schemaProp;
        }

        private static void InlineSchema(OpenApiSchema schema, OpenApiProperty schemaProp)
        {
            schemaProp.Items = new Dictionary<string, object>
            {
                {"title", schema.Title},
                {"discriminator", schema.Discriminator},
                {"readOnly", schema.ReadOnly},
                {"xml", schema.Xml},
                {"externalDocs", schema.ExternalDocs},
                {"example", schema.Example},
                {"required", schema.Required},
                {"allOf", schema.AllOf},
                {"properties", schema.Properties},
                {"additionalProperties", schema.AdditionalProperties},
                {"description", schema.Description},
                {"type", schema.Type},
                {"format", schema.Format},
                {"items", schema.Items},
                {"collectionFormat", schema.CollectionFormat},
                {"default", schema.Default},
                {"maximum", schema.Maximum},
                {"exclusiveMaximum", schema.ExclusiveMaximum},
                {"exclusiveMinimum", schema.ExclusiveMinimum},
                {"maxLength", schema.MaxLength},
                {"minLength", schema.MinLength},
                {"pattern", schema.Pattern},
                {"maxItems", schema.MaxItems},
                {"minItems", schema.MinItems},
                {"uniqueItems", schema.UniqueItems},
                {"maxProperties", schema.MaxProperties},
                {"minProperties", schema.MinProperties},
                {"enum", schema.Enum},
                {"multipleOf", schema.MultipleOf},
                {"x-nullable", schema.Nullable}
            };
        }

        protected bool IsInlineSchema(Type schemaType)
        {
            return InlineSchemaTypesInNamespaces.Contains(schemaType.Namespace);
        }

        public Dictionary<string, Type> SchemaIdToClrType { get; } = new();
        
        private void ParseDefinitions(IDictionary<string, OpenApiSchema> schemas, Type schemaType, string route, string verb)
        {
            if (IsSwaggerScalarType(schemaType) || schemaType.ExcludesFeature(Feature.Metadata)) return;

            var schemaId = GetSchemaDefinitionRef(schemaType);
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
                    Title = schemaType.Name,
                    Description = schemaType.GetDescription() ?? GetSchemaTypeName(schemaType),
                    Properties = new OrderedDictionary<string, OpenApiProperty>()
                };
                parseProperties = schemaType.IsUserType();
            }
            schemas[schemaId] = schema;
            SchemaIdToClrType[schemaId] = schemaType;

            var properties = schemaType.GetProperties();

            // Order schema properties by DataMember.Order if [DataContract] and [DataMember](s) defined
            // Ordering defined by: http://msdn.microsoft.com/en-us/library/ms729813.aspx
            var dataContractAttr = schemaType.FirstAttribute<DataContractAttribute>();
            if (dataContractAttr != null && properties.Any(prop => prop.IsDefined(typeof(DataMemberAttribute), true)))
            {
                var typeOrder = new List<Type> { schemaType };
                var baseType = schemaType.BaseType;
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
                    var schemaProperty = GetOpenApiProperty(schemas, prop, route, verb);
                    var schemaPropertyName = GetSchemaPropertyName(prop);

                    schemaProperty.Description = prop.GetDescription() ?? apiDoc?.Description;

                    var propAttr = prop.FirstAttribute<ApiMemberAttribute>();
                    if (propAttr != null)
                    {
                        if (propAttr.DataType != null)
                            schemaProperty.Type = propAttr.DataType;

                        if (propAttr.Format != null)
                            schemaProperty.Format = propAttr.Format;

                        if (propAttr.IsRequired)
                        {
                            if (schema.Required == null)
                                schema.Required = new List<string>();
                            schema.Required.Add(schemaPropertyName);
                        }
                    }

                    schemaProperty.Enum = GetEnumValues(prop.FirstAttribute<ApiAllowableValuesAttribute>());

                    SchemaPropertyFilter?.Invoke(schemaProperty);

                    schema.Properties[schemaPropertyName] = schemaProperty;
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

        private OpenApiSchema GetResponseSchema(IRestPath restPath, IDictionary<string, OpenApiSchema> schemas, out string schemaDescription)
        {
            schemaDescription = string.Empty;

            // Given: class MyDto : IReturn<X>. Determine the type X.
            foreach (var i in restPath.RequestType.GetInterfaces())
            {
                if (i == typeof(IReturnVoid))
                    return GetSchemaForResponseType(typeof(void), schemas, out schemaDescription);

                if (i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IReturn<>))
                {
                    var schemaType = i.GetGenericArguments()[0];
                    return GetSchemaForResponseType(schemaType, schemas, out schemaDescription);
                }
            }

            return new OpenApiSchema { Ref = "#/definitions/Object" };
        }

        private OpenApiSchema GetSchemaForResponseType(Type schemaType, IDictionary<string, OpenApiSchema> schemas, out string schemaDescription)
        {
            if (schemaType == typeof(IReturnVoid) || schemaType == typeof(void))
            {
                schemaDescription = "No Content";
                return null;
            }

            ParseDefinitions(schemas, schemaType, null, null);

            var schema = GetDictionarySchema(schemas, schemaType, null, null)
                ?? GetKeyValuePairSchema(schemas, schemaType, null, null)
                ?? GetListSchema(schemas, schemaType, null, null)
                ?? (IsSwaggerScalarType(schemaType)
                    ? new OpenApiSchema
                    {
                        Title = GetSchemaTypeName(schemaType),
                        Type = GetSwaggerTypeName(schemaType),
                        Format = GetSwaggerTypeFormat(schemaType)
                    }
                : IsInlineSchema(schemaType)
                    ? schemas[GetSchemaTypeName(schemaType)]
                    : new OpenApiSchema { Ref = "#/definitions/" + GetSchemaDefinitionRef(schemaType) });

            schemaDescription = schema.Description ?? schemaType.GetDescription() ?? string.Empty;

            return schema;
        }

        private OrderedDictionary<string, OpenApiResponse> GetMethodResponseCodes(IRestPath restPath, IDictionary<string, OpenApiSchema> schemas, Type requestType)
        {
            var responses = new OrderedDictionary<string, OpenApiResponse>();

            var responseSchema = GetResponseSchema(restPath, schemas, out string schemaDescription);
            //schema is null when return type is IReturnVoid
            var statusCode = responseSchema == null && HostConfig.Instance.Return204NoContentForEmptyResponse
                ? ((int)HttpStatusCode.NoContent).ToString()
                : ((int)HttpStatusCode.OK).ToString();

            responses.Add(statusCode, new OpenApiResponse
            {
                Schema = responseSchema,
                Description = !string.IsNullOrEmpty(schemaDescription) ? schemaDescription : "Success"
            });

            foreach (var attr in requestType.AllAttributes<ApiResponseAttribute>())
            {
                string apiSchemaDescription = string.Empty;

                var response = new OpenApiResponse
                {
                    Schema = attr.ResponseType != null
                        ? GetSchemaForResponseType(attr.ResponseType, schemas, out apiSchemaDescription)
                        : responseSchema,
                    Description = attr.Description ?? apiSchemaDescription
                };

                statusCode = attr.IsDefaultResponse ? "default" : attr.StatusCode.ToString();
                if (!responses.ContainsKey(statusCode))
                    responses.Add(statusCode, response);
                else
                    responses[statusCode] = response;
            }

            return responses;
        }

        private OrderedDictionary<string, OpenApiPath> ParseOperations(List<RestPath> restPaths, Dictionary<string, OpenApiSchema> schemas, Dictionary<string, OpenApiTag> tags)
        {
            var feature = HostContext.GetPlugin<OpenApiFeature>();
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

                if (!apiPaths.TryGetValue(restPath.Path, out var curPath))
                {
                    curPath = new OpenApiPath
                    {
                        Parameters = new List<OpenApiParameter>
                        {
                            new() { Ref = "#/parameters/Accept" }
                        }
                    };
                    apiPaths.Add(restPath.Path, curPath);
                }

                var op = HostContext.Metadata.OperationsMap[requestType];

                var annotatingTagAttributes = requestType.AllAttributes<TagAttribute>();

                foreach (var verb in verbs)
                {
                    var needAuth = op.RequiresAuthentication;
                    var userTags = annotatingTagAttributes.Select(x => x.Name).ToList();

                    var operation = new OpenApiOperation
                    {
                        RequestType = requestType.Name,
                        Summary = summary,
                        Description = restPath.Notes ?? summary,
                        OperationId = GetOperationName(requestType.Name, routePath, verb),
                        Parameters = ParseParameters(schemas, requestType, routePath, verb),
                        Responses = GetMethodResponseCodes(restPath, schemas, requestType),
                        Consumes = new List<string> { MimeTypes.Json },
                        Produces = new List<string> { MimeTypes.Json },
                        Tags = userTags.Count > 0 ? userTags : GetTags(restPath.Path),
                        Deprecated = requestType.HasAttribute<ObsoleteAttribute>(),
                        Security = needAuth ? new List<Dictionary<string, List<string>>> {
                            OperationSecurity
                        } : null
                    };

                    if (HasFormData(verb, operation.Parameters))
                        operation.Consumes = new List<string> { "application/x-www-form-urlencoded" };

                    foreach (var tag in operation.Tags)
                    {
                        if (!tags.ContainsKey(tag))
                        {
                            var tagObject = feature.Tags.FirstOrDefault(x => x.Name == tag)
                                ?? new OpenApiTag { Name = tag };

                            tags.Add(tag, tagObject);
                        }
                    }

                    switch (verb)
                    {
                        case HttpMethods.Get: curPath.Get = operation; break;
                        case HttpMethods.Post: curPath.Post = operation; break;
                        case HttpMethods.Put: curPath.Put = operation; break;
                        case HttpMethods.Delete: curPath.Delete = operation; break;
                        case HttpMethods.Patch: curPath.Patch = operation; break;
                        case HttpMethods.Head: curPath.Head = operation; break;
                        case HttpMethods.Options: curPath.Options = operation; break;
                    }
                }
            }

            return apiPaths;
        }

        private bool IsFormData(string verb, ApiAttribute apiAttr)
        {
            if (verb != HttpMethods.Post && verb != HttpMethods.Put)
                return false;

            if (apiAttr?.BodyParameter == GenerateBodyParameter.Always
                || (!DisableAutoDtoInBodyParam && apiAttr?.BodyParameter != GenerateBodyParameter.Never))
                return false;

            return true;
        }

        private bool HasFormData(string verb, List<OpenApiParameter> parameters)
        {
            return (verb == HttpMethods.Post || verb == HttpMethods.Put) && parameters.Any(p => p.In == "formData");
        }

        static readonly Dictionary<string, string> postfixes = new()
        {
            { HttpMethods.Get, "_Get" },      //'Get' or 'List' to pass Autorest validation
            { HttpMethods.Put, "_Create" },   //'Create' to pass Autorest validation
            { HttpMethods.Post, "_Post" },
            { HttpMethods.Patch, "_Update" }, //'Update' to pass Autorest validation
            { HttpMethods.Delete, "_Delete" } //'Delete' to pass Autorest validation
        };


        HashSet<string> operationIds = new();

        /// Returns operation postfix to make operationId unique and swagger json be validatable
        private string GetOperationName(string name, string route, string verb)
        {
            string pathPostfix = string.Empty;

            var entries = route.Replace("{", string.Empty)
                .Replace("}", string.Empty)
                .Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);

            if (entries.Length > 1)
                pathPostfix = string.Join(string.Empty, entries, 1, entries.Length - 1);

            postfixes.TryGetValue(verb, out var verbPostfix);
            verbPostfix ??= string.Empty;

            var operationId = name + pathPostfix + verbPostfix;

            int num = 2;
            while (operationIds.Contains(operationId))
            {
                operationId = name + pathPostfix + num + verbPostfix;
                num++;
            }

            operationIds.Add(operationId);

            return operationId;
        }

        private static string[] GetEnumValues(ApiAllowableValuesAttribute attr)
        {
            return attr?.Values?.ToArray();
        }

        private List<OpenApiParameter> ParseParameters(IDictionary<string, OpenApiSchema> schemas, Type operationType, string route, string verb)
        {
            var hasDataContract = operationType.HasAttribute<DataContractAttribute>();
            var apiAttr = operationType.FirstAttribute<ApiAttribute>();

            var properties = operationType.GetProperties();
            var paramAttrs = new Dictionary<string, ApiMemberAttribute[]>();
            var propertyTypes = new Dictionary<string, Type>();
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
                propertyTypes[propertyName] = property.PropertyType;
                var allowableValuesAttr = property.FirstAttribute<ApiAllowableValuesAttribute>(); 

                if (hasDataContract && attr == null)
                    continue;

                var inPath = (route ?? "").ToLowerInvariant().Contains("{" + propertyName.ToLowerInvariant() + "}");
                var paramType = inPath
                    ? "path"
                    : IsFormData(verb, apiAttr) ? "formData" : "query";


                var parameter = GetParameter(schemas, property.PropertyType,
                    route, verb,
                    propertyName, paramType,
                    enumValues: allowableValuesAttr != null
                        ? GetEnumValues(allowableValuesAttr)
                        : Html.Input.GetEnumValues(property.PropertyType));

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
                            var allowableValuesAttr = allowableParams.FirstOrDefault(attr => attr.Name == (member.Name ?? key)); 
                            var p = GetParameter(schemas, propertyTypes[key], route, verb,
                                    member.Name ?? key,
                                    member.GetParamType(operationType, member.Verb ?? verb),
                                    enumValues: allowableValuesAttr != null
                                        ? GetEnumValues(allowableValuesAttr)
                                        : Html.Input.GetEnumValues(propertyTypes[key]),
                                    true
                                );
                            p.Type = member.DataType ?? p.Type;
                            p.Format = member.Format ?? p.Format;
                            p.Required = p.In =="path" || member.IsRequired;
                            p.Description = member.Description ?? p.Description;

                            //Fix old Swagger 1.2 parameter type
                            if (p.In == "form")
                                p.In = "formData";

                            methodOperationParameters.Add(p);
                        }
                    }
                }
            }

            if (apiAttr?.BodyParameter == GenerateBodyParameter.Always
                || (!DisableAutoDtoInBodyParam && apiAttr?.BodyParameter != GenerateBodyParameter.Never))
            {
                if (!HttpMethods.Get.EqualsIgnoreCase(verb) && !HttpMethods.Delete.EqualsIgnoreCase(verb)
                    && !methodOperationParameters.Any(p => "body".EqualsIgnoreCase(p.In)))
                {
                    ParseDefinitions(schemas, operationType, route, verb);

                    var parameter = GetParameter(schemas, operationType, route, verb, "body", "body");

                    if (apiAttr?.IsRequired == true)
                        parameter.Required = true;

                    methodOperationParameters.Add(parameter);
                }
            }

            return methodOperationParameters;
        }

        private OpenApiParameter GetParameter(IDictionary<string, OpenApiSchema> schemas, Type schemaType, string route, string verb, string paramName, string paramIn, 
            string[] enumValues = null, 
            bool isApiMember = false)
        {
            //Compatibility: replace old Swagger ParamType to new Open API 
            if (paramIn == "form") paramIn = "formData";

            if (IsSwaggerScalarType(schemaType))
            {
                return new OpenApiParameter
                {
                    In = paramIn,
                    Name = paramName,
                    Type = GetSwaggerTypeName(schemaType),
                    Format = GetSwaggerTypeFormat(schemaType, route, verb),
                    Enum = enumValues,
                    Nullable = IsRequiredType(schemaType) ? false : (bool?)null,
                    Required = paramIn == "path" ? true : (bool?)null
                };
            }

            if (paramIn != "body" && !isApiMember)
            {
                return new OpenApiParameter
                {
                    In = paramIn,
                    Name = paramName,
                    Type = OpenApiType.String,
                    Required = paramIn == "path" ? true : (bool?)null
                };
            }

            if (IsDictionaryType(schemaType))
            {
                return new OpenApiParameter
                {
                    In = paramIn,
                    Name = paramName,
                    Schema = GetDictionarySchema(schemas, schemaType, route, verb)
                };
            }

            if (IsListType(schemaType))
            {
                return GetListParameter(schemas, schemaType, route, verb, paramName, paramIn, enumValues:enumValues);
            }

            OpenApiSchema openApiSchema;

            if (IsInlineSchema(schemaType))
            {
                openApiSchema = schemas[GetSchemaTypeName(schemaType)];
            }
            else
            {
                openApiSchema = new OpenApiSchema {Ref = "#/definitions/" + GetSchemaTypeName(schemaType)};
            }

            return new OpenApiParameter
            {
                In = paramIn,
                Name = paramName,
                Schema = openApiSchema
            };
        }

        private List<string> GetTags(string path)
        {
            var tagName = GetTagName(path);
            return tagName != null ? new List<string> { tagName } : null;
        }

        private string GetTagName(string path)
        {
            var tags = path.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);

            return tags.Length > 0 ? tags[0] : null;
        }

        private OpenApiParameter GetListParameter(IDictionary<string, OpenApiSchema> schemas, Type listType, string route, string verb, string paramName, string paramIn, 
            string[] enumValues = null)
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
            parameter.Items = GetOpenApiListItems(listItemType, route, verb, enumValues: enumValues);

            return parameter;
        }

        private OpenApiParameter GetAcceptHeaderParameter()
        {
            return new OpenApiParameter
            {
                Type = OpenApiType.String,
                Name = "Accept",
                Description = "Accept Header",
                Enum = new [] { "application/json" },
                In = "header",
                Required = true,
            };
        }
    }
}