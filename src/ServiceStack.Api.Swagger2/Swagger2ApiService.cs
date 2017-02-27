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

namespace ServiceStack.Api.Swagger2
{
    using ServiceStack.Api.Swagger2.Support;

    public static class Swagger2Type
    {
        public const string Array = "array";
        public const string Boolean = "boolean";
        public const string Number = "number";
        public const string Integer = "integer";
        public const string String = "string";
    }

    public static class Swagger2TypeFormat
    {
        public const string Array = "int32";
        public const string Byte = "byte";
        public const string Binary = "binary";
        public const string Date = "date";
        public const string DateTime = "date-time";
        public const string Double = "double";
        public const string Float = "float";
        public const string Int = "int32";
        public const string Long = "int64";
        public const string Password = "password";
    }


    [DataContract]
    public class Swagger2ApiDeclaration
    {
        [DataMember(Name = "swagger")]
        public string Swagger => "2.0";

        [DataMember(Name = "info")]
        public Swagger2Info Info { get; set; }

        [DataMember(Name = "host")]
        public string Host { get; set; }

        [DataMember(Name = "basePath")]
        public string BasePath { get; set; }

        [DataMember(Name = "schemes")]
        public List<string> Schemes { get; set; }

        [DataMember(Name = "consumes")]
        public List<string> Consumes { get; set; }

        [DataMember(Name = "produces")]
        public List<string> Produces { get; set; }

        [DataMember(Name = "paths")]
        public Dictionary<string, Swagger2Path> Paths { get; set; }

        [DataMember(Name = "definitions")]
        public Dictionary<string, Swagger2Schema> Definitions { get; set; }

        [DataMember(Name = "parameters")]
        public Dictionary<string, Swagger2Parameter> Parameters { get; set; }

        [DataMember(Name = "responses")]
        public Dictionary<string, Swagger2Response> Responses { get; set; }

        [DataMember(Name = "securityDefinitions")]
        public Dictionary<string, Swagger2SecuritySchema> SecurityDefinitions { get; set; }

        [DataMember(Name = "security")]
        public Dictionary<string, List<string>> Security { get; set; }

        [DataMember(Name = "tags")]
        public Swagger2Tag Tags { get; set; }

        [DataMember(Name = "externalDocs")]
        public Swagger2ExternalDocumentation ExternalDocs { get; set; }
    }

    [DataContract]
    public class Swagger2Info
    {
        [DataMember(Name = "title")]
        public string Title { get; set; }
        [DataMember(Name = "description")]
        public string Description { get; set; }
        [DataMember(Name = "termsOfServiceUrl")]
        public string TermsOfServiceUrl { get; set; }
        [DataMember(Name = "contact")]
        public Swagger2Contact Contact { get; set; }
        [DataMember(Name = "license")]
        public Swagger2License License { get; set; }
        [DataMember(Name = "version")]
        public string Version { get; set; }
    }

    [DataContract]
    public class Swagger2Contact
    {
        [DataMember(Name = "name")]
        public string Name { get; set; }
        [DataMember(Name = "url")]
        public string Url { get; set; }
        [DataMember(Name = "email")]
        public string Email { get; set; }
    }

    [DataContract]
    public class Swagger2License
    {
        [DataMember(Name = "name")]
        public string Name { get; set; }
        [DataMember(Name = "url")]
        public string Url { get; set; }
    }


