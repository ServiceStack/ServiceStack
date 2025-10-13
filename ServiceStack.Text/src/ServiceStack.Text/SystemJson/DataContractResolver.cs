#if NET8_0_OR_GREATER

#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace ServiceStack.SystemJson;

// https://github.com/dotnet/runtime/blob/v8.0.1/src/libraries/System.Text.Json/tests/System.Text.Json.Tests/Serialization/TypeInfoResolverFunctionalTests.cs#L671
public class DataContractResolver : DefaultJsonTypeInfoResolver
{
    public static DataContractResolver Instance { get; } = new();

    public Dictionary<JsonNamingPolicy, Func<string,string>> NamingPolicyConverters { get; } = new()
    {
        [JsonNamingPolicy.CamelCase] = s => s.ToCamelCase(),
        [JsonNamingPolicy.SnakeCaseLower] = s => s.ToLowercaseUnderscore(),
        [JsonNamingPolicy.SnakeCaseUpper] = s => s.ToUppercaseUnderscore(),
        [JsonNamingPolicy.KebabCaseLower] = s => s.ToKebabCase(),
        [JsonNamingPolicy.KebabCaseUpper] = s => s.ToKebabCase().ToUpper(),
    };
    
    public override JsonTypeInfo GetTypeInfo(Type type, JsonSerializerOptions options)
    {
        var jsonTypeInfo = base.GetTypeInfo(type, options);

        if (jsonTypeInfo.Kind == JsonTypeInfoKind.Object)
        {
            var propInfos = type.GetProperties(BindingFlags.Instance | BindingFlags.Public);
            var hasDataContract = type.GetCustomAttribute<DataContractAttribute>() is not null;
            var propInfosToIgnore = propInfos.Where(x => x.GetCustomAttribute<IgnoreDataMemberAttribute>() is not null)
                .Select(x => x.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);

            if (hasDataContract)
            {
                jsonTypeInfo.Properties.Clear();
                foreach (var propInfo in propInfos)
                {
                    if (propInfo.GetCustomAttribute<IgnoreDataMemberAttribute>() is not null
                        || propInfo.GetCustomAttribute<System.Text.Json.Serialization.JsonIgnoreAttribute>() is not null)
                        continue;

                    var attr = propInfo.GetCustomAttribute<DataMemberAttribute>();
                    var name = options.PropertyNamingPolicy != null && 
                               NamingPolicyConverters.TryGetValue(options.PropertyNamingPolicy, out var converter)
                        ? converter(propInfo.Name)
                        : propInfo.Name;

                    if (attr?.Name != null)
                        name = attr.Name;
                    var jsonPropertyInfo = jsonTypeInfo.CreateJsonPropertyInfo(propInfo.PropertyType, name);
                    jsonPropertyInfo.Order = attr?.Order ?? 0;
                    jsonPropertyInfo.Get =
                        propInfo.CanRead
                            ? propInfo.GetValue
                            : null;

                    jsonPropertyInfo.Set = propInfo.CanWrite
                        ? propInfo.SetValue
                        : null;

                    jsonTypeInfo.Properties.Add(jsonPropertyInfo);
                }
            }
            else if (propInfosToIgnore.Count > 0)
            {
                var propsToRemove = jsonTypeInfo.Properties
                    .Where(jsonPropertyInfo => propInfosToIgnore.Contains(jsonPropertyInfo.Name)).ToList();
                foreach (var jsonPropertyInfo in propsToRemove)
                {
                    jsonTypeInfo.Properties.Remove(jsonPropertyInfo);
                }
            }
        }
        return jsonTypeInfo;
    }
}

#endif