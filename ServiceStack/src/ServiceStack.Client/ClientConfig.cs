using System;
using System.Net;
using ServiceStack.Text;

namespace ServiceStack;

public static class ClientConfig
{
    public static bool SkipEmptyArrays { get; set; } = false;

    public static bool ImplicitRefInfo { get; set; } = true;

    public static Func<string, object> EvalExpression { get; set; }

    public static void ConfigureTls12()
    {
        //https://githubengineering.com/crypto-removal-notice/
        ServicePointManager.Expect100Continue = true;
        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
    }

    public static Func<string, string> EncodeDispositionFileName { get; set; } = DefaultEncodeDispositionFileName;

    public static string DefaultEncodeDispositionFileName(string fileName) =>
        fileName.UrlEncode().Replace("+", "%20");

    public static void Reset()
    {
#if NET8_0_OR_GREATER
        UseSystemJson = UseSystemJson.Never;
#endif
    }
    
#if NET8_0_OR_GREATER
    /// <summary>
    /// Use System.Text JSON for JsonApiClient
    /// </summary>
    public static UseSystemJson UseSystemJson { get; set; } = UseSystemJson.Never;

    /// <summary>
    /// Config scope of ServiceStack.Text when System.Text.Json is enabled
    /// </summary>
    public static Text.Config SystemJsonTextConfig { get; set; } = new()
    {
        TextCase = TextCase.CamelCase,
        SystemJsonCompatible = true
    };

    public static System.Text.Json.JsonSerializerOptions DefaultSystemJsonOptions() => new()
    {
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
        Converters = {
            new SystemJson.JsonEnumMemberStringEnumConverter(),
            new SystemJson.XsdTimeSpanJsonConverter(),
            new SystemJson.XsdTimeOnlyJsonConverter(),
        },
        TypeInfoResolver = SystemJson.DataContractResolver.Instance,
    };

    public static System.Text.Json.JsonSerializerOptions SystemJsonOptions { get; set; } = DefaultSystemJsonOptions();
#endif

    public static string ToJson<T>(T obj)
    {
#if NET8_0_OR_GREATER
        var useSystemJson = typeof(T).FirstAttribute<SystemJsonAttribute>()?.Use ?? UseSystemJson;
        if (useSystemJson.HasFlag(UseSystemJson.Request))
        {
            return System.Text.Json.JsonSerializer.Serialize(obj, SystemJsonOptions);
        }
        using (UseSystemJson != UseSystemJson.Never ? JsConfig.With(SystemJsonTextConfig) : null)
        {
            return obj.ToJson();
        }
#else
        return obj.ToJson();
#endif
    }

    public static T FromJson<T>(string json, Type requestType = null)
    {
#if NET8_0_OR_GREATER
        var useSystemJson = (requestType ?? typeof(T)).FirstAttribute<SystemJsonAttribute>()?.Use ?? UseSystemJson;
        if (useSystemJson.HasFlag(UseSystemJson.Response))
        {
            return System.Text.Json.JsonSerializer.Deserialize<T>(json, SystemJsonOptions);
        }
        using (UseSystemJson != UseSystemJson.Never ? JsConfig.With(SystemJsonTextConfig) : null)
        {
            return json.FromJson<T>();
        }
#else
        return json.FromJson<T>();
#endif
    }
}
