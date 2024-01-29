#if NET6_0_OR_GREATER

using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ServiceStack.SystemJson;

public class XsdTimeSpanJsonConverter : JsonConverter<TimeSpan>
{
    public override TimeSpan Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var formatted = reader.GetString()!;
        return Text.Support.TimeSpanConverter.FromXsdDuration(formatted);
    }

    public override void Write(Utf8JsonWriter writer, TimeSpan value, JsonSerializerOptions options)
    {
        var formatted = Text.Support.TimeSpanConverter.ToXsdDuration(value);
        writer.WriteStringValue(formatted);
    }
}

#endif