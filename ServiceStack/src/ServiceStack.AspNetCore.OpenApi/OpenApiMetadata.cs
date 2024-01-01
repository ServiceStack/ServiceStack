using System.Net;
using System.Reflection;
using System.Runtime.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using ServiceStack.Host;
using ServiceStack.NativeTypes;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack.AspNetCore.OpenApi;

public static class OpenApiSecurity
{
    public static OpenApiSecurityRequirement BasicAuth { get; } = new()
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = BasicAuthenticationHandler.Scheme
                }
            },
            Array.Empty<string>()
        }
    };
    public static OpenApiSecurityScheme BasicAuthScheme { get; set; } = new()
    {
        In = ParameterLocation.Header,
        Name = "Authorization",
        Description = "HTTP Basic access authentication",
        Type = SecuritySchemeType.Http,
        Scheme = BasicAuthenticationHandler.Scheme,
    };

    public static OpenApiSecurityRequirement JwtBearer { get; } = new()
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = JwtBearerDefaults.AuthenticationScheme,
                }
            },
            Array.Empty<string>()
        }
    };
    public static OpenApiSecurityScheme JwtBearerScheme { get; set; } = new()
    {
        In = ParameterLocation.Header,
        Name = "Authorization",
        Description = "JWT Bearer Authorization",
        Type = SecuritySchemeType.Http,
        BearerFormat = "JWT",
        Scheme = JwtBearerDefaults.AuthenticationScheme,
    };
}

public class OpenApiMetadata
{
    public List<Type> DocumentFilterTypes { get; set; } = [
        typeof(ServiceStackDocumentFilter),
    ];
    public List<Type> SchemaFilterTypes { get; set; } = [
    ];
    
    public static Action<string, OpenApiOperation>? OperationFilter { get; set; }
    public static Action<OpenApiOperation>? SchemaFilter { get; set; }
    public static Action<OpenApiSchema>? SchemaPropertyFilter { get; set; }
    