    [DataContract]
    public abstract class Swagger2DataTypeFields
    {
        [DataMember(Name = "type")]
        public string Type { get; set; }
        [DataMember(Name = "format")]
        public string Format { get; set; }
        [DataMember(Name = "items")]
        public Dictionary<string, string> Items { get; set; }
        [DataMember(Name = "collectionFormat")]
        public string CollectionFormat { get; set; }
        [DataMember(Name = "default")]
        public string Default { get; set; }
        [DataMember(Name = "maximum")]
        public string Maximum { get; set; }
        [DataMember(Name = "exclusiveMaximum")]
        public string ExclusiveMaximum { get; set; }
        [DataMember(Name = "minimum")]
        public string Minimum { get; set; }
        [DataMember(Name = "exclusiveMinimum")]
        public string ExclusiveMinimum { get; set; }
        [DataMember(Name = "maxLength")]
        public string MaxLength { get; set; }
        [DataMember(Name = "minLength")]
        public string MinLength { get; set; }
        [DataMember(Name = "pattern")]
        public string Pattern { get; set; }
        [DataMember(Name = "maxItems")]
        public string MaxItems { get; set; }
        [DataMember(Name = "minItems")]
        public string MinItems { get; set; }
        [DataMember(Name = "uniqueItems")]
        public bool? UniqueItems { get; set; }
        [DataMember(Name = "enum")]
        public List<string> Enum { get; set; }
        [DataMember(Name = "multipleOf")]
        public string MultipleOf { get; set; }
    }

    [DataContract]
    public class Swagger2Response
    {
        [DataMember(Name = "description")]
        public string Description { get; set; }
        [DataMember(Name = "schema")]
        public Swagger2Schema Schema { get; set; }
        [DataMember(Name = "headers")]
        public Dictionary<string, Swagger2Property> Headers { get; set; }
        [DataMember(Name = "examples")]
        public Dictionary<string, string> Examples { get; set; }
    }

    [DataContract]
    public class Swagger2Schema : Swagger2DataTypeFields
    {
        [DataMember(Name = "$ref")]
        public string Ref { get; set; }
        [DataMember(Name = "discriminator")]
        public string Discriminator { get; set; }
        [DataMember(Name = "readOnly")]
        public bool? ReadOnly { get; set; }
        [DataMember(Name = "xml")]
        public Swagger2XmlObject Xml { get; set; }
        [DataMember(Name = "externalDocs")]
        public Swagger2ExternalDocumentation ExternalDocs { get; set; }
        [DataMember(Name = "example")]
        public string Example { get; set; }

        //TODO: allOf, additionalProperties
        [DataMember(Name = "properties")]
        public OrderedDictionary<string, Swagger2Property> Properties { get; set; }
    }

    [DataContract]
    public class Swagger2Path
    {
        [DataMember(Name = "$ref")]
        public string Ref { get; set; }
        [DataMember(Name = "get")]
        public Swagger2Operation Get { get; set; }
        [DataMember(Name = "put")]
        public Swagger2Operation Put { get; set; }
        [DataMember(Name = "post")]
        public Swagger2Operation Post { get; set; }
        [DataMember(Name = "delete")]
        public Swagger2Operation Delete { get; set; }
        [DataMember(Name = "options")]
        public Swagger2Operation Options { get; set; }
        [DataMember(Name = "head")]
        public Swagger2Operation Head { get; set; }
        [DataMember(Name = "patch")]
        public Swagger2Operation Patch { get; set; }
        [DataMember(Name = "parameters")]
        public List<Swagger2Parameter> Parameters { get; set; }
    }

    [DataContract]
    public class Swagger2Operation
    {
        [DataMember(Name = "tags")]
        public List<string> Tags { get; set; }
        [DataMember(Name = "summary")]
        public string Summary { get; set; }
        [DataMember(Name = "description")]
        public string Description { get; set; }
        [DataMember(Name = "externalDocs")]
        public Swagger2ExternalDocumentation ExternalDocs { get; set; }
        [DataMember(Name = "operationId")]
        public string OperationId { get; set; }
        [DataMember(Name = "consumes")]
        public List<string> Consumes { get; set; }
        [DataMember(Name = "produces")]
        public List<string> Produces { get; set; }
        [DataMember(Name = "parameters")]
        public List<Swagger2Parameter> Parameters { get; set; }
        [DataMember(Name = "responses")]
        public Dictionary<string, Swagger2Response> Responses { get; set; }
        [DataMember(Name = "schemes")]
        public List<string> Schemes { get; set; }
        [DataMember(Name = "deprecated")]
        public bool Deprecated { get; set; }
        [DataMember(Name = "security")]
        public Dictionary<string, List<string>> Security { get; set; }
    }

