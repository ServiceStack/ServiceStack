#if NET8_0_OR_GREATER
#nullable enable

using System;
using System.Collections.Generic;

namespace ServiceStack;

public enum JsonSerializerType
{
    ServiceStackJson,
    SystemJson,
    JsonObject,
}

/// <summary>
/// JSON Serializer for serializing Complex Types in OrmLite 
/// </summary>
public class JsonComplexTypeSerializer : Text.IStringSerializer
{
    public JsonSerializerType DefaultSerializer { get; set; } = JsonSerializerType.ServiceStackJson;
    
    /// <summary>
    /// Types that should always be deserialized with JSON.parse()
    /// </summary>
    public HashSet<Type> JsonObjectTypes { get; set; } =
    [
        typeof(object),
        typeof(List<object>),
        typeof(Dictionary<string, object?>),
    ];

    /// <summary>
    /// Types that should always be deserialized with System.Text.Json
    /// </summary>
    public HashSet<Type> SystemJsonTypes { get; set; } = [];
    
    /// <summary>
    /// Types that should always be deserialized with ServiceStack.Text JsonSerializer
    /// </summary>
    public HashSet<Type> ServiceStackJsonTypes { get; set; } = [];
    
    public To? DeserializeFromString<To>(string json)
    {
        if (JsonObjectTypes.Contains(typeof(To)))
            return (To)JSON.Deserialize(json, typeof(To));
        if (SystemJsonTypes.Contains(typeof(To)))
            return System.Text.Json.JsonSerializer.Deserialize<To>(json, Text.TextConfig.SystemJsonOptions);
        if (ServiceStackJsonTypes.Contains(typeof(To)))
            return Text.JsonSerializer.DeserializeFromString<To>(json);

        return DefaultSerializer switch
        {
            JsonSerializerType.JsonObject => (To)JSON.Deserialize(json, typeof(To))!,
            JsonSerializerType.SystemJson => System.Text.Json.JsonSerializer.Deserialize<To>(json, Text.TextConfig.SystemJsonOptions),
            JsonSerializerType.ServiceStackJson => Text.JsonSerializer.DeserializeFromString<To>(json),
            _ => throw new NotSupportedException(DefaultSerializer.ToString())
        };
    }

    public object? DeserializeFromString(string json, Type type)
    {
        if (JsonObjectTypes.Contains(type))
            return JSON.parse(json);
        if (SystemJsonTypes.Contains(type))
            return System.Text.Json.JsonSerializer.Deserialize(json, type, Text.TextConfig.SystemJsonOptions);
        if (ServiceStackJsonTypes.Contains(type))
            return Text.JsonSerializer.DeserializeFromString(json, type);
        
        return DefaultSerializer switch
        {
            JsonSerializerType.JsonObject => JSON.Deserialize(json, type),
            JsonSerializerType.SystemJson => System.Text.Json.JsonSerializer.Deserialize(json, type, Text.TextConfig.SystemJsonOptions),
            JsonSerializerType.ServiceStackJson => Text.JsonSerializer.DeserializeFromString(json, type),
            _ => throw new NotSupportedException(DefaultSerializer.ToString())
        };
    }

    public string SerializeToString<TFrom>(TFrom from)
    {
        if (JsonObjectTypes.Contains(typeof(TFrom)))
            return JSON.stringify(from);
        if (SystemJsonTypes.Contains(typeof(TFrom)))
            return System.Text.Json.JsonSerializer.Serialize(from, Text.TextConfig.SystemJsonOptions);
        if (ServiceStackJsonTypes.Contains(typeof(TFrom)))
            return Text.JsonSerializer.SerializeToString(from);
        
        return DefaultSerializer switch
        {
            JsonSerializerType.JsonObject => JSON.stringify(from),
            JsonSerializerType.SystemJson => System.Text.Json.JsonSerializer.Serialize(from, Text.TextConfig.SystemJsonOptions),
            JsonSerializerType.ServiceStackJson => Text.JsonSerializer.SerializeToString(from),
            _ => throw new NotSupportedException(DefaultSerializer.ToString())
        };
    }
}
#endif