    private static readonly Dictionary<Type, string> ClrTypesToSwaggerScalarTypes = new()
    {
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

    private static readonly Dictionary<Type, string> ClrTypesToSwaggerScalarFormats = new()
    {
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

    public Dictionary<string, OpenApiSchema> Schemas { get; } = new();
    internal static List<string> InlineSchemaTypesInNamespaces { get; set; } = new();
    
    public OpenApiSecurityScheme? SecurityDefinition { get; set; }
    public OpenApiSecurityRequirement? SecurityRequirement { get; set; }

    public void AddBasicAuth()
    {
        SecurityDefinition = OpenApiSecurity.BasicAuthScheme;
        SecurityRequirement = OpenApiSecurity.BasicAuth;
    }

    public void AddJwtBearer()
    {
        SecurityDefinition = OpenApiSecurity.JwtBearerScheme;
        SecurityRequirement = OpenApiSecurity.JwtBearer;
    }

    public OpenApiOperation AddOperation(OpenApiOperation op, Operation operation, string verb, string route)
    {
        //Console.WriteLine($"AddOperation {verb} {route} {operation.RequestType.Name}...");

        // Response is handled by Endpoints Metadata
        op.Summary = operation.RequestType.GetDescription();
        op.Description = operation.RequestType.FirstAttribute<NotesAttribute>()?.Notes;

        var hasRequestBody = HttpUtils.HasRequestBody(verb);
        if (!hasRequestBody)
        {
            var parameters = CreateParameters(operation.RequestType, route, verb);
            op.Parameters.AddDistinctRange(parameters);
        }
        
        var apiAttr = operation.RequestType.FirstAttribute<ApiAttribute>();
        if (hasRequestBody)
        {
            var openApiType = CreateSchema(operation.RequestType, route, verb);
            if (openApiType != null)
            {
                // Move path parameters from body
                var inPaths = new List<string>();
                foreach (var entry in openApiType.Properties)
                {
                    var inPath = route.Contains("{" + entry.Key + "}", StringComparison.OrdinalIgnoreCase);
                    if (inPath)
                    {
                        inPaths.Add(entry.Key);
                        OpenApiSchema? prop = entry.Value;
                        op.Parameters.Add(new OpenApiParameter
                        {
                            Name = entry.Key,
                            In = ParameterLocation.Path,
                            Required = true,
                            Schema = prop,
                            Style = ParameterStyle.Simple,
                            Explode = true,
                        });
                    }
                }
                foreach (var propName in inPaths)
                {
                    openApiType.Properties.Remove(propName);
                }
                
                var formType = new OpenApiMediaType
                {
                    Schema = openApiType,
                };
                foreach (var entry in openApiType.Properties)
                {
                    formType.Encoding[entry.Key] = new OpenApiEncoding { Style = ParameterStyle.Form, Explode = false };
                }
                op.RequestBody = new()
                {
                    Content = {
                        [MimeTypes.MultiPartFormData] = formType
                    }
                };
                if (apiAttr?.BodyParameter != GenerateBodyParameter.Never)
                {
                    op.RequestBody.Content[MimeTypes.Json] = new OpenApiMediaType
                    {
                        Schema = new()
                        {
                            Reference = ToOpenApiReference(operation.RequestType),
                        }
                    };
                }
            }
        }
        
        if (operation.RequiresAuthentication)
        {
            if (SecurityRequirement != null)
                op.Security.Add(SecurityRequirement);
        }
        var userTags = operation.RequestType.AllAttributes<TagAttribute>().Map(x => x.Name);
        if (userTags.Count > 0)
        {
            userTags.Each(tag => op.Tags.Add(new OpenApiTag { Name = tag }));
        }
        
        return op;
    }

    internal static OpenApiReference ToOpenApiReference(Type type) =>
        new() {
            Type = ReferenceType.Schema,
            Id = GetSchemaDefinitionRef(type),
        };

    private static bool IsKeyValuePairType(Type type)
    {
        return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(KeyValuePair<,>);
    }

    private static bool IsSwaggerScalarType(Type type)
    {
        var lookupType = Nullable.GetUnderlyingType(type) ?? type;

        return ClrTypesToSwaggerScalarTypes.ContainsKey(lookupType)
               || lookupType.IsEnum
               || (lookupType.IsValueType && !IsKeyValuePairType(lookupType));
    }

    private static string GetSwaggerTypeName(Type type)
    {
        var lookupType = Nullable.GetUnderlyingType(type) ?? type;

        return ClrTypesToSwaggerScalarTypes.TryGetValue(lookupType, out var scalarType)
            ? scalarType
            : GetSchemaTypeName(lookupType);
    }

    private static string GetSwaggerTypeFormat(Type type)
    {
        var lookupType = Nullable.GetUnderlyingType(type) ?? type;
        return ClrTypesToSwaggerScalarFormats.GetValueOrDefault(lookupType);
    }

    private OpenApiSchema? GetListSchema(Type schemaType)
    {
        if (!IsListType(schemaType))
            return null;

        var listItemType = GetListElementType(schemaType);
        return new OpenApiSchema
        {
            Title = GetSchemaTypeName(schemaType),
            Type = OpenApiType.Array,
            Items = new()
            {
                Type = listItemType != null && IsSwaggerScalarType(listItemType)
                    ? GetSwaggerTypeName(listItemType) 
                    : null, 
                Reference = listItemType != null && !IsSwaggerScalarType(listItemType)
                    ? ToOpenApiReference(listItemType)
                    : null,
            },
        };
    }

    private static bool IsDictionaryType(Type type)
    {
        if (!type.IsGenericType) return false;
        var genericType = type.GetGenericTypeDefinition();
        return genericType == typeof(Dictionary<,>)
           || genericType == typeof(IDictionary<,>)
           || genericType == typeof(IReadOnlyDictionary<,>)
           || genericType == typeof(SortedDictionary<,>);
    }

    private OpenApiSchema? CreateDictionarySchema(Type schemaType)
    {
        if (!IsDictionaryType(schemaType))
            return null;

        var valueType = schemaType.GetGenericArguments()[1];

        return new OpenApiSchema
        {
            Title = GetSchemaTypeName(schemaType),
            Type = OpenApiType.Object,
            Description = schemaType.GetDescription() ?? GetSchemaTypeName(schemaType),
            AdditionalProperties = GetOpenApiProperty(valueType)
        };
    }    

    private OpenApiSchema? GetKeyValuePairSchema(Type schemaType)
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
            Properties = new OrderedDictionary<string, OpenApiSchema>
            {
                ["Key"] = GetOpenApiProperty(keyType),
                ["Value"] = GetOpenApiProperty(valueType),
            }
        };
    }

