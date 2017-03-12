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
using ServiceStack.Api.Swagger2.Support;
using ServiceStack.Api.Swagger2.Specification;

namespace ServiceStack.Api.Swagger2
{
    [DataContract]
    [Exclude(Feature.Soap)]
    public class Swagger2Resources : IReturn<Swagger2ApiDeclaration>
    {
        [DataMember(Name = "apiKey")]
        public string ApiKey { get; set; }
    }

    [AddHeader(DefaultContentType = MimeTypes.Json)]
    [DefaultRequest(typeof(Swagger2Resources))]
    [Restrict(VisibilityTo = RequestAttributes.None)]
    public class Swagger2ApiService : Service
    {
        internal static bool UseCamelCaseModelPropertyNames { get; set; }
        internal static bool UseLowercaseUnderscoreModelPropertyNames { get; set; }
        internal static bool DisableAutoDtoInBodyParam { get; set; }

        internal static Regex resourceFilterRegex;

        internal static Action<Swagger2ApiDeclaration> ApiDeclarationFilter { get; set; }
        internal static Action<string, Swagger2Operation> OperationFilter { get; set; }
        internal static Action<Swagger2Schema> ModelFilter { get; set; }
        internal static Action<Swagger2Property> ModelPropertyFilter { get; set; }

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

            var definitions = new Dictionary<string, Swagger2Schema>() {
                { "Object",  new Swagger2Schema() {Description = "Object", Type = Swagger2Type.Object, Properties = new OrderedDictionary<string, Swagger2Property>() } }
            };

            foreach (var restPath in paths.SelectMany(x => x.Verbs.Select(y => new { Value = x, Verb = y })))
            {
                ParseDefinitions(definitions, restPath.Value.RequestType, restPath.Value.Path, restPath.Verb);
            }

            var tags = new List<Swagger2Tag>();
            var apiPaths = ParseOperations(paths, definitions, tags);

