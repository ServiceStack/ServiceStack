#nullable enable

using System;
using System.Collections.Generic;
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

    public static void ConfigureJsonOptions(Action<System.Text.Json.JsonSerializerOptions> configure)
    {
        SystemJsonOptionFilters.Add(configure);
        SystemJsonOptions = CreateSystemJsonOptions();
    }

    public static List<Action<System.Text.Json.JsonSerializerOptions>> SystemJsonOptionFilters { get; } =
    [
        DefaultConfigureSystemJsonOptions,
    ];
    
    public static void DefaultConfigureSystemJsonOptions(System.Text.Json.JsonSerializerOptions options)
    {
        options.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
        options.PropertyNameCaseInsensitive = true;
        options.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.Converters.Add(new SystemJson.JsonEnumMemberStringEnumConverter());
        options.Converters.Add(new SystemJson.XsdTimeSpanJsonConverter());
        options.Converters.Add(new SystemJson.XsdTimeOnlyJsonConverter());
        options.Converters.Add(new SystemJson.TypeJsonConverter());
        options.TypeInfoResolver = SystemJson.DataContractResolver.Instance;
    }

    public static void ApplySystemJsonOptions(System.Text.Json.JsonSerializerOptions options)
    {
        foreach (var configure in SystemJsonOptionFilters)
        {
            configure(options);
        }
    }

    public static System.Text.Json.JsonSerializerOptions CreateSystemJsonOptions()
    {
        var to = new System.Text.Json.JsonSerializerOptions();
        ApplySystemJsonOptions(to);
        return to;
    }

    public static System.Text.Json.JsonSerializerOptions SystemJsonOptions { get; set; } = CreateSystemJsonOptions();

    public static System.Text.Json.JsonSerializerOptions CustomSystemJsonOptions(System.Text.Json.JsonSerializerOptions systemJsonOptions, JsConfigScope jsScope)
    {
        var to = new System.Text.Json.JsonSerializerOptions(systemJsonOptions)
        {
            PropertyNamingPolicy = jsScope.TextCase switch {
                TextCase.CamelCase => System.Text.Json.JsonNamingPolicy.CamelCase,
                TextCase.SnakeCase => System.Text.Json.JsonNamingPolicy.SnakeCaseLower,
                _ => null
            }
        };
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
