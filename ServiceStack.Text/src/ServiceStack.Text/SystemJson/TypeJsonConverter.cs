#if NET6_0_OR_GREATER

using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using ServiceStack.Text;
using ServiceStack.Text.Common;

namespace ServiceStack.SystemJson;

public class TypeJsonConverter : JsonConverter<Type>
{
    public override Type Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
            throw new JsonException("Expected string value for Type");

        string typeName = reader.GetString();
        
        if (string.IsNullOrEmpty(typeName))
            throw new JsonException("Type name cannot be null or empty");
        

        try
        {
            // First try to get the type from the currently loaded assemblies
            Type type = JsConfig.GetConfig().TypeFinder(typeName);
            JsWriter.AssertAllowedRuntimeType(type);

            if (type != null)
            {
                return type;
            }

            // If not found, try to load from all loaded assemblies
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                type = assembly.GetType(typeName);
                if (type != null)
                {
                    return type;
                }
            }

            throw new JsonException($"Could not find type: {typeName}");
        }
        catch (Exception ex) when (ex is not JsonException)
        {
            throw new JsonException($"Error deserializing type: {typeName}", ex);
        }
    }

    public override void Write(Utf8JsonWriter writer, Type value, JsonSerializerOptions options)
    {
        if (value == null)
        {
            writer.WriteNullValue();
            return;
        }

        // Use AssemblyQualifiedName to ensure the type can be properly deserialized
        writer.WriteStringValue(value.FullName);
    }
}

#endif