            var result = new Swagger2ApiDeclaration
            {
                Info = new Swagger2Info()
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

        private IEnumerable<Tuple<string, Swagger2Operation>> GetOperations(Swagger2Path value)
        {
            if (value.Get != null) yield return new Tuple<string, Swagger2Operation>("GET", value.Get);
            if (value.Post != null) yield return new Tuple<string, Swagger2Operation>("POST", value.Post);
            if (value.Put != null) yield return new Tuple<string, Swagger2Operation>("PUT", value.Put);
            if (value.Patch != null) yield return new Tuple<string, Swagger2Operation>("PATCH", value.Patch);
            if (value.Delete != null) yield return new Tuple<string, Swagger2Operation>("DELETE", value.Delete);
            if (value.Head != null) yield return new Tuple<string, Swagger2Operation>("HEAD", value.Head);
            if (value.Options != null) yield return new Tuple<string, Swagger2Operation>("OPTIONS", value.Options);
        }

        private static readonly Dictionary<Type, string> ClrTypesToSwaggerScalarTypes = new Dictionary<Type, string> {
            {typeof(byte[]), Swagger2Type.String},
            {typeof(sbyte[]), Swagger2Type.String},
            {typeof(byte), Swagger2Type.Integer},
            {typeof(sbyte), Swagger2Type.Integer},
            {typeof(bool), Swagger2Type.Boolean},
            {typeof(short), Swagger2Type.Integer},
            {typeof(ushort), Swagger2Type.Integer},
            {typeof(int), Swagger2Type.Integer},
            {typeof(uint), Swagger2Type.Integer},
            {typeof(long), Swagger2Type.Integer},
            {typeof(ulong), Swagger2Type.Integer},
            {typeof(float), Swagger2Type.Number},
            {typeof(double), Swagger2Type.Number},
            {typeof(decimal), Swagger2Type.Number},
            {typeof(string), Swagger2Type.String},
            {typeof(DateTime), Swagger2Type.String}
        };

        private static readonly Dictionary<Type, string> ClrTypesToSwaggerScalarFormats = new Dictionary<Type, string> {
            {typeof(byte[]), Swagger2TypeFormat.Byte},
            {typeof(sbyte[]), Swagger2TypeFormat.Byte},
            {typeof(byte), Swagger2TypeFormat.Int},
            {typeof(sbyte), Swagger2TypeFormat.Int},
            {typeof(short), Swagger2TypeFormat.Int},
            {typeof(ushort), Swagger2TypeFormat.Int},
            {typeof(int), Swagger2TypeFormat.Int},
            {typeof(uint), Swagger2TypeFormat.Int},
            {typeof(long), Swagger2TypeFormat.Long},
            {typeof(ulong), Swagger2TypeFormat.Long},
            {typeof(float), Swagger2TypeFormat.Float},
            {typeof(double), Swagger2TypeFormat.Double},
            {typeof(decimal), Swagger2TypeFormat.Double},
            {typeof(DateTime), Swagger2TypeFormat.DateTime}
        };


        private static bool IsSwaggerScalarType(Type type)
        {
            return ClrTypesToSwaggerScalarTypes.ContainsKey(type) 
                || (Nullable.GetUnderlyingType(type) ?? type).IsEnum()
                || type.IsValueType()
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
                return Swagger2TypeFormat.Binary;

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

        private Swagger2Schema GetDictionaryModel(IDictionary<string, Swagger2Schema> models, Type modelType, string route, string verb)
        {
            if (!IsDictionaryType(modelType))
                return null;

            var valueType = modelType.GetTypeGenericArguments()[1];

            ParseDefinitions(models, valueType, route, verb);

            return new Swagger2Schema()
            {
                Type = Swagger2Type.Object,
                Description = modelType.GetDescription() ?? GetModelTypeName(modelType),
                AdditionalProperties = GetSwaggerProperty(models, valueType, route, verb)
            };
        }

        private static bool IsNullable(Type type)
        {
            return type.IsGenericType() && type.GetGenericTypeDefinition() == typeof(Nullable<>);
        }

		private static string GetModelTypeName(Type modelType)
		{
		    if (modelType.IsValueType() || modelType.IsNullableType())
		        return Swagger2Type.String;

		    if (!modelType.IsGenericType())
		        return modelType.Name;

            var typeName = modelType.ToPrettyName();
		    return typeName;
		}

        private Swagger2Property GetSwaggerProperty(IDictionary<string, Swagger2Schema> models, Type propertyType, string route, string verb)
        {
            var modelProp = IsSwaggerScalarType(propertyType) || IsListType(propertyType)
                ? new Swagger2Property
                {
                    Type = GetSwaggerTypeName(propertyType),
                    Format = GetSwaggerTypeFormat(propertyType, route, verb)
                }
                : new Swagger2Property { Ref = "#/definitions/" + GetModelTypeName(propertyType) };


            if (IsListType(propertyType))
            {
                modelProp.Type = Swagger2Type.Array;
                var listItemType = GetListElementType(propertyType);
                if (IsSwaggerScalarType(listItemType))
                {
                    modelProp.Items = new Dictionary<string, string>
                            {
                                { "type", GetSwaggerTypeName(listItemType) },
                                { "format", GetSwaggerTypeFormat(listItemType, route, verb) }
                            };
                }
                else
                {
                    modelProp.Items = new Dictionary<string, string> { { "$ref", "#/definitions/" + GetModelTypeName(listItemType) } };
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
                    modelProp.Type = Swagger2Type.String;
                    modelProp.Enum = Enum.GetNames(enumType).ToList();
                }
            }
            else
            {
                ParseDefinitions(models, propertyType, route, verb);
            }

            return modelProp;
        }

        private void ParseResponseModel(IDictionary<string, Swagger2Schema> models, Type modelType)
        {
            ParseDefinitions(models, modelType, null, null);
        }

        private void ParseDefinitions(IDictionary<string, Swagger2Schema> models, Type modelType, string route, string verb)
        {
            if (IsSwaggerScalarType(modelType) || modelType.ExcludesFeature(Feature.Metadata)) return;

            var modelId = GetModelTypeName(modelType);
            if (models.ContainsKey(modelId)) return;

            var model = GetDictionaryModel(models, modelType, route, verb);
                
            model = model ?? new Swagger2Schema
            {
                Type = Swagger2Type.Object,
                Description = modelType.GetDescription() ?? GetModelTypeName(modelType),
                Properties = new OrderedDictionary<string, Swagger2Property>()
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

        private Swagger2Schema GetResponseSchema(IRestPath restPath, IDictionary<string, Swagger2Schema> models)
        {
            // Given: class MyDto : IReturn<X>. Determine the type X.
            foreach (var i in restPath.RequestType.GetInterfaces())
            {
                if (i.IsGenericType() && i.GetGenericTypeDefinition() == typeof(IReturn<>))
                {
                    var returnType = i.GetGenericArguments()[0];

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
                        var schema = new Swagger2Schema()
                        {
                            Type = SwaggerType.Array,
                        };
                        var listItemType = GetListElementType(returnType);
                        ParseResponseModel(models, listItemType);
                        if (IsSwaggerScalarType(listItemType))
                        {
                            schema.Items = new Dictionary<string, string>
                            {
                                { "type", GetSwaggerTypeName(listItemType) },
                                { "format", GetSwaggerTypeFormat(listItemType) }
                            };
                        }
                        else
                        {
                            schema.Items = new Dictionary<string, string> { { "$ref", "#/definitions/" + GetModelTypeName(listItemType) } };
                        }

                        return schema;
                    }
                    ParseResponseModel(models, returnType);

                    var genericArgType = i.GetGenericArguments()[0];
                    if (IsSwaggerScalarType(genericArgType))
                    {
                        return new Swagger2Schema()
                        {
                            Type = GetSwaggerTypeName(genericArgType),
                            Format = GetSwaggerTypeFormat(genericArgType)
                        };
                    }

                    return new Swagger2Schema()
                    {
                        Ref = "#/definitions/" + GetModelTypeName(genericArgType)
                    };
                }
            }

            return new Swagger2Schema() { Ref = "#/definitions/Object" };
        }

        private OrderedDictionary<string, Swagger2Response> GetMethodResponseCodes(IRestPath restPath, IDictionary<string, Swagger2Schema> models, Type requestType)
        {
            var responses = new OrderedDictionary<string, Swagger2Response>();

            var responseSchema = GetResponseSchema(restPath, models);

            responses.Add("default", new Swagger2Response()
            {
                Schema = responseSchema,
                Description = String.Empty //TODO: description
            });
                
            foreach (var attr in requestType.AllAttributes<ApiResponseAttribute>())
            {
                responses.Add(attr.StatusCode.ToString(), new Swagger2Response()
                {
                    Description = attr.Description,
                });
            }

            return responses;
        }

        private OrderedDictionary<string, Swagger2Path> ParseOperations(List<RestPath> restPaths, Dictionary<string, Swagger2Schema> models, List<Swagger2Tag> tags)
        {
            var apiPaths = new OrderedDictionary<string, Swagger2Path>();

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

                Swagger2Path curPath;

                if (!apiPaths.TryGetValue(restPath.Path, out curPath))
                {
                    curPath = new Swagger2Path()
                    {
                        Parameters = new List<Swagger2Parameter>() { GetFormatJsonParameter() }
                    };
                    apiPaths.Add(restPath.Path, curPath);

                    tags.Add(new Swagger2Tag() { Name = restPath.Path, Description = summary });
                }

                foreach (var verb in verbs)
                {
                    var operation = new Swagger2Operation()
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
        
        private List<Swagger2Parameter> ParseParameters(string verb, Type operationType, IDictionary<string, Swagger2Schema> models, string route)
        {
            var hasDataContract = operationType.HasAttribute<DataContractAttribute>();

            var properties = operationType.GetProperties();
            var paramAttrs = new Dictionary<string, ApiMemberAttribute[]>();
            var allowableParams = new List<ApiAllowableValuesAttribute>();
            var defaultOperationParameters = new List<Swagger2Parameter>();

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


                var parameter = new Swagger2Parameter
                {
                    Type = GetSwaggerTypeName(property.PropertyType),
                    Format = GetSwaggerTypeFormat(property.PropertyType, route, verb),
                    //AllowMultiple = false,
                    Description = property.PropertyType.GetDescription(),
                    Name = propertyName,
                    In = paramType,
                    Required = paramType == "path",
                    Enum = GetEnumValues(allowableValuesAttrs.FirstOrDefault()),
                };

                if (!IsSwaggerScalarType(property.PropertyType) && !IsListType(property.PropertyType))
                {
                    parameter.Type = null;
                    parameter.Format = null;
                    parameter.Schema = new Swagger2Schema() { Ref = "#/definitions/" + GetModelTypeName(property.PropertyType) };
                }
                else if (IsListType(property.PropertyType))
                {
                    parameter = new Swagger2Parameter
                    {
                        Type = Swagger2Type.Array,
                        CollectionFormat = "multi",
                        Description = property.PropertyType.GetDescription(),
                        Name = propertyName,
                        In = paramType,
                        Required = paramType == "path"
                    };

                    var listItemType = GetListElementType(property.PropertyType);
                    if (IsSwaggerScalarType(listItemType))
                    {
                        parameter.Items = new Dictionary<string, string>
                        {
                            { "type", GetSwaggerTypeName(listItemType) },
                            { "format", GetSwaggerTypeFormat(listItemType, route, verb) }
                        };
                    }
                    else
                    {
                        parameter.Items = new Dictionary<string, string> { { "$ref", "#/definitions/" + GetModelTypeName(listItemType) } };
                    }
                    ParseDefinitions(models, listItemType, route, verb);
                }

                defaultOperationParameters.Add(parameter);
            }

            var methodOperationParameters = defaultOperationParameters;
            if (hasApiMembers)
            {
                methodOperationParameters = new List<Swagger2Parameter>();
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
                            methodOperationParameters.Add(new Swagger2Parameter
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

            //FIX: this is commented, because it breaks validation of swagger2 schema
            /*if (!DisableAutoDtoInBodyParam)
            {
                if (!HttpMethods.Get.EqualsIgnoreCase(verb) && !HttpMethods.Delete.EqualsIgnoreCase(verb) 
                    && !methodOperationParameters.Any(p => "body".EqualsIgnoreCase(p.In)))
                {
                    ParseDefinitions(models, operationType, route, verb);
                    methodOperationParameters.Add(new Swagger2Parameter
                    {
                        In = "body",
                        Name = "body",
                        Type = GetSwaggerTypeName(operationType),
                        Format = GetSwaggerTypeFormat(operationType, route, verb)
                    });
                }
            }*/
            return methodOperationParameters;
        }
        
        private Swagger2Parameter GetFormatJsonParameter()
        {
            return new Swagger2Parameter()
            {
                Type = Swagger2Type.String,
                Name = "format",
                Description = "Specifies response output format",
                Default = "json",
                In = "query",
            };
        }

    }
}