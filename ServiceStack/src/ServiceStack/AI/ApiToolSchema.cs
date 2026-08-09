#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using ServiceStack.DataAnnotations;
using ServiceStack.Text;

namespace ServiceStack;

/// <summary>
/// Generates the restricted JSON Schema subset sent inside model-provider function definitions,
/// from the Request DTO and its existing documentation attributes.
/// <para>
/// API discovery and approval UIs use <c>MetadataSchemaGenerator</c>'s complete API Schema;
/// providers can reject its richer route, auth, reference and UI annotations in function schemas.
/// </para>
/// </summary>
public static class ApiToolSchema
{
    /// <summary>Complex properties are expanded to this depth, then reduced to a plain object</summary>
    public const int MaxDepth = 3;

    public static Dictionary<string, object?> ToJsonSchema(Type type) => ObjectSchema(type, 0, []);

    static Dictionary<string, object?> ObjectSchema(Type type, int depth, HashSet<Type> seen)
    {
        var properties = new Dictionary<string, object?>();
        var required = new List<string>();

        foreach (var pi in GetSchemaProperties(type))
        {
            var apiMember = pi.FirstAttribute<ApiMemberAttribute>();
            var schema = PropertySchema(pi, apiMember, depth, seen);
            properties[pi.Name.ToCamelCase()] = schema;

            var isRequired = apiMember?.IsRequired == true
                || (pi.HasAttributeOf<RequiredAttribute>() && apiMember?.IsOptional != true);
            if (isRequired)
                required.Add(pi.Name.ToCamelCase());
        }

        var to = new Dictionary<string, object?>
        {
            ["type"] = "object",
            ["properties"] = properties,
        };
        if (required.Count > 0)
            to["required"] = required;
        return to;
    }

    static List<PropertyInfo> GetSchemaProperties(Type type) => type
        .GetProperties(BindingFlags.Public | BindingFlags.Instance)
        .Where(x => x.CanRead && x.CanWrite && x.GetIndexParameters().Length == 0)
        .Where(x => !x.HasAttributeOf<IgnoreDataMemberAttribute>()
            && !x.HasAttributeOf<IgnoreAttribute>()
            && x.FirstAttribute<ApiMemberAttribute>()?.ExcludeInSchema != true)
        .ToList();

    static Dictionary<string, object?> PropertySchema(PropertyInfo pi, ApiMemberAttribute? apiMember,
        int depth, HashSet<Type> seen)
    {
        var schema = TypeSchema(pi.PropertyType, depth, seen);

        // GetDescription() covers [ApiMember], System.ComponentModel's [Description] and
        // ServiceStack.DataAnnotations' — a schema that ignores two of the three reads as undocumented
        var description = apiMember?.Description ?? pi.GetDescription();
        if (!string.IsNullOrEmpty(description))
            schema["description"] = description;

        // [ApiAllowableValues] wins over the enum's own values, it's the narrower statement
        if (pi.FirstAttribute<ApiAllowableValuesAttribute>()?.Values is { Length: > 0 } allowable)
            schema["enum"] = allowable.ToList();

        return schema;
    }

    static Dictionary<string, object?> TypeSchema(Type type, int depth, HashSet<Type> seen)
    {
        type = Nullable.GetUnderlyingType(type) ?? type;

        if (type.IsEnum)
        {
            return new Dictionary<string, object?>
            {
                ["type"] = "string",
                ["enum"] = Enum.GetNames(type).ToList(),
            };
        }

        if (JsonTypes.TryGetValue(type, out var jsonType))
        {
            var to = new Dictionary<string, object?> { ["type"] = jsonType.Type };
            if (jsonType.Format != null)
                to["format"] = jsonType.Format;
            return to;
        }

        if (GetDictionaryValueType(type) is { } valueType)
        {
            return new Dictionary<string, object?>
            {
                ["type"] = "object",
                ["additionalProperties"] = TypeSchema(valueType, depth + 1, seen),
            };
        }

        if (GetCollectionItemType(type) is { } itemType)
        {
            return new Dictionary<string, object?>
            {
                ["type"] = "array",
                ["items"] = TypeSchema(itemType, depth + 1, seen),
            };
        }

        if (type.IsClass || type.IsInterface || (type.IsValueType && !type.IsPrimitive))
        {
            // a self-referencing DTO would otherwise expand forever
            if (depth >= MaxDepth || !seen.Add(type))
                return new Dictionary<string, object?> { ["type"] = "object", ["description"] = type.Name };
            try
            {
                return ObjectSchema(type, depth + 1, seen);
            }
            finally
            {
                seen.Remove(type);
            }
        }

        return new Dictionary<string, object?> { ["type"] = "string" };
    }

    static Type? GetCollectionItemType(Type type)
    {
        if (type == typeof(string) || type == typeof(byte[]))
            return null;
        if (type.IsArray)
            return type.GetElementType();
        var enumerable = type.GetInterfaces().Concat([type])
            .FirstOrDefault(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IEnumerable<>));
        return enumerable?.GetGenericArguments()[0];
    }

    static Type? GetDictionaryValueType(Type type)
    {
        var dictionary = type.GetInterfaces().Concat([type])
            .FirstOrDefault(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IDictionary<,>));
        return dictionary?.GetGenericArguments()[1];
    }

    static readonly Dictionary<Type, (string Type, string? Format)> JsonTypes = new()
    {
        [typeof(string)] = ("string", null),
        [typeof(char)] = ("string", null),
        [typeof(Guid)] = ("string", "uuid"),
        [typeof(Uri)] = ("string", "uri"),
        [typeof(TimeSpan)] = ("string", "duration"),
        [typeof(DateTime)] = ("string", "date-time"),
        [typeof(DateTimeOffset)] = ("string", "date-time"),
        [typeof(byte[])] = ("string", "byte"),
        [typeof(bool)] = ("boolean", null),
        [typeof(byte)] = ("integer", null),
        [typeof(sbyte)] = ("integer", null),
        [typeof(short)] = ("integer", null),
        [typeof(ushort)] = ("integer", null),
        [typeof(int)] = ("integer", null),
        [typeof(uint)] = ("integer", null),
        [typeof(long)] = ("integer", null),
        [typeof(ulong)] = ("integer", null),
        [typeof(float)] = ("number", null),
        [typeof(double)] = ("number", null),
        [typeof(decimal)] = ("number", null),
    };
}
