using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ServiceStack.Blazor.Components;
using System.IO;
using System.Text.Json.Serialization;
using ServiceStack.Web;

namespace ServiceStack.Blazor;

/// <summary>
/// Default conventions and behavior used by ServiceStack.Blazor Components
/// </summary>
public class BlazorConfig
{
    public static BlazorConfig Instance { get; private set; } = new();
    public static void Set(BlazorConfig config)
    {
        Instance = config;
    }

    /// <summary>
    /// IOC used to resolve App dependencies (e.g. ILoggerFactory)
    /// </summary>
    public IServiceProvider? Services { get; set; }
    /// <summary>
    /// Custom ILogger for Blazor Components to use (default uses ILoggerFactory)
    /// </summary>
    public ILogger? Log { get; set; }
    public ILogger? GetLog()
    {
        return Log ??= Services?.GetService<ILoggerFactory>()?.CreateLogger<BlazorConfig>();
    }

    /// <summary>
    /// Flag App can use to detect if running in Blazor WASM or Blazor Server
    /// </summary>
    public bool IsDevelopment { get; init; }
    /// <summary>
    /// Flag App can use to detect if running in Blazor WASM or Blazor Server
    /// </summary>
    public bool IsWasm { get; init; }
    /// <summary>
    /// Whether Components should be rendered in Dark Mode
    /// </summary>
    public bool DarkMode { get; internal set; }
    /// <summary>
    /// Enable Error Logging (default true)
    /// </summary>
    public bool EnableErrorLogging { get; init; } = true;
    /// <summary>
    /// Enable Verbose Logging (default false)
    /// </summary>
    public bool EnableVerboseLogging { get; init; } = false;
    /// <summary>
    /// Enable Verbose Logging (default false)
    /// </summary>
    public bool EnableLogging { get; init; } = false;
    /// <summary>
    /// Max Field Length in Format components (default 150)
    /// </summary>
    public int MaxFieldLength { get; init; } = 150;
    /// <summary>
    /// Max Number of Fields in Format components (default 2)
    /// </summary>
    public int MaxNestedFields { get; init; } = 2;
    /// <summary>
    /// Max Field Length in Nested Types in Format components (default 30)
    /// </summary>
    public int MaxNestedFieldLength { get; init; } = 30;
    /// <summary>
    /// Sign In Page to redirect for Unauthorized access to protected compontents (default /signin)
    /// </summary>
    public string RedirectSignIn { get; init; } = "/signin";
    
    /// <summary>
    /// Whether to Register ILocalStorage with AddBlazorApiClient()
    /// </summary>
    public bool UseLocalStorage { get; init; } = true;
    /// <summary>
    /// Image URI to use when No ProfileUrl exists
    /// </summary>
    public string DefaultProfileUrl { get; init; } = "data:image/svg+xml,%3Csvg width='100' height='100' viewBox='0 0 100 100' xmlns='http://www.w3.org/2000/svg'%3E %3Cstyle%3E .path%7B%7D %3C/style%3E %3Cg id='male-svg'%3E%3Cpath fill='%23556080' d='M1 92.84V84.14C1 84.14 2.38 78.81 8.81 77.16C8.81 77.16 19.16 73.37 27.26 69.85C31.46 68.02 32.36 66.93 36.59 65.06C36.59 65.06 37.03 62.9 36.87 61.6H40.18C40.18 61.6 40.93 62.05 40.18 56.94C40.18 56.94 35.63 55.78 35.45 47.66C35.45 47.66 32.41 48.68 32.22 43.76C32.1 40.42 29.52 37.52 33.23 35.12L31.35 30.02C31.35 30.02 28.08 9.51 38.95 12.54C34.36 7.06 64.93 1.59 66.91 18.96C66.91 18.96 68.33 28.35 66.91 34.77C66.91 34.77 71.38 34.25 68.39 42.84C68.39 42.84 66.75 49.01 64.23 47.62C64.23 47.62 64.65 55.43 60.68 56.76C60.68 56.76 60.96 60.92 60.96 61.2L64.74 61.76C64.74 61.76 64.17 65.16 64.84 65.54C64.84 65.54 69.32 68.61 74.66 69.98C84.96 72.62 97.96 77.16 97.96 81.13C97.96 81.13 99 86.42 99 92.85L1 92.84Z'/%3E%3C/g%3E%3C/svg%3E";

    public ImageInfo DefaultTableIcon { get; init; } = new ImageInfo { Svg = "<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 24 24'><g fill='none' stroke='currentColor' stroke-width='1.5'><path d='M5 12v6s0 3 7 3s7-3 7-3v-6'/><path d='M5 6v6s0 3 7 3s7-3 7-3V6'/><path d='M12 3c7 0 7 3 7 3s0 3-7 3s-7-3-7-3s0-3 7-3Z'/></g></svg>" };
    
