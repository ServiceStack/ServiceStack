using System.Text.Json.Serialization;

namespace ServiceStack.Blazor;

public class BlazorConfig
{
    public static BlazorConfig Instance { get; private set; } = new();
    public static void Set(BlazorConfig config)
    {
        Instance = config;
    }

    public bool EnableErrorLogging { get; init; } = true;
    public bool EnableVerboseLogging { get; init; } = false;
    public bool EnableLogging { get; init; } = false;
    public int MaxFieldLength { get; init; } = 150;
    public int MaxNestedFields { get; init; } = 2;
    public int MaxNestedFieldLength { get; init; } = 30;
    public int MaxComplexPreviewLength { get; init; } = 800;
    public int MaxComplexFieldLength { get; init; } = 50;
    public string RedirectSignIn { get; init; } = "/signin";
    public bool UseLocalStorage { get; init; } = true;
    public string DefaultProfileUrl { get; init; } = "data:image/svg+xml,%3Csvg width='100' height='100' viewBox='0 0 100 100' xmlns='http://www.w3.org/2000/svg'%3E %3Cstyle%3E .path%7B%7D %3C/style%3E %3Cg id='male-svg'%3E%3Cpath fill='%23556080' d='M1 92.84V84.14C1 84.14 2.38 78.81 8.81 77.16C8.81 77.16 19.16 73.37 27.26 69.85C31.46 68.02 32.36 66.93 36.59 65.06C36.59 65.06 37.03 62.9 36.87 61.6H40.18C40.18 61.6 40.93 62.05 40.18 56.94C40.18 56.94 35.63 55.78 35.45 47.66C35.45 47.66 32.41 48.68 32.22 43.76C32.1 40.42 29.52 37.52 33.23 35.12L31.35 30.02C31.35 30.02 28.08 9.51 38.95 12.54C34.36 7.06 64.93 1.59 66.91 18.96C66.91 18.96 68.33 28.35 66.91 34.77C66.91 34.77 71.38 34.25 68.39 42.84C68.39 42.84 66.75 49.01 64.23 47.62C64.23 47.62 64.65 55.43 60.68 56.76C60.68 56.76 60.96 60.92 60.96 61.2L64.74 61.76C64.74 61.76 64.17 65.16 64.84 65.54C64.84 65.54 69.32 68.61 74.66 69.98C84.96 72.62 97.96 77.16 97.96 81.13C97.96 81.13 99 86.42 99 92.85L1 92.84Z'/%3E%3C/g%3E%3C/svg%3E";

    public ImageInfo DefaultTableIcon { get; set; } = new ImageInfo { Svg = "<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 24 24'><g fill='none' stroke='currentColor' stroke-width='1.5'><path d='M5 12v6s0 3 7 3s7-3 7-3v-6'/><path d='M5 6v6s0 3 7 3s7-3 7-3V6'/><path d='M12 3c7 0 7 3 7 3s0 3-7 3s-7-3-7-3s0-3 7-3Z'/></g></svg>" };

    public System.Text.Json.JsonSerializerOptions FormatJsonOptions { get; set; } = new() {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
    };

public List<AutoQueryConvention> DefaultFilters { get; init; } = new List<AutoQueryConvention> {
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

}
