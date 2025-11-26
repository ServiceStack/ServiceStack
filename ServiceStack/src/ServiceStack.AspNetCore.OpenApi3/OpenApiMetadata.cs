using System.Collections.Concurrent;
using System.Net;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.OpenApi;
using ServiceStack.Host;
using ServiceStack.NativeTypes;
using ServiceStack.Text;
using ServiceStack.Web;
using OpenApiReference = Microsoft.OpenApi.BaseOpenApiReference;

namespace ServiceStack.AspNetCore.OpenApi;

public static class OpenApiSecurity
{
    public static OpenApiSecurityScheme BasicAuthScheme { get; set; } = new()
    {
        In = ParameterLocation.Header,
        Name = "Authorization",
        Description = "HTTP Basic access authentication",
        Type = SecuritySchemeType.Http,
        Scheme = BasicAuthenticationHandler.Scheme,
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

    public static OpenApiSecurityScheme ApiKeyScheme { get; set; } = new()
    {
        Description = "API Key authorization header using the Bearer scheme in the format `Bearer <ApiKey>`",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "ApiKey"
    };

    /// <summary>
    /// Create a security requirement for the given scheme that will serialize correctly.
    /// The hostDocument is required for the OpenApiSecuritySchemeReference to resolve its Target.
    /// </summary>
    public static OpenApiSecurityRequirement CreateSecurityRequirement(string schemeId, OpenApiDocument hostDocument)
    {
        return new OpenApiSecurityRequirement
        {
            { new OpenApiSecuritySchemeReference(schemeId, hostDocument), [] }
        };
    }
}

public class OpenApiMetadata
{
    public static OpenApiMetadata Instance { get; } = new();
    
    public List<Type> DocumentFilterTypes { get; set; } = [
        typeof(ServiceStackDocumentFilter),
    ];
    public List<Type> SchemaFilterTypes { get; set; } = [
    ];
    
    public Func<Operation, bool>? Ignore { get; set; }
    
    public Action<string, OpenApiOperation, Operation>? OperationFilter { get; set; }
    public Action<OpenApiOperation, OpenApiSchema>? SchemaFilter { get; set; }
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

    public ConcurrentDictionary<string, OpenApiSchema> Schemas { get; } = new();
    internal static List<string> InlineSchemaTypesInNamespaces { get; set; } = new();
    
    public OpenApiSecurityScheme? SecurityDefinition { get; set; }

    public OpenApiSecurityScheme? ApiKeySecurityDefinition { get; set; }

    /// <summary>
    /// Exclude showing Request DTO APIs in Open API metadata and Swagger UI
    /// </summary>
    public HashSet<Type> ExcludeRequestTypes { get; set; } = [];

    public void AddBasicAuth()
    {
        SecurityDefinition = OpenApiSecurity.BasicAuthScheme;
    }

    public void AddJwtBearer()
    {
        SecurityDefinition = OpenApiSecurity.JwtBearerScheme;
    }

    public void AddApiKeys()
    {
        ApiKeySecurityDefinition = OpenApiSecurity.ApiKeyScheme;
    }