    [DataContract]
    public class Swagger2Parameter : Swagger2DataTypeFields
    {
        [DataMember(Name = "name")]
        public string Name { get; set; }
        [DataMember(Name = "in")]
        public string In { get; set; }
        [DataMember(Name = "description")]
        public string Description { get; set; }
        [DataMember(Name = "required")]
        public bool Required { get; set; }
        [DataMember(Name = "schema")]
        public Swagger2Schema Schema { get; set; }
        [DataMember(Name = "allowEmptyValue")]
        public bool AllowEmptyValue { get; set; }
    }

    [DataContract]
    public class Swagger2XmlObject
    {
        [DataMember(Name = "name")]
        public string Name { get; set; }
        [DataMember(Name = "namespace")]
        public string Namespace { get; set; }
        [DataMember(Name = "prefix")]
        public string Prefix { get; set; }
        [DataMember(Name = "attribute")]
        public bool Attribute { get; set; }
        [DataMember(Name = "wrapped")]
        public bool Wrapped { get; set; }
    }

    [DataContract]
    public class Swagger2SecuritySchema
    {
        [DataMember(Name = "type")]
        public string Type { get; set; }
        [DataMember(Name = "description")]
        public string Description { get; set; }
        [DataMember(Name = "name")]
        public string Name { get; set; }
        [DataMember(Name = "in")]
        public string In { get; set; }
        [DataMember(Name = "flow")]
        public string Flow { get; set; }
        [DataMember(Name = "authorizationUrl")]
        public string AuthorizationUrl { get; set; }
        [DataMember(Name = "tokenUrl")]
        public string TokenUrl { get; set; }
        [DataMember(Name = "scopes")]
        public Dictionary<string, string> Scopes { get; set; }
    }

    [DataContract]
    public class Swagger2ExternalDocumentation
    {
        [DataMember(Name = "description")]
        public string Description { get; set; }
        [DataMember(Name = "url")]
        public string Url { get; set;  }
    }

    [DataContract]
    public class Swagger2Tag
    {
        [DataMember(Name = "name")]
        public string Name { get; set; }
        [DataMember(Name = "description")]
        public string Description { get; set; }
        [DataMember(Name = "externalDocs")]
        public Swagger2ExternalDocumentation ExternalDocs { get; set; }
    }

    [DataContract]
    public class Swagger2Security
    {
        [DataMember(Name = "name")]
        public string Name { get; set; }
        [DataMember(Name = "description")]
        public string Description { get; set; }
        [DataMember(Name = "externalDocs")]
        public Swagger2ExternalDocumentation ExternalDocs { get; set; }
    }

    [DataContract]
    [Exclude(Feature.Soap)]
    public class Swagger2Resources : IReturn<Swagger2ApiDeclaration>
    {
        [DataMember(Name = "apiKey")]
        public string ApiKey { get; set; }
    }

    [DataContract]
    public class Swagger2Property : Swagger2DataTypeFields
    {
        [DataMember(Name = "$ref")]
        public string Ref { get; set; }
        [DataMember(Name = "description")]
        public string Description { get; set; }
    }

