using System;
using System.Linq;
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
    /// Serialize to Object to Indented Json with System.Text JSON
    /// </summary>
    public static string ToSystemJson<T>(T obj)
    {
        var options = TextConfig.CreateSystemJsonOptions();
        options.WriteIndented = true;
        return System.Text.Json.JsonSerializer.Serialize(obj, options);
    }

    /// <summary>
    /// Print Indented and unquoted System.Text JSON output to the Console
    /// </summary>
    public static void PrintSystemJson<T>(T obj)
    {
        var json = ToSystemJson(obj);
        Console.WriteLine(json.Replace("\"", ""));
    }
#endif

    public static string ToJson<T>(T obj)
    {
#if NET8_0_OR_GREATER
        var useSystemJson = (typeof(T)
            .GetCustomAttributes(typeof(SystemJsonAttribute), true)
            .FirstOrDefault() as SystemJsonAttribute)?.Use ?? UseSystemJson;
        if (useSystemJson.HasFlag(UseSystemJson.Request))
        {
            return System.Text.Json.JsonSerializer.Serialize(obj, TextConfig.SystemJsonOptions);
        }
        using (UseSystemJson != UseSystemJson.Never ? JsConfig.With(TextConfig.SystemJsonTextConfig) : null)
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
        var useSystemJson = (typeof(T)
            .GetCustomAttributes(typeof(SystemJsonAttribute), true)
            .FirstOrDefault() as SystemJsonAttribute)?.Use ?? UseSystemJson;
        if (useSystemJson.HasFlag(UseSystemJson.Response))
        {
            return System.Text.Json.JsonSerializer.Deserialize<T>(json, TextConfig.SystemJsonOptions);
        }
        using (UseSystemJson != UseSystemJson.Never ? JsConfig.With(TextConfig.SystemJsonTextConfig) : null)
        {
            return json.FromJson<T>();
        }
#else
        return json.FromJson<T>();
#endif
    }

    public static object FromJson(Type type, string json)
    {
#if NET8_0_OR_GREATER
        var useSystemJson = (type
            .GetCustomAttributes(typeof(SystemJsonAttribute), true)
            .FirstOrDefault() as SystemJsonAttribute)?.Use ?? UseSystemJson;
        if (useSystemJson.HasFlag(UseSystemJson.Response))
        {
            return System.Text.Json.JsonSerializer.Deserialize(json, type, TextConfig.SystemJsonOptions);
        }
        using (UseSystemJson != UseSystemJson.Never ? JsConfig.With(TextConfig.SystemJsonTextConfig) : null)
        {
            return JsonSerializer.DeserializeFromString(json, type);
        }
#else
        return JsonSerializer.DeserializeFromString(json, type);
#endif
    }

#if NET8_0_OR_GREATER
    public static string IndentJson(string json)
    {
        using var document = System.Text.Json.JsonDocument.Parse(json);
        string formattedJson = System.Text.Json.JsonSerializer.Serialize(document, new System.Text.Json.JsonSerializerOptions { 
            WriteIndented = true 
        });
        return formattedJson;
    }
#endif
}