    public OpenApiOperation AddOperation(OpenApiOperation op, Operation operation, string verb, string route, OpenApiDocument? hostDocument = null)
    {
        if (ExcludeRequestTypes.Contains(operation.RequestType))
            return op;
        //Console.WriteLine($"AddOperation {verb} {route} {operation.RequestType.Name}...");

        // Response is handled by Endpoints Metadata
        op.Summary = operation.RequestType.GetDescription();
        op.Description = operation.RequestType.FirstAttribute<NotesAttribute>()?.Notes;

        var hasRequestBody = HttpUtils.HasRequestBody(verb);
        if (!hasRequestBody)
        {
            var parameters = CreateParameters(operation.RequestType, route, verb);
            if (parameters.Count > 0)
            {
                if (op.Parameters == null)
                    op.Parameters = new List<IOpenApiParameter>();
                op.Parameters.AddDistinctRange(parameters);
            }
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
                        var propNameUsed = route.Contains("{" + entry.Key + "}")
                            ? entry.Key
                            : TypeProperties.Get(operation.RequestType).GetPublicProperty(entry.Key)?.Name
                              ?? throw new ArgumentException($"Could not find property '{entry.Key}' for route '{route}' in Request {operation.RequestType.Name}");
                        inPaths.Add(entry.Key);
                        IOpenApiSchema prop = entry.Value;
                        if (op.Parameters == null)
                            op.Parameters = new List<IOpenApiParameter>();
                        op.Parameters.Add(new OpenApiParameter
                        {
                            Name = propNameUsed,
                            In = ParameterLocation.Path,
                            Required = true,
                            Schema = prop,
                            Style = ParameterStyle.Simple,
                            Explode = true,
                        });
                    }
                }

                var formSchema = openApiType.CreateShallowCopy();
                foreach (var propName in inPaths)
                {
                    formSchema.Properties.Remove(propName);
                }

