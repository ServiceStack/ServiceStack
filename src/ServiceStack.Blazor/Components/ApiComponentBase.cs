#pragma warning disable IDE1006 // Naming Styles

using Microsoft.AspNetCore.Components;

namespace ServiceStack.Blazor.Components;

public abstract class ApiComponentBase : ComponentBase
{
    [Parameter] public string? @class { get; set; }

    [Parameter] public ResponseStatus? Status { get; set; }

    [CascadingParameter] public ResponseStatus? CascadingStatus { get; set; }
    protected ResponseStatus? UseStatus => Status ?? (!IgnoreCascadingStatus ? CascadingStatus : null);

    /// <summary>
    /// Whether to ignore CascadingStatus injected by CascadingValue
    /// </summary>
    [Parameter] public ResponseStatus? ExplicitStatus
    {
        set
        {
            IgnoreCascadingStatus = true;
            Status = value;
        }
        get => IgnoreCascadingStatus ? Status : null;
    }

    protected bool IgnoreCascadingStatus { get; set; }

    [Parameter] public BlazorTheme? Theme { get; set; }

    [CascadingParameter] public BlazorTheme? CascadingTheme { get; set; }
    protected BlazorTheme UseTheme => Theme ?? CascadingTheme ?? BlazorTheme.Bootstrap5;

    /// <summary>
    /// True for any Bootstrap version
    /// </summary>
    protected bool IsBootstrap => UseTheme == BlazorTheme.Bootstrap5;
    protected bool IsBootstrap5 => UseTheme == BlazorTheme.Bootstrap5;
    protected bool IsTailwind => UseTheme == BlazorTheme.Tailwind;

    protected bool IsError => UseStatus.IsError();

    protected virtual string InputClass(string? valid = null, string? invalid = null) => !IsError
        ? valid ?? ""
        : invalid ?? "";

    protected virtual string CssClass(string? valid = null, string? invalid = null) =>
        CssUtils.ClassNames(InputClass(valid, invalid), @class);
    protected virtual string ClassNames(params string?[] classes) => CssUtils.ClassNames(classes);
}