    private static bool IsRequiredType(Type type)
    {
        return !type.IsNullableType() && type != typeof(string);
    }

    public static string GetSchemaTypeName(Type schemaType)
    {
        if (schemaType.IsEnum)
            return schemaType.Name;
        
        if ((!IsKeyValuePairType(schemaType) && schemaType.IsValueType) || schemaType.IsNullableType())
            return OpenApiType.String;

        if (!schemaType.IsGenericType)
            return schemaType.Name;

        var typeName = schemaType.ToPrettyName();
        return typeName;
    }

    private static string[]? GetEnumValues(ApiAllowableValuesAttribute? attr)
    {
        return attr?.Values?.ToArray();
    }

    private static Type? GetListElementType(Type type)
    {
        if (type.IsArray) 
            return type.GetElementType();
        if (!type.IsGenericType) 
            return null;
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

    private List<OpenApiParameter> CreateParameters(Type operationType, string route, string verb)
    {
        var hasDataContract = operationType.HasAttribute<DataContractAttribute>();

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
            var allowableValuesAttrs = property.AllAttributes<ApiAllowableValuesAttribute>();
            var allowableValuesAttr = allowableValuesAttrs.FirstOrDefault(); 
            allowableParams.AddRange(allowableValuesAttrs);

            if (hasDataContract && attr == null)
                continue;

            var inPath = route.Contains("{" + propertyName + "}", StringComparison.OrdinalIgnoreCase);
            var paramLocation = inPath
                ? ParameterLocation.Path
                : ParameterLocation.Query;

            var parameter = CreateParameter(property.PropertyType,
                propertyName, 
                paramLocation,
                enumValues: GetEnumValues(allowableValuesAttr));

            defaultOperationParameters.Add(parameter);
        }

        var methodOperationParameters = defaultOperationParameters;
        if (hasApiMembers)
        {
            methodOperationParameters = [];
            foreach (var key in paramAttrs.Keys)
            {
                var apiMembers = paramAttrs[key];
                foreach (var member in apiMembers)
                {
                    if ((member.Verb == null || string.Compare(member.Verb, verb, StringComparison.OrdinalIgnoreCase) == 0)
                        && (member.Route == null || route.StartsWith(member.Route))
                        && !string.Equals(member.ParameterType, "model")
                        && methodOperationParameters.All(x => x.Name != (member.Name ?? key)))
                    {
                        var allowableValuesAttr = allowableParams.FirstOrDefault(attr => attr.Name == (member.Name ?? key)); 
                        var p = CreateParameter(propertyTypes[key],
                            member.Name ?? key,
                            GetParamLocation(member.GetParamType(operationType, member.Verb ?? verb)),
                            enumValues: GetEnumValues(allowableValuesAttr),
                            isApiMember:true);
                        // p.Type = member.DataType ?? p.Type;
                        // p.Format = member.Format ?? p.Format;
                        p.Required = p.In == ParameterLocation.Path || member.IsRequired;
                        p.Description = member.Description ?? p.Description;

                        methodOperationParameters.Add(p);
                    }
                }
            }
        }

        return methodOperationParameters;
    }

