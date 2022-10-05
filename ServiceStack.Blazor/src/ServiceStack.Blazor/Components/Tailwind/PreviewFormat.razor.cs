using ServiceStack.Text;
using Microsoft.AspNetCore.Components;

namespace ServiceStack.Blazor.Components.Tailwind;

public partial class PreviewFormat
{
    [Parameter] public object? Value { get; set; }
    [Parameter] public string? @class { get; set; } = "flex items-center";
    [Parameter] public string? IconClass { get; set; } = "w-6 h-6 mr-1";
    [Parameter] public bool IncludeIcon { get; set; } = true;
    [Parameter] public int MaxFieldLength { get; set; } = BlazorConfig.Instance.MaxFieldLength;
    [Parameter] public int MaxNestedFields { get; set; } = BlazorConfig.Instance.MaxNestedFields;
    [Parameter] public int MaxNestedFieldLength { get; set; } = BlazorConfig.Instance.MaxNestedFieldLength;
    string Format(object? value) => BlazorUtils.FormatValue(value, MaxFieldLength);
}