    /// <summary>
    /// Capture the Server Api BaseUrl
    /// </summary>
    public string? ApiBaseUrl { get; init; }
    /// <summary>
    /// Prefix added to relative Asset URLs
    /// </summary>
    public string? AssetsBasePath { get; init; }
    /// <summary>
    /// Prefix used to add to fallback URLs when default Asset Path fails
    /// </summary>
    public string? FallbackAssetsBasePath { get; init; }
    /// <summary>
    /// Use custom strategy for resolving Asset Paths
    /// </summary>
    public Func<string, string> AssetsPathResolver { get; init; } = DefaultAssetsPathResolver;
    /// <summary>
    /// Use custom strategy for resolving Fallback Asset Paths
    /// </summary>
    public Func<string, string> FallbackPathResolver { get; init; } = DefaultFallbackPathResolver;
    static bool IsRelative(string path) => path.IndexOf("://") == -1 && !path.StartsWith("//") && !path.StartsWith("data:") && !path.StartsWith("blob:");
    public static string DefaultAssetsPathResolver(string path)
    {
        return IsRelative(path) && Instance.AssetsBasePath != null
            ? Instance.AssetsBasePath.CombineWith(path)
            : path;
    }
    public static string DefaultFallbackPathResolver(string path)
    {
        return IsRelative(path) && Instance.FallbackAssetsBasePath != null
            ? Instance.FallbackAssetsBasePath.CombineWith(path)
            : FileIcons.SvgToDataUri(FileIcons.Icons["img"]);
    }
    /// <summary>
    /// Whether ApiAsync BlazorComponentBase APIs should use IServiceGateway instead of JsonApiClient in Blazor Server by default
    /// </summary>
    public bool UseInProcessClient { get; init; } = true;
    
    /// <summary>
    /// Change defaults for AutoQueryGrid Components
    /// </summary>
    public AutoQueryGridDefaults AutoQueryGridDefaults { get; init; } = new();

    /// <summary>
    /// Function used to parse JS Object literals
    /// </summary>
    public Func<string, Dictionary<string, object>> JSParseObject { get; init; } = DefaultJSObjectParser;

    /// <summary>
    /// Function used to evaluate script expressions in Inputs
    /// </summary>
    public Func<string,object?> EvalExpression
    {
        get => ClientConfig.EvalExpression;
        set => ClientConfig.EvalExpression = value;
    }

    public static Dictionary<string,object> DefaultJSObjectParser(string js)
    {
        // Hack till we port the proper JS.eval() Object literal parser to ServiceStack.Text
        // Blazor Server can set JSParseObject = JS.ParseObject
        var to = new Dictionary<string, object>();
        try
        {
            var toJsv = js.Replace('\'', '"');
            var strAttrs = toJsv.FromJsv<Dictionary<string, string>>() ?? new();
            foreach (var entry in strAttrs)
            {
                to[entry.Key.Trim()] = entry.Value;
            }
        }
        catch (Exception e)
        {
            Instance.GetLog()?.LogError(e, "Could not parse JS {0}", js);
        }
        return to;
    }

    public System.Text.Json.JsonSerializerOptions FormatJsonOptions { get; init; } = new() {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
    };

    /// <summary>
    /// Default Filter Conventions to use in DataGrid (for non AutoQuery APIs)
    /// </summary>
    public List<AutoQueryConvention> DefaultFilters { get; init; } = new() {
        Definition("=","%"),
        Definition("!=","%!"),
        Definition("<","<%"),
        Definition("<=","%<"),
        Definition(">","%>"),
        Definition(">=",">%"),
        Definition("In","%In"),
        Definition("Starts With","%StartsWith", x => x.Types = "string"),
        Definition("Contains","%Contains", x => x.Types = "string"),
        Definition("Ends With","%EndsWith", x => x.Types = "string"),
        Definition("Exists","%IsNotNull", x => x.ValueType = "none"),
        Definition("Not Exists","%IsNull", x => x.ValueType = "none"),
    };

    static AutoQueryConvention Definition(string name, string value, Action<AutoQueryConvention>? fn = null) =>
        X.Apply(new() { Name = name, Value = value }, fn);

    public bool ToggleDarkMode(bool? value = null)
    {
        return DarkMode = value ?? !DarkMode;
    }

    public Func<object, IHasErrorStatus, Task>? OnApiErrorAsync { get; init; }
}

public class AutoQueryGridDefaults
{
    public bool AllowSelection { get; set; } = true;
    public bool AllowFiltering { get; set; } = true;
    public bool AllowQueryFilters { get; set; } = true;

    public bool ShowToolbar { get; set; } = true;
    public bool ShowPreferences { get; set; } = true;
    public bool ShowPagingNav { get; set; } = true;
    public bool ShowPagingInfo { get; set; } = true;
    public bool ShowDownloadCsv { get; set; } = true;
    public bool ShowRefresh { get; set; } = true;
    public bool ShowCopyApiUrl { get; set; } = true;
    public bool ShowResetPreferences { get; set; } = true;
    public bool ShowFiltersView { get; set; } = true;
    public bool ShowNewItem { get; set; } = true;
    public TableStyle TableStyle { get; set; } = CssDefaults.Grid.DefaultTableStyle;
    public string ToolbarButtonClass { get; set; } = CssUtils.Tailwind.ToolbarButtonClass;
    public int MaxFieldLength { get; set; } = 150;
}