    private OpenApiParameter CreateParameter(Type propType, string paramName, 
        ParameterLocation? paramLocation, 
        string[]? enumValues = null, 
        bool isApiMember = false)
    {
        if (propType.IsEnum)
        {
            return new OpenApiParameter
            {
                In = paramLocation,
                Name = paramName,
                Reference = ToOpenApiReference(propType),
                Required = paramLocation == ParameterLocation.Path,
            };
        }
        
        if (IsSwaggerScalarType(propType))
        {
            return new OpenApiParameter
            {
                In = paramLocation,
                Name = paramName,
                Schema = new()
                {
                    Type = GetSwaggerTypeName(propType), 
                    Enum = enumValues?.Select(x => new OpenApiString(x)).Cast<IOpenApiAny>().ToList() ?? [],
                    Nullable = !IsRequiredType(propType),
                    Format = GetSwaggerTypeFormat(propType), 
                },
                Required = paramLocation == ParameterLocation.Path,
            };
        }

        if (!isApiMember)
        {
            return new OpenApiParameter
            {
                In = paramLocation,
                Name = paramName,
                Schema = new()
                {
                    Type = OpenApiType.String,
                },
                Required = paramLocation == ParameterLocation.Path,
            };
        }

        if (IsDictionaryType(propType))
        {
            return new OpenApiParameter
            {
                In = paramLocation,
                Name = paramName,
                Schema = CreateDictionarySchema(propType)
            };
        }

        if (IsListType(propType))
        {
            return CreateArrayParameter(propType, paramName, paramLocation);
        }

        OpenApiSchema openApiSchema;

        if (IsInlineSchema(propType))
        {
            openApiSchema = CreateSchema(propType);
        }
        else
        {
            openApiSchema = new OpenApiSchema {
                Reference = ToOpenApiReference(propType)
            };
        }

        return new OpenApiParameter
        {
            In = paramLocation,
            Name = paramName,
            Schema = openApiSchema
        };
    }

    private static ParameterLocation? GetParamLocation(string paramIn)
    {
        ParameterLocation? paramLocation = paramIn switch
        {
            "query" => ParameterLocation.Query,
            "header" => ParameterLocation.Header,
            "path" => ParameterLocation.Path,
            "cookie" => ParameterLocation.Cookie,
            _ => null,
        };
        return paramLocation;
    }

    private static readonly char[] pathSep = { '/' };
    private string? GetTagName(string path)
    {
        var tags = path.Split(pathSep, StringSplitOptions.RemoveEmptyEntries);
        return tags.Length > 0 ? tags[0] : null;
    }

    private OpenApiParameter CreateArrayParameter(Type listType, 
        string paramName, 
        ParameterLocation? paramLocation)
    {
        var listItemType = GetListElementType(listType);
        var parameter = new OpenApiParameter
        {
            In = paramLocation,
            Schema = new() {
                Type = OpenApiType.Array,
                Items = new()
                {
                    Type = listItemType != null && IsSwaggerScalarType(listItemType)
                        ? GetSwaggerTypeName(listItemType) 
                        : null, 
                    Reference = listItemType != null && !IsSwaggerScalarType(listItemType)
                        ? ToOpenApiReference(listItemType)
                        : null,
                }
            },
            Description = listType.GetDescription(),
            Name = paramName,
            Required = paramLocation == ParameterLocation.Path,
            Style = ParameterStyle.Form,
            Explode = true,
        };
        return parameter;
    }

    private static string GetSchemaDefinitionRef(Type schemaType) => GetSchemaTypeName(schemaType);

    private OpenApiSchema GetOpenApiProperty(PropertyInfo pi)
    {
        var schema = GetOpenApiProperty(pi.PropertyType);
        schema.Nullable = pi.IsAssignableToNull();
        return schema;
    }
    
    private OpenApiSchema GetOpenApiProperty(Type propertyType)
    {
        var schemaProp = new OpenApiSchema {
            Nullable = propertyType.IsNullableType(),
        };

        propertyType = Nullable.GetUnderlyingType(propertyType) ?? propertyType;
        
        if (IsKeyValuePairType(propertyType))
        {
            if (IsInlineSchema(propertyType))
            {
                var schema = CreateSchema(propertyType);
                if (schema != null) InlineSchema(schema, schemaProp);
            }
            else
            {
                schemaProp.Reference = ToOpenApiReference(propertyType);
            }
        }
        else if (IsListType(propertyType))
        {
            schemaProp.Type = OpenApiType.Array;
            var listItemType = GetListElementType(propertyType);
            if (listItemType == null) return schemaProp;
            if (IsSwaggerScalarType(listItemType))
            {
                schemaProp.Items = new OpenApiSchema {
                    Type = GetSwaggerTypeName(listItemType),
                    Format = GetSwaggerTypeFormat(listItemType),
                };
                if (IsRequiredType(listItemType))
                {
                    schemaProp.Nullable = false;
                }
            }
            else if (IsInlineSchema(listItemType))
            {
                var schema = CreateSchema(listItemType);
                if (schema != null) InlineSchema(schema, schemaProp);
            }
            else
            {
                schemaProp.Items = new OpenApiSchema
                {
                    Reference = ToOpenApiReference(listItemType)
                };
            }
        }
        else if (IsDictionaryType(propertyType))
        {
            schemaProp = CreateDictionarySchema(propertyType);
        }
        else if (propertyType.IsEnum)
        {
            schemaProp.Reference = ToOpenApiReference(propertyType);
        }
        else if (IsSwaggerScalarType(propertyType))
        {
            schemaProp.Type = GetSwaggerTypeName(propertyType);
            schemaProp.Format = GetSwaggerTypeFormat(propertyType);
            schemaProp.Nullable = !IsRequiredType(propertyType);
            //schemaProp.Required = IsRequiredType(propertyType) ? true : (bool?)null;
        }
        else if (IsInlineSchema(propertyType))
        {
            var schema = CreateSchema(propertyType);
            if (schema != null) InlineSchema(schema, schemaProp);
        }
        else
        {
            //CreateSchema(propertyType, route, verb);
            schemaProp.Reference = ToOpenApiReference(propertyType);
        }

        return schemaProp;
    }