    //old declarations
    /*    [DataContract]
        public class Swagger2Model
        {
            [DataMember(Name = "id")]
            public string Id { get; set; }
            [DataMember(Name = "description")]
            public string Description { get; set; }
            [DataMember(Name = "required")]
            public List<string> Required { get; set; }
            [DataMember(Name = "properties")]
            public OrderedDictionary<string, Swagger2Property> Properties { get; set; }
            [DataMember(Name = "subTypes")]
            public List<string> SubTypes { get; set; }
            [DataMember(Name = "discriminator")]
            public string Discriminator { get; set; }
        }

        [DataContract]
        public class Swagger2Api
        {
            [DataMember(Name = "path")]
            public string Path { get; set; }
            [DataMember(Name = "description")]
            public string Description { get; set; }
            [DataMember(Name = "operations")]
            public List<Swagger2Operation> Operations { get; set; }
        }


        [DataContract]
        public class Swagger2ResponseMessage
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
        public class Swagger2Parameter : Swagger2DataTypeFields
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
        */
    [AddHeader(DefaultContentType = MimeTypes.Json)]
    [DefaultRequest(typeof(Swagger2Resources))]
    [Restrict(VisibilityTo = RequestAttributes.None)]
    public class Swagger2ApiService : Service
    {
        internal static bool UseCamelCaseModelPropertyNames { get; set; }
        internal static bool UseLowercaseUnderscoreModelPropertyNames { get; set; }
        internal static bool DisableAutoDtoInBodyParam { get; set; }

        internal static Action<Swagger2ApiDeclaration> ApiDeclarationFilter { get; set; }
        internal static Action<Swagger2Operation> OperationFilter { get; set; }
        //internal static Action<Swagger2Model> ModelFilter { get; set; }
        internal static Action<Swagger2Property> ModelPropertyFilter { get; set; }

        public object Get(Swagger2Resources request)
        {
            var map = HostContext.ServiceController.RestPathMap;
            var paths = new List<RestPath>();

            var basePath = base.Request.GetBaseUrl();

            var meta = HostContext.Metadata;
            foreach (var key in map.Keys)
            {
                var restPaths = map[key];
                //var selectedPaths = restPaths.Where( x => x.Path == path || x.Path.StartsWith(path + "/"));
                var visiblePaths = restPaths.Where(x => meta.IsVisible(Request, Format.Json, x.RequestType.Name));
                paths.AddRange(visiblePaths);
            }

            var definitions = new Dictionary<string, Swagger2Schema>();
            foreach (var restPath in paths.SelectMany(x => x.Verbs.Select(y => new { Value = x, Verb = y })))
            {
                ParseDefinitions(definitions, restPath.Value.RequestType, restPath.Value.Path, restPath.Verb);
            }

            var apiPaths = ParseOperations(paths, definitions);

            //var apis = paths.Select(p => FormatMethodDescription(p, models))
            //    .ToArray().OrderBy(md => md.Path).ToList();

            var result = new Swagger2ApiDeclaration
            {
                Info = new Swagger2Info()
                {
                    Version = HostContext.Config.ApiVersion,
                },
                Paths = apiPaths,
                BasePath = basePath,
                Schemes = new List<string> { "http", "https" }, //TODO: get https from config
                Host = HostConfig.ServiceStackPath,
                Consumes = new List<string>(){ "application/json"},
                Definitions = definitions
            };

            
            /*if (OperationFilter != null)
                apis.Each(x => x.Operations.Each(OperationFilter));

            if (ApiDeclarationFilter != null)
                ApiDeclarationFilter(result);
                */
            return new HttpResult(result)
            {
                ResultScope = () => JsConfig.With(includeNullValues: false)
            };
        }

