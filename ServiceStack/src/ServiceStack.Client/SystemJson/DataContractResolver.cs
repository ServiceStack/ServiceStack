#if NET8_0_OR_GREATER

#nullable enable

using System;
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
                    if (propInfo.GetCustomAttribute<IgnoreDataMemberAttribute>() is not null)
                        continue;

                    var attr = propInfo.GetCustomAttribute<DataMemberAttribute>();
                    var name = options.PropertyNamingPolicy == JsonNamingPolicy.CamelCase 
                        ? propInfo.Name.ToCamelCase()
                        : (options.PropertyNamingPolicy == JsonNamingPolicy.SnakeCaseLower || options.PropertyNamingPolicy == JsonNamingPolicy.SnakeCaseUpper)
                            ? propInfo.Name.ToLowercaseUnderscore()
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
                    .Where(jsonPropertyInfo => propInfosToIgnore.Contains(jsonPropertyInfo.Name));
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