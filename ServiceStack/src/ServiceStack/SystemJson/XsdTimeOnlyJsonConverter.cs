#if NET6_0_OR_GREATER

using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ServiceStack.SystemJson;

public class XsdTimeOnlyJsonConverter : JsonConverter<TimeOnly>
{
    public override TimeOnly Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var formatted = reader.GetString()!;
        return TimeOnly.FromTimeSpan(Text.Support.TimeSpanConverter.FromXsdDuration(formatted));
    }

    public override void Write(Utf8JsonWriter writer, TimeOnly value, JsonSerializerOptions options)
    {
        var formatted = Text.Support.TimeSpanConverter.ToXsdDuration(value.ToTimeSpan());
        writer.WriteStringValue(formatted);
    }
}

#endif