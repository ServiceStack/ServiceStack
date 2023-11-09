#pragma warning disable IDE1006 // Naming Styles

using Microsoft.AspNetCore.Components;

namespace ServiceStack.Blazor.Components;

public abstract class UiComponentLiteBase : ComponentBase
{
    /// <summary>
    /// Optional user defined classes for this component
    /// </summary>
    [Parameter] public string? @class { get; set; }
    public string? Class => @class;

    [Parameter(CaptureUnmatchedValues = true)]
    public IDictionary<string, object?>? AdditionalAttributes { get; set; }

    /// <summary>
    /// Helper to combine multiple css classes. Strings can contain multiple classes, empty strings are ignored.
    /// </summary>
    /// <param name="classes"></param>
    /// <returns></returns>
    protected virtual string ClassNames(params string?[] classes) => CssUtils.ClassNames(classes);
}
