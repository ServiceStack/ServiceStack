using System;
using System.Security.Cryptography;

namespace ServiceStack.Text;

public class TextConfig
{
    public static Func<SHA1> CreateSha { get; set; } = SHA1.Create;

#if NET8_0_OR_GREATER
    /// <summary>
    /// Config scope of ServiceStack.Text when System.Text.Json is enabled
    /// </summary>
    public static Config SystemJsonTextConfig { get; set; } = new()
    {
        TextCase = TextCase.CamelCase,
        SystemJsonCompatible = true
    };

    public static System.Text.Json.JsonSerializerOptions DefaultSystemJsonOptions() => new()
    {
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
        Converters = {
            new SystemJson.JsonEnumMemberStringEnumConverter(),
            new SystemJson.XsdTimeSpanJsonConverter(),
            new SystemJson.XsdTimeOnlyJsonConverter(),
        },
        TypeInfoResolver = SystemJson.DataContractResolver.Instance,
    };

    public static System.Text.Json.JsonSerializerOptions SystemJsonOptions { get; set; } = DefaultSystemJsonOptions();

    public static System.Text.Json.JsonSerializerOptions CustomSystemJsonOptions(System.Text.Json.JsonSerializerOptions systemJsonOptions, JsConfigScope jsScope)
    {
        var to = new System.Text.Json.JsonSerializerOptions(systemJsonOptions);
        if (jsScope.TextCase != TextCase.Default)
        {
            to.PropertyNamingPolicy = jsScope.TextCase switch {
                TextCase.CamelCase => System.Text.Json.JsonNamingPolicy.CamelCase,
                TextCase.SnakeCase => System.Text.Json.JsonNamingPolicy.SnakeCaseLower,
                _ => null
            };
        }
        if (jsScope.ExcludeDefaultValues)
        {
            to.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingDefault;
        }
        if (jsScope.IncludeNullValues)
        {
            to.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.Never;
        }
        if (jsScope.Indent)
        {
            to.WriteIndented = true;
        }
        return to;
    }
#endif

}