        private static readonly Dictionary<Type, string> ClrTypesToSwaggerScalarTypes = new Dictionary<Type, string> {
            {typeof(byte), Swagger2Type.String},
            {typeof(sbyte), Swagger2Type.String},
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
            {typeof(byte), Swagger2TypeFormat.Byte},
            {typeof(sbyte), Swagger2TypeFormat.Byte},
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

        private static string GetSwaggerTypeName(Type type, string route = null, string verb = null)
        {
            var lookupType = Nullable.GetUnderlyingType(type) ?? type;

            return ClrTypesToSwaggerScalarTypes.ContainsKey(lookupType)
                ? ClrTypesToSwaggerScalarTypes[lookupType]
                : GetModelTypeName(lookupType, route, verb);
        }

        private static string GetSwaggerTypeFormat(Type type, string route = null, string verb = null)
        {
            var lookupType = Nullable.GetUnderlyingType(type) ?? type;

            string format = null;
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
            return GetListElementType(type) != null;
        }

        private static bool IsNullable(Type type)
        {
            return type.IsGenericType() && type.GetGenericTypeDefinition() == typeof(Nullable<>);
        }

		private static string GetModelTypeName(Type modelType, string path = null, string verb = null)
		{
		    if (modelType.IsValueType() || modelType.IsNullableType())
		        return Swagger2Type.String;

		    if (!modelType.IsGenericType())
		        return modelType.Name;

            var typeName = modelType.ToPrettyName();
		    return typeName;
		}

        private void ParseResponseModel(IDictionary<string, Swagger2Schema> models, Type modelType)
        {
            ParseDefinitions(models, modelType, null, null);
        }
        

        private void ParseDefinitions(IDictionary<string, Swagger2Schema> models, Type modelType, string route, string verb)
        {
            if (IsSwaggerScalarType(modelType) || modelType.ExcludesFeature(Feature.Metadata)) return;

            var modelId = GetModelTypeName(modelType, route, verb);
            if (models.ContainsKey(modelId)) return;

            var modelTypeName = GetModelTypeName(modelType);
            var model = new Swagger2Schema
            {
                Type = "object",
                //Description = modelType.GetDescription() ?? modelTypeName,
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

                    var propertyType = prop.PropertyType;
                    var modelProp = IsSwaggerScalarType(propertyType) || IsListType(propertyType)
                        ? new Swagger2Property
                        {
                            Type = GetSwaggerTypeName(propertyType, route, verb),
                            Format = GetSwaggerTypeFormat(propertyType, route, verb),
                            Description = prop.GetDescription(),
                        }
                        : new Swagger2Property { Ref = "#/definitions/" + GetModelTypeName(propertyType, route, verb) };

                    if (IsListType(propertyType))
                    {
                        modelProp.Type = Swagger2Type.Array;
                        var listItemType = GetListElementType(propertyType);
                        if (IsSwaggerScalarType(listItemType))
                        {
                            modelProp.Items = new Dictionary<string, string>
                            {
                                { "type", GetSwaggerTypeName(listItemType, route, verb) },
                                { "format", GetSwaggerTypeFormat(listItemType, route, verb) }
                            };
                        } else
                        {
                            modelProp.Items = new Dictionary<string, string> { { "$ref", "#/definitions/" + GetModelTypeName(listItemType, route, verb) } };
                        }
                        ParseDefinitions(models, listItemType, route, verb);
                    }
                    else if ((Nullable.GetUnderlyingType(propertyType) ?? propertyType).IsEnum())
                    {
                        var enumType = Nullable.GetUnderlyingType(propertyType) ?? propertyType;
                        if (enumType.IsNumericType())
                        {
                            var underlyingType = Enum.GetUnderlyingType(enumType);
                            modelProp.Type = GetSwaggerTypeName(underlyingType, route, verb);
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

                        var propAttr = prop.FirstAttribute<ApiMemberAttribute>();
                        if (propAttr != null && propAttr.DataType != null)
                            modelProp.Format = propAttr.DataType;     //modelProp.Type = propAttr.DataType;
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
        }


/*
        private void ParseModel(IDictionary<string, Swagger2Model> models, Type modelType, string route, string verb)
        {
            if (IsSwaggerScalarType(modelType) || modelType.ExcludesFeature(Feature.Metadata)) return;

            var modelId = GetModelTypeName(modelType, route, verb);
            if (models.ContainsKey(modelId)) return;

            var modelTypeName = GetModelTypeName(modelType);
            var model = new Swagger2Model
            {
                Id = modelId,
                Description = modelType.GetDescription() ?? modelTypeName,
                Properties = new OrderedDictionary<string, Swagger2Property>()
            };
            models[model.Id] = model;

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

                    var propertyType = prop.PropertyType;
                    var modelProp = new Swagger2Parameter
                    {
                        Type = GetSwaggerTypeName(propertyType, route, verb),
                        Description = prop.GetDescription(),
                    };

                    if ((propertyType.IsValueType() && !IsNullable(propertyType)) || apiMembers.Any(x => x.IsRequired))
                    {
                        modelProp.Required = true;
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
                    else if ((Nullable.GetUnderlyingType(propertyType) ?? propertyType).IsEnum())
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
  */      
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

            return new Swagger2Schema() { Type = Swagger2Type.String };
        }

        private string GetResponseClass(IRestPath restPath, IDictionary<string, Swagger2Schema> models)
        {
            // Given: class MyDto : IReturn<X>. Determine the type X.
            foreach (var i in restPath.RequestType.GetInterfaces())
            {
                if (i.IsGenericType() && i.GetGenericTypeDefinition() == typeof(IReturn<>))
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

        private Dictionary<string, Swagger2Response> GetMethodResponseCodes(IRestPath restPath, IDictionary<string, Swagger2Schema> models, Type requestType)
        {
            var responses = new Dictionary<string, Swagger2Response>();

            var responseSchema = GetResponseSchema(restPath, models);

            responses.Add("200", new Swagger2Response()
            {
                Schema = responseSchema,
                Description = "TODO: description"
            });
                
            //TODO: order by status code
            foreach (var attr in requestType.AllAttributes<ApiResponseAttribute>())
            {
                responses.Add(attr.StatusCode.ToString(), new Swagger2Response()
                {
                    Description = attr.Description,
                });
            }

            return responses;
        }

        private Dictionary<string, Swagger2Path> ParseOperations(List<RestPath> restPaths, Dictionary<string, Swagger2Schema> models)
        {
            var apiPaths = new Dictionary<string, Swagger2Path>();

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
                    curPath = new Swagger2Path();
                    apiPaths.Add(restPath.Path, curPath);
                }

                foreach (var verb in verbs)
                {
                    var operation = new Swagger2Operation()
                    {
                        Summary = summary,
                        Description = notes,
                        OperationId = requestType.Name,
                        Parameters = ParseParameters(verb, requestType, models, routePath),
                        Responses = GetMethodResponseCodes(restPath, models, requestType),
                        Consumes = new List<string>() { "application/json" },
                        Produces = new List<string>() { "application/json" }
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

/*
            var md = new Swagger2Api
            {
                Path = routePath,
                Description = summary,
                Operations = verbs.Map(verb => new Swagger2Operation
                {
                    OperationId = requestType.Name,
                    Nickname = requestType.Name,
                    Summary = summary,
                    Notes = notes,
                    Parameters = ParseParameters(verb, requestType, models, routePath),
                    ResponseClass = GetResponseClass(restPath, models),
                    Responses = GetMethodResponseCodes(requestType)
                })
            };
            */
            return apiPaths;
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
                        ? "form" 
                        : "query";

                defaultOperationParameters.Add(new Swagger2Parameter {
                    Type = GetSwaggerTypeName(property.PropertyType),
                    Format = GetSwaggerTypeFormat(property.PropertyType),
                    //AllowMultiple = false,
                    Description = property.PropertyType.GetDescription(),
                    Name = propertyName,
                    In = paramType,
                    Required = paramType == "path",
                    Enum = GetEnumValues(allowableValuesAttrs.FirstOrDefault()),
                });
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

            if (!DisableAutoDtoInBodyParam)
            {
                if (!HttpMethods.Get.EqualsIgnoreCase(verb) && !HttpMethods.Delete.EqualsIgnoreCase(verb) 
                    && !methodOperationParameters.Any(p => "body".EqualsIgnoreCase(p.In)))
                {
                    ParseDefinitions(models, operationType, route, verb);
                    methodOperationParameters.Add(new Swagger2Parameter
                    {
                        In = "body",
                        Name = "body",
                        Type = GetSwaggerTypeName(operationType, route, verb),
                        Format = GetSwaggerTypeFormat(operationType, route, verb)
                    });
                }
            }
            return methodOperationParameters;
        }
        

    }
}