                var formType = new OpenApiMediaType
                {
                    Schema = formSchema,
                };
                if (formSchema.Properties.Count > 0)
                {
                    formType.Encoding ??= new OrderedDictionary<string, OpenApiEncoding>();
                    foreach (var entry in formSchema.Properties)
                    {
                        formType.Encoding[entry.Key] = new OpenApiEncoding { Style = ParameterStyle.Form, Explode = false };
                    }
                }
                var requestBody = new OpenApiRequestBody
                {
                };
                var content = requestBody.Content ??= new Dictionary<string, OpenApiMediaType>();
                content[MimeTypes.MultiPartFormData] = formType;
                if (apiAttr?.BodyParameter != GenerateBodyParameter.Never)
                {
                    content[MimeTypes.Json] = new OpenApiMediaType
                    {
                        Schema = ToOpenApiSchemaReference(operation.RequestType)
                    };
                }
                op.RequestBody = requestBody;
                SchemaFilter?.Invoke(op, openApiType);
            }
        }

        if (operation.RequiresAuthentication)
        {
            if (SecurityDefinition != null && hostDocument != null)
            {
                op.Security ??= new List<OpenApiSecurityRequirement>();
                op.Security.Add(OpenApiSecurity.CreateSecurityRequirement(SecurityDefinition.Scheme, hostDocument));
            }
        }
        if (operation.RequiresApiKey)
        {
            if (ApiKeySecurityDefinition != null && hostDocument != null)
            {
                op.Security ??= new List<OpenApiSecurityRequirement>();
                op.Security.Add(OpenApiSecurity.CreateSecurityRequirement(ApiKeySecurityDefinition.Scheme, hostDocument));
            }
        }

        var userTags = operation.RequestType.AllAttributes<TagAttribute>().Map(x => x.Name);
        if (userTags.Count > 0)
        {
            // Clear the endpoint out of the first (ServiceName) tag, so the API only appears once under its custom tag
            op.Tags ??= new HashSet<OpenApiTagReference>();
            op.Tags.Clear();
            userTags.Each(tag => op.Tags.Add(new OpenApiTagReference(tag)));
        }

        OperationFilter?.Invoke(verb, op, operation);

        return op;
    }

    internal static IOpenApiSchema ToOpenApiSchemaReference(Type type) =>
        new OpenApiSchemaReference(GetSchemaDefinitionRef(type));

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
        IOpenApiSchema? items = null;
        if (listItemType != null)
        {
            if (IsSwaggerScalarType(listItemType))
            {
                items = new OpenApiSchema
                {
                    Type = OpenApiType.ToJsonSchemaType(GetSwaggerTypeName(listItemType))
                };
            }
            else
            {
                items = ToOpenApiSchemaReference(listItemType);
            }
        }

        return new OpenApiSchema
        {
            Title = GetSchemaTypeName(schemaType),
            Type = OpenApiType.ToJsonSchemaType(OpenApiType.Array),
            Items = items,
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
            Type = OpenApiType.ToJsonSchemaType(OpenApiType.Object),
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
            Type = OpenApiType.ToJsonSchemaType(OpenApiType.Object),
            Title = GetSchemaTypeName(schemaType),
            Description = schemaType.GetDescription() ?? GetSchemaTypeName(schemaType),
            Properties = new OrderedDictionary<string, IOpenApiSchema>
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
                Schema = CreateEnumSchema(propType),
                Required = paramLocation == ParameterLocation.Path,
            };
        }
        
        if (IsSwaggerScalarType(propType))
        {
            var schema = new OpenApiSchema
            {
                Type = OpenApiType.ToJsonSchemaType(GetSwaggerTypeName(propType)),
                Format = GetSwaggerTypeFormat(propType),
            };
            if (enumValues != null && enumValues.Length > 0)
            {
                schema.Enum = enumValues.Select(x => (JsonNode)System.Text.Json.Nodes.JsonValue.Create(x)).ToList();
            }
            if (!IsRequiredType(propType))
            {
                ApplyNullable(schema, true);
            }
            return new OpenApiParameter
            {
                In = paramLocation,
                Name = paramName,
                Schema = schema,
                Required = paramLocation == ParameterLocation.Path,
            };
        }

        if (!isApiMember)
        {
            return new OpenApiParameter
            {
                In = paramLocation,
                Name = paramName,
                Schema = new OpenApiSchema
                {
                    Type = OpenApiType.ToJsonSchemaType(OpenApiType.String),
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

        IOpenApiSchema openApiSchema;

        if (IsInlineSchema(propType))
        {
            openApiSchema = CreateSchema(propType);
        }
        else
        {
            openApiSchema = ToOpenApiSchemaReference(propType);
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

    private OpenApiParameter CreateArrayParameter(Type listType, 
        string paramName, 
        ParameterLocation? paramLocation)
    {
        var listItemType = GetListElementType(listType);
        IOpenApiSchema? items = null;
        if (listItemType != null)
        {
            if (IsSwaggerScalarType(listItemType))
            {
                items = new OpenApiSchema
                {
                    Type = OpenApiType.ToJsonSchemaType(GetSwaggerTypeName(listItemType))
                };
            }
            else
            {
                items = ToOpenApiSchemaReference(listItemType);
            }
        }

        var parameter = new OpenApiParameter
        {
            In = paramLocation,
            Schema = new OpenApiSchema {
                Type = OpenApiType.ToJsonSchemaType(OpenApiType.Array),
                Items = items
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

    private IOpenApiSchema GetOpenApiProperty(PropertyInfo pi)
    {
        var schema = GetOpenApiProperty(pi.PropertyType);
        if (schema is OpenApiSchema openApiSchema && pi.IsAssignableToNull())
        {
            openApiSchema.Type = openApiSchema.Type.HasValue
                ? openApiSchema.Type.Value | JsonSchemaType.Null
                : JsonSchemaType.Null;
        }
        return schema;
    }

    private IOpenApiSchema GetOpenApiProperty(Type propertyType)
    {
        var isNullable = propertyType.IsNullableType();
        propertyType = Nullable.GetUnderlyingType(propertyType) ?? propertyType;

        if (IsKeyValuePairType(propertyType))
        {
            if (IsInlineSchema(propertyType))
            {
                var schemaProp = new OpenApiSchema();
                var schema = CreateSchema(propertyType);
                if (schema != null) InlineSchema(schema, schemaProp);
                return ApplyNullable(schemaProp, isNullable);
            }
            else
            {
                return ToOpenApiSchemaReference(propertyType);
            }
        }
        else if (IsListType(propertyType))
        {
            var schemaProp = new OpenApiSchema
            {
                Type = OpenApiType.ToJsonSchemaType(OpenApiType.Array)
            };
            var listItemType = GetListElementType(propertyType);
            if (listItemType == null) return ApplyNullable(schemaProp, isNullable);
            if (IsSwaggerScalarType(listItemType))
            {
                schemaProp.Items = new OpenApiSchema {
                    Type = OpenApiType.ToJsonSchemaType(GetSwaggerTypeName(listItemType)),
                    Format = GetSwaggerTypeFormat(listItemType),
                };
            }
            else if (IsInlineSchema(listItemType))
            {
                var schema = CreateSchema(listItemType);
                if (schema != null) InlineSchema(schema, schemaProp);
            }
            else
            {
                schemaProp.Items = ToOpenApiSchemaReference(listItemType);
            }
            return ApplyNullable(schemaProp, isNullable);
        }
        else if (IsDictionaryType(propertyType))
        {
            var schemaProp = CreateDictionarySchema(propertyType);
            return ApplyNullable(schemaProp, isNullable);
        }
        else if (propertyType.IsEnum)
        {
            return ToOpenApiSchemaReference(propertyType);
        }
        else if (IsSwaggerScalarType(propertyType))
        {
            var schemaProp = new OpenApiSchema
            {
                Type = OpenApiType.ToJsonSchemaType(GetSwaggerTypeName(propertyType)),
                Format = GetSwaggerTypeFormat(propertyType),
            };
            var nullable = isNullable || !IsRequiredType(propertyType);
            return ApplyNullable(schemaProp, nullable);
        }
        else if (IsInlineSchema(propertyType))
        {
            var schemaProp = new OpenApiSchema();
            var schema = CreateSchema(propertyType);
            if (schema != null) InlineSchema(schema, schemaProp);
            return ApplyNullable(schemaProp, isNullable);
        }
        else
        {
            //CreateSchema(propertyType, route, verb);
            return ToOpenApiSchemaReference(propertyType);
        }
    }

    private static IOpenApiSchema ApplyNullable(OpenApiSchema schema, bool isNullable)
    {
        if (isNullable && schema.Type.HasValue)
        {
            schema.Type = schema.Type.Value | JsonSchemaType.Null;
        }
        return schema;
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
            schema.Type = OpenApiType.ToJsonSchemaType(GetSwaggerTypeName(underlyingType));
            schema.Format = GetSwaggerTypeFormat(underlyingType);
            schema.Enum = GetNumericValues(enumType, underlyingType).ToOpenApiEnums();
        }
        else
        {
            schema.Type = OpenApiType.ToJsonSchemaType(OpenApiType.String);
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
        schemaProp.Default = schema?.Default?.DeepClone();
        schemaProp.ReadOnly = schema?.ReadOnly ?? schemaProp.ReadOnly;
        schemaProp.WriteOnly = schema?.WriteOnly ?? schemaProp.WriteOnly;
        schemaProp.AllOf = schema?.AllOf != null ? new List<IOpenApiSchema>(schema.AllOf) : null;
        schemaProp.OneOf = schema?.OneOf != null ? new List<IOpenApiSchema>(schema.OneOf) : null;
        schemaProp.AnyOf = schema?.AnyOf != null ? new List<IOpenApiSchema>(schema.AnyOf) : null;
        schemaProp.Not = schema?.Not?.CreateShallowCopy();
        schemaProp.Required = schema?.Required != null ? new HashSet<string>(schema.Required) : null;
        schemaProp.Items = schema?.Items?.CreateShallowCopy();
        schemaProp.MaxItems = schema?.MaxItems ?? schemaProp.MaxItems;
        schemaProp.MinItems = schema?.MinItems ?? schemaProp.MinItems;
        schemaProp.UniqueItems = schema?.UniqueItems ?? schemaProp.UniqueItems;
        schemaProp.Properties = schema?.Properties != null ? new Dictionary<string, IOpenApiSchema>(schema.Properties) : null;
        schemaProp.MaxProperties = schema?.MaxProperties ?? schemaProp.MaxProperties;
        schemaProp.MinProperties = schema?.MinProperties ?? schemaProp.MinProperties;
        schemaProp.AdditionalPropertiesAllowed = schema?.AdditionalPropertiesAllowed ?? schemaProp.AdditionalPropertiesAllowed;
        schemaProp.AdditionalProperties = schema?.AdditionalProperties?.CreateShallowCopy();
        schemaProp.Discriminator = schema?.Discriminator != null ? new(schema.Discriminator) : null;
        // In Microsoft.OpenApi 3.x, Example is read-only for some implementations; preserve existing example if available
        // but avoid assigning directly when it's not supported.
        if (schema?.Example is JsonNode exampleNode)
            schemaProp.Example = exampleNode.DeepClone();
        schemaProp.Enum = schema?.Enum != null ? new List<JsonNode>(schema.Enum) : null;
        schemaProp.ExternalDocs = schema?.ExternalDocs != null ? new(schema.ExternalDocs) : null;
        schemaProp.Deprecated = schema?.Deprecated ?? schemaProp.Deprecated;
        schemaProp.Xml = schema?.Xml != null ? new(schema.Xml) : null;
    }

    private bool IsInlineSchema(Type schemaType)
    {
        return schemaType.Namespace != null && InlineSchemaTypesInNamespaces.Contains(schemaType.Namespace);
    }

    List<string> RequiredValidators { get; } = ["NotNull", "NotEmpty"];
                
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
                    Type = OpenApiType.ToJsonSchemaType(OpenApiType.Object),
                    Title = GetSchemaTypeName(schemaType),
                    Description = schemaType.GetDescription() ?? GetSchemaTypeName(schemaType),
                    Properties = new OrderedDictionary<string, IOpenApiSchema>()
                };
                parseProperties = schemaType.IsUserType();
            }

            if (allTypes != null && schemaType.BaseType != null && allTypes.Contains(schemaType.BaseType))
            {
                schema.AllOf ??= new List<IOpenApiSchema>();
                schema.AllOf.Add(ToOpenApiSchemaReference(schemaType.BaseType));
            }
        }
        Schemas[schemaId] = schema;

        var properties = schemaType.GetProperties()
            .Where(pi => !SwaggerUtils.IgnoreProperty(pi))
            .ToArray();

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

                if (schemaProperty is OpenApiSchema openApiSchema)
                {
                    openApiSchema.Description = prop.GetDescription() ?? apiDoc?.Description;

                    var propAttr = prop.FirstAttribute<ApiMemberAttribute>();
                    var validateAttrs = prop.AllAttributes<ValidateAttribute>();

                    var isRequired = propAttr?.IsRequired == true
                        || validateAttrs.Any(x => RequiredValidators.Contains(x.Validator))
                        || (prop.PropertyType.IsNumericType() && validateAttrs.Any(attr => attr.Validator?.StartsWith("GreaterThan") == true));

                    if (propAttr != null)
                    {
                        if (propAttr.DataType != null)
                            openApiSchema.Type = OpenApiType.ToJsonSchemaType(propAttr.DataType);

                        if (propAttr.Format != null)
                            openApiSchema.Format = propAttr.Format;
                    }

                    if (isRequired)
                    {
                        schema.Required ??= new HashSet<string>();
                        schema.Required.Add(schemaPropertyName);
                    }

                    var uploadTo = prop.FirstAttribute<UploadToAttribute>();
                    if (uploadTo != null)
                    {
                        if (openApiSchema.Type != OpenApiType.ToJsonSchemaType(OpenApiType.Array))
                        {
                            openApiSchema.Type = JsonSchemaType.String; // "file" type doesn't exist in JsonSchemaType
                        }
                        openApiSchema.Items = new OpenApiSchema
                        {
                            Type = OpenApiType.ToJsonSchemaType(OpenApiType.String),
                            Format = OpenApiTypeFormat.Binary,
                        };
                    }

                    openApiSchema.Enum = GetEnumValues(prop.FirstAttribute<ApiAllowableValuesAttribute>()).ToOpenApiEnums();

                    SchemaPropertyFilter?.Invoke(openApiSchema);
                }
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
            Type = OpenApiType.ToJsonSchemaType(OpenApiType.Object),
        };
    }

    private OpenApiSchema? GetSchemaForResponseType(Type schemaType, out string schemaDescription)
    {
        if (schemaType == typeof(IReturnVoid) || schemaType == typeof(void))
        {
            schemaDescription = "No Content";
            return null;
        }

        OpenApiSchema? schema = CreateDictionarySchema(schemaType)
            ?? GetKeyValuePairSchema(schemaType)
            ?? GetListSchema(schemaType);

        if (schema == null)
        {
            if (IsSwaggerScalarType(schemaType))
            {
                schema = new OpenApiSchema
                {
                    Title = GetSchemaTypeName(schemaType),
                    Type = OpenApiType.ToJsonSchemaType(GetSwaggerTypeName(schemaType)),
                    Format = GetSwaggerTypeFormat(schemaType)
                };
            }
            else if (IsInlineSchema(schemaType))
            {
                schema = CreateSchema(schemaType);
            }
            else
            {
                // For references, we need to return a schema that references the type
                // In v3.0, we can't use OpenApiSchema with Reference property
                // Instead, we should use OpenApiSchemaReference, but since the return type is OpenApiSchema?,
                // we'll create a schema with AllOf containing the reference
                schema = new OpenApiSchema
                {
                    AllOf = new List<IOpenApiSchema> { ToOpenApiSchemaReference(schemaType) }
                };
            }
        }

        schemaDescription = schema?.Description ?? schemaType.GetDescription() ?? string.Empty;

        return schema;
    }

    internal OrderedDictionary<string, OpenApiResponse> GetMethodResponseCodes(IRestPath restPath, IDictionary<string, OpenApiSchema> schemas, Type requestType)
    {
        var responses = new OrderedDictionary<string, OpenApiResponse>();

        var responseSchema = GetResponseSchema(restPath, out string schemaDescription);
        //schema is null when return type is IReturnVoid
        var statusCode = responseSchema == null && HostConfig.Instance.Return204NoContentForEmptyResponse
            ? ((int)HttpStatusCode.NoContent).ToString()
            : ((int)HttpStatusCode.OK).ToString();

        var response = new OpenApiResponse
        {
            Description = !string.IsNullOrEmpty(schemaDescription) ? schemaDescription : "Success"
        };
        var content = response.Content ??= new Dictionary<string, OpenApiMediaType>();
        content[MimeTypes.Json] = new OpenApiMediaType
        {
            Schema = responseSchema,
        };
        responses.Add(statusCode, response);

        foreach (var attr in requestType.AllAttributes<ApiResponseAttribute>())
        {
            string apiSchemaDescription = string.Empty;

            var apiResponse = new OpenApiResponse
            {
                Description = attr.Description ?? apiSchemaDescription
            };
            var apiContent = apiResponse.Content ??= new Dictionary<string, OpenApiMediaType>();
            apiContent[MimeTypes.Json] = new OpenApiMediaType
            {
                Schema = attr.ResponseType != null
                    ? GetSchemaForResponseType(attr.ResponseType, out apiSchemaDescription)
                    : responseSchema,
            };

            statusCode = attr.IsDefaultResponse ? "default" : attr.StatusCode.ToString();
            if (!responses.ContainsKey(statusCode))
                responses.Add(statusCode, apiResponse);
            else
                responses[statusCode] = apiResponse;
        }

        return responses;
    }

}
