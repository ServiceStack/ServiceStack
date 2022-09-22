using ServiceStack.Text;
using Microsoft.AspNetCore.Components;

namespace ServiceStack.Blazor.Components.Tailwind;

public partial class PreviewFormat
{
    [Parameter] public object? Value { get; set; }
    [Parameter] public int MaxFieldLength { get; set; } = BlazorConfig.Instance.MaxFieldLength;
    [Parameter] public int MaxNestedFields { get; set; } = BlazorConfig.Instance.MaxNestedFields;
    [Parameter] public int MaxNestedFieldLength { get; set; } = BlazorConfig.Instance.MaxNestedFieldLength;
    string Format(object? value) => BlazorUtils.FormatValue(value, MaxFieldLength);
}
