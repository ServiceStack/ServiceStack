using System.Text.Json;
using System.Text.Json.Nodes;

namespace ServiceStack.AI;

/// <summary>
/// JSON conventions for the Chat UI APIs: camelCase keys, exact wire fidelity with llms-py dict responses.
/// Internal UI API payloads are modelled as JsonNode/JsonObject and only converted to typed DTOs at the edges.
/// </summary>
public static class ChatJson
{
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    public static readonly JsonSerializerOptions Indented = new(Options)
    {
        WriteIndented = true,
    };

    public static JsonObject ParseObject(string json) => JsonNode.Parse(json)?.AsObject()
        ?? throw new ArgumentException("Expected JSON object");

    public static JsonObject? TryParseObject(string? json)
    {
        if (string.IsNullOrEmpty(json)) return null;
        try
        {
            return JsonNode.Parse(json) as JsonObject;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public static JsonNode? Parse(string json) => JsonNode.Parse(json);

    public static string Serialize(object? value) => value switch
    {
        null => "null",
        JsonNode node => node.ToJsonString(Options),
        _ => JsonSerializer.Serialize(value, Options)
    };

    public static T? Deserialize<T>(string json) => JsonSerializer.Deserialize<T>(json, Options);

    public static JsonNode? ToNode(object? value) => value switch
    {
        null => null,
        JsonNode node => node,
        _ => JsonSerializer.SerializeToNode(value, Options)
    };

    public static string? GetString(this JsonObject? obj, string name) =>
        obj?.TryGetPropertyValue(name, out var node) == true && node is JsonValue v && v.TryGetValue<string>(out var s) ? s : null;

    public static bool GetBool(this JsonObject? obj, string name, bool defaultValue = false) =>
        obj?.TryGetPropertyValue(name, out var node) == true && node is JsonValue v && v.TryGetValue<bool>(out var b) ? b : defaultValue;

    public static int? GetInt(this JsonObject? obj, string name)
    {
        if (obj?.TryGetPropertyValue(name, out var node) != true || node is not JsonValue v)
            return null;
        if (v.TryGetValue<int>(out var i)) return i;
        if (v.TryGetValue<double>(out var d)) return (int)d;
        if (v.TryGetValue<string>(out var s) && int.TryParse(s, out var parsed)) return parsed;
        return null;
    }

    public static long? GetLong(this JsonObject? obj, string name)
    {
        if (obj?.TryGetPropertyValue(name, out var node) != true || node is not JsonValue v)
            return null;
        if (v.TryGetValue<long>(out var l)) return l;
        // a JsonValue built from a C# int won't widen on its own, unlike one parsed from JSON
        if (v.TryGetValue<int>(out var i)) return i;
        if (v.TryGetValue<double>(out var d)) return (long)d;
        if (v.TryGetValue<string>(out var s) && long.TryParse(s, out var parsed)) return parsed;
        return null;
    }

    public static double? GetDouble(this JsonObject? obj, string name)
    {
        if (obj?.TryGetPropertyValue(name, out var node) != true || node is not JsonValue v)
            return null;
        if (v.TryGetValue<double>(out var d)) return d;
        if (v.TryGetValue<string>(out var s) && double.TryParse(s, out var parsed)) return parsed;
        return null;
    }

    public static JsonObject? GetObject(this JsonObject? obj, string name) =>
        obj?.TryGetPropertyValue(name, out var node) == true ? node as JsonObject : null;

    public static JsonArray? GetArray(this JsonObject? obj, string name) =>
        obj?.TryGetPropertyValue(name, out var node) == true ? node as JsonArray : null;

    /// <summary>Deep-clone a node so it can be attached to a different parent</summary>
    public static T Clone<T>(this T node) where T : JsonNode => (T)node.DeepClone();

    /// <summary>Python-style error response: {"responseStatus":{"errorCode":...,"message":...}}</summary>
    public static JsonObject CreateErrorResponse(string message, string errorCode = "Error", string? stackTrace = null)
    {
        var status = new JsonObject
        {
            ["errorCode"] = errorCode,
            ["message"] = message,
        };
        if (stackTrace != null)
            status["stackTrace"] = stackTrace;
        return new JsonObject { ["responseStatus"] = status };
    }

    public static JsonObject ToErrorResponse(Exception e, bool stackTrace = false) =>
        CreateErrorResponse(ToErrorMessage(e), errorCode: e is IHasErrorCode { ErrorCode: not null } ec ? ec.ErrorCode : "Error",
            stackTrace: stackTrace ? e.StackTrace : null);

    public static string ToErrorMessage(Exception e)
    {
        var msg = e.Message;
        if (msg.StartsWith('{'))
        {
            var obj = TryParseObject(msg);
            var inner = obj.GetString("message") ?? obj.GetString("error");
            if (inner != null)
                return inner;
        }
        return msg;
    }
}