    public static OpenApiSchema CreateEnumSchema(Type propertyType)
    {
        var enumType = Nullable.GetUnderlyingType(propertyType) ?? propertyType;
        if (!enumType.IsEnum)
            throw new ArgumentException(propertyType.Name + " is not an enum", nameof(propertyType));
        
        var schema = new OpenApiSchema();
        if (enumType.IsNumericType())
        {
            var underlyingType = Enum.GetUnderlyingType(enumType);
            schema.Type = GetSwaggerTypeName(underlyingType);
            schema.Format = GetSwaggerTypeFormat(underlyingType);
            schema.Enum = GetNumericValues(enumType, underlyingType).ToOpenApiEnums();
        }
        else
        {
            schema.Type = OpenApiType.String;
            schema.Enum = Enum.GetNames(enumType).ToOpenApiEnums();
        }
        return schema;
    }

    private static void InlineSchema(OpenApiSchema schemaProp, OpenApiSchema schema)
    {
        schemaProp.Title = schema?.Title ?? schemaProp.Title;
        schemaProp.Type = schema?.Type ?? schemaProp.Type;
        schemaProp.Format = schema?.Format ?? schemaProp.Format;
        schemaProp.Description = schema?.Description ?? schemaProp.Description;
        schemaProp.Maximum = schema?.Maximum ?? schemaProp.Maximum;
        schemaProp.ExclusiveMaximum = schema?.ExclusiveMaximum ?? schemaProp.ExclusiveMaximum;
        schemaProp.Minimum = schema?.Minimum ?? schemaProp.Minimum;
        schemaProp.ExclusiveMinimum = schema?.ExclusiveMinimum ?? schemaProp.ExclusiveMinimum;
        schemaProp.MaxLength = schema?.MaxLength ?? schemaProp.MaxLength;
        schemaProp.MinLength = schema?.MinLength ?? schemaProp.MinLength;
        schemaProp.Pattern = schema?.Pattern ?? schemaProp.Pattern;
        schemaProp.MultipleOf = schema?.MultipleOf ?? schemaProp.MultipleOf;
        schemaProp.Default = OpenApiAnyCloneHelper.CloneFromCopyConstructor(schema?.Default);
        schemaProp.ReadOnly = schema?.ReadOnly ?? schemaProp.ReadOnly;
        schemaProp.WriteOnly = schema?.WriteOnly ?? schemaProp.WriteOnly;
        schemaProp.AllOf = schema?.AllOf != null ? new List<OpenApiSchema>(schema.AllOf) : null;
        schemaProp.OneOf = schema?.OneOf != null ? new List<OpenApiSchema>(schema.OneOf) : null;
        schemaProp.AnyOf = schema?.AnyOf != null ? new List<OpenApiSchema>(schema.AnyOf) : null;
        schemaProp.Not = schema?.Not != null ? new(schema.Not) : null;
        schemaProp.Required = schema?.Required != null ? new HashSet<string>(schema.Required) : null;
        schemaProp.Items = schema?.Items != null ? new(schema.Items) : null;
        schemaProp.MaxItems = schema?.MaxItems ?? schemaProp.MaxItems;
        schemaProp.MinItems = schema?.MinItems ?? schemaProp.MinItems;
        schemaProp.UniqueItems = schema?.UniqueItems ?? schemaProp.UniqueItems;
        schemaProp.Properties = schema?.Properties != null ? new Dictionary<string, OpenApiSchema>(schema.Properties) : null;
        schemaProp.MaxProperties = schema?.MaxProperties ?? schemaProp.MaxProperties;
        schemaProp.MinProperties = schema?.MinProperties ?? schemaProp.MinProperties;
        schemaProp.AdditionalPropertiesAllowed = schema?.AdditionalPropertiesAllowed ?? schemaProp.AdditionalPropertiesAllowed;
        schemaProp.AdditionalProperties = new(schema?.AdditionalProperties);
        schemaProp.Discriminator = schema?.Discriminator != null ? new(schema.Discriminator) : null;
        schemaProp.Example = OpenApiAnyCloneHelper.CloneFromCopyConstructor(schema?.Example);
        schemaProp.Enum = schema?.Enum != null ? new List<IOpenApiAny>(schema.Enum) : null;
        schemaProp.Nullable = schema?.Nullable ?? schemaProp.Nullable;
        schemaProp.ExternalDocs = schema?.ExternalDocs != null ? new(schema.ExternalDocs) : null;
        schemaProp.Deprecated = schema?.Deprecated ?? schemaProp.Deprecated;
        schemaProp.Xml = schema?.Xml != null ? new(schema.Xml) : null;
        schemaProp.UnresolvedReference = schema?.UnresolvedReference ?? schemaProp.UnresolvedReference;
        schemaProp.Reference = schema?.Reference != null ? new(schema.Reference) : null;
    }

