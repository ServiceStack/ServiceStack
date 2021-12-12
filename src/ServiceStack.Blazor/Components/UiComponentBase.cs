#pragma warning disable IDE1006 // Naming Styles

using Microsoft.AspNetCore.Components;

namespace ServiceStack.Blazor.Components;

/// <summary>
/// The Base class for all ServiceStack.Blazor Components
/// </summary>
public abstract class UiComponentBase : ComponentBase
{
    /// <summary>
    /// Optional user defined classes for this component
    /// </summary>
    [Parameter] public string? @class { get; set; }

    /// <summary>
    /// Return any user-defined classes along with optional classes for when component is in a `valid` or `invalid` state
    /// </summary>
    /// <param name="valid">css classes to include when valid</param>
    /// <param name="invalid">css classes to include when invalid</param>
    /// <returns></returns>
    protected virtual string CssClass(string? valid = null, string? invalid = null) =>
        CssUtils.ClassNames(@class);

    /// <summary>
    /// Helper to combine multiple css classes. Strings can contain multiple classes, empty strings are ignored.
    /// </summary>
    /// <param name="classes"></param>
    /// <returns></returns>
    protected virtual string ClassNames(params string?[] classes) => CssUtils.ClassNames(classes);
}
