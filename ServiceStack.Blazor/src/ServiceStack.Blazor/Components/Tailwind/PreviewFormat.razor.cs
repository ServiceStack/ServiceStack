﻿using ServiceStack.Text;
using Microsoft.AspNetCore.Components;

namespace ServiceStack.Blazor.Components.Tailwind;

/// <summary>
/// Customize how Table Cell data are rendered into different UX-Friendly formats
/// </summary>
/// <remarks>
/// ![](https://raw.githubusercontent.com/ServiceStack/docs/master/docs/images/blazor/components/PreviewFormat.png)
/// </remarks>
public partial class PreviewFormat
{
    [Parameter] public object? Value { get; set; }
    [Parameter] public string? @class { get; set; } = CssDefaults.PreviewFormat.Class;
    [Parameter] public string? IconClass { get; set; } = CssDefaults.PreviewFormat.IconClass;
    [Parameter] public string? IconRoundedClass { get; set; } = CssDefaults.PreviewFormat.IconRoundedClass;
    [Parameter] public string? ValueIconClass { get; set; } = CssDefaults.PreviewFormat.ValueIconClass;
    [Parameter] public FormatInfo? Format { get; set; }
    [Parameter] public bool IncludeIcon { get; set; } = true;
    [Parameter] public bool IncludeCount { get; set; } = true;
    [Parameter] public int MaxFieldLength { get; set; } = BlazorConfig.Instance.MaxFieldLength;
    [Parameter] public int MaxNestedFields { get; set; } = BlazorConfig.Instance.MaxNestedFields;
    [Parameter] public int MaxNestedFieldLength { get; set; } = BlazorConfig.Instance.MaxNestedFieldLength;
    string FormatValue(object? value) => BlazorUtils.FormatValue(value, MaxFieldLength);

    string? UseImageSrc { get; set; }
}