    private bool IsInlineSchema(Type schemaType)
    {
        return schemaType.Namespace != null && InlineSchemaTypesInNamespaces.Contains(schemaType.Namespace);
    }

    public OpenApiSchema? CreateSchema(Type schemaType, string? route=null, string? verb=null, HashSet<Type>? allTypes = null)
    {
        if (schemaType.ExcludesFeature(Feature.Metadata) || schemaType.ExcludesFeature(Feature.ApiExplorer)) 
            return null;

        if (IsSwaggerScalarType(schemaType) && !schemaType.IsEnum)
            return null;

        var schemaId = GetSchemaDefinitionRef(schemaType);
        if (Schemas.TryGetValue(schemaId, out var schema)) 
            return schema;

        schema = CreateDictionarySchema(schemaType)
                ?? GetKeyValuePairSchema(schemaType)
                ?? GetListSchema(schemaType);

        bool parseProperties = false;
        if (schema == null)
        {
            if (schemaType.IsEnum)
            {
                schema = CreateEnumSchema(schemaType);
            }
            else
            {
                schema = new OpenApiSchema
                {
                    Type = OpenApiType.Object,
                    Title = GetSchemaTypeName(schemaType),
                    Description = schemaType.GetDescription() ?? GetSchemaTypeName(schemaType),
                    Properties = new OrderedDictionary<string, OpenApiSchema>()
                };
                parseProperties = schemaType.IsUserType();
            }
            
            if (allTypes != null && schemaType.BaseType != null && allTypes.Contains(schemaType.BaseType))
            {
                schema.AllOf.Add(new OpenApiSchema { Reference = ToOpenApiReference(schemaType.BaseType) });
            }
        }
        Schemas[schemaId] = schema;

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
                if (prop.HasAttributeOf<IgnoreDataMemberAttribute>())
                    continue;

                var apiMembers = prop
                    .AllAttributes<ApiMemberAttribute>()
                    .OrderByDescending(attr => attr.Route)
                    .ToList();
                var apiDoc = apiMembers
                    .Where(attr => string.IsNullOrEmpty(verb) || string.IsNullOrEmpty(attr.Verb) || (verb ?? "").Equals(attr.Verb))
                    .Where(attr => string.IsNullOrEmpty(route) || string.IsNullOrEmpty(attr.Route) || (route ?? "").StartsWith(attr.Route))
                    .FirstOrDefault(attr => attr.ParameterType is "body" or "model");

                if (apiMembers.Any(x => x.ExcludeInSchema))
                    continue;
                var schemaProperty = GetOpenApiProperty(prop);
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
                        schema.Required.Add(schemaPropertyName);
                    }
                }

                schemaProperty.Enum = GetEnumValues(prop.FirstAttribute<ApiAllowableValuesAttribute>()).ToOpenApiEnums();

                SchemaPropertyFilter?.Invoke(schemaProperty);

                schema.Properties[schemaPropertyName] = schemaProperty;
            }
        }
        return schema;
    }

    private static string GetSchemaPropertyName(PropertyInfo prop)
    {
        var dataMemberAttr = prop.FirstAttribute<DataMemberAttribute>();
        if (dataMemberAttr?.Name != null)
            return dataMemberAttr.Name;

        return JsConfig.TextCase == TextCase.CamelCase
            ? prop.Name.ToCamelCase()
            : JsConfig.TextCase == TextCase.SnakeCase
                ? prop.Name.ToLowercaseUnderscore()
                : prop.Name;
    }

    private static List<string> GetNumericValues(Type propertyType, Type underlyingType)
    {
        var values = Enum.GetValues(propertyType)
            .Map(x => $"{Convert.ChangeType(x, underlyingType)} ({x})");

        return values;
    }

    private OpenApiSchema? GetResponseSchema(IRestPath restPath, out string schemaDescription)
    {
        schemaDescription = string.Empty;

        // Given: class MyDto : IReturn<X>. Determine the type X.
        foreach (var i in restPath.RequestType.GetInterfaces())
        {
            if (i == typeof(IReturnVoid))
                return GetSchemaForResponseType(typeof(void), out schemaDescription);

            if (i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IReturn<>))
            {
                var schemaType = i.GetGenericArguments()[0];
                return GetSchemaForResponseType(schemaType, out schemaDescription);
            }
        }

        return new OpenApiSchema { 
            Type = OpenApiType.Object,
        };
    }

    private OpenApiSchema? GetSchemaForResponseType(Type schemaType, out string schemaDescription)
    {
        if (schemaType == typeof(IReturnVoid) || schemaType == typeof(void))
        {
            schemaDescription = "No Content";
            return null;
        }

        var schema = CreateDictionarySchema(schemaType)
            ?? GetKeyValuePairSchema(schemaType)
            ?? GetListSchema(schemaType)
            ?? (IsSwaggerScalarType(schemaType)
                ? new OpenApiSchema
                {
                    Title = GetSchemaTypeName(schemaType),
                    Type = GetSwaggerTypeName(schemaType),
                    Format = GetSwaggerTypeFormat(schemaType)
                }
            : IsInlineSchema(schemaType)
                ? CreateSchema(schemaType)
                : new OpenApiSchema { Reference = ToOpenApiReference(schemaType) });

        schemaDescription = schema?.Description ?? schemaType.GetDescription() ?? string.Empty;

        return schema;
    }

    private OrderedDictionary<string, OpenApiResponse> GetMethodResponseCodes(IRestPath restPath, IDictionary<string, OpenApiSchema> schemas, Type requestType)
    {
        var responses = new OrderedDictionary<string, OpenApiResponse>();

        var responseSchema = GetResponseSchema(restPath, out string schemaDescription);
        //schema is null when return type is IReturnVoid
        var statusCode = responseSchema == null && HostConfig.Instance.Return204NoContentForEmptyResponse
            ? ((int)HttpStatusCode.NoContent).ToString()
            : ((int)HttpStatusCode.OK).ToString();

        responses.Add(statusCode, new OpenApiResponse
        {
            Content =
            {
                [MimeTypes.Json] = new OpenApiMediaType
                {
                    Schema = responseSchema,
                }
            },
            Description = !string.IsNullOrEmpty(schemaDescription) ? schemaDescription : "Success"
        });

        foreach (var attr in requestType.AllAttributes<ApiResponseAttribute>())
        {
            string apiSchemaDescription = string.Empty;

            var response = new OpenApiResponse
            {
                Content =
                {
                    [MimeTypes.Json] = new OpenApiMediaType
                    {
                        Schema = attr.ResponseType != null
                            ? GetSchemaForResponseType(attr.ResponseType, out apiSchemaDescription)
                            : responseSchema,
                    }
                },
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

}