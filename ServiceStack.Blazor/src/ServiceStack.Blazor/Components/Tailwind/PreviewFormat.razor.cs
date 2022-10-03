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
    [Parameter] public int MaxComplexPreviewLength { get; set; } = BlazorConfig.Instance.MaxComplexPreviewLength;
    [Parameter] public int MaxComplexFieldLength { get; set; } = BlazorConfig.Instance.MaxComplexFieldLength;
    string Format(object? value) => BlazorUtils.FormatValue(value, MaxFieldLength);
}
