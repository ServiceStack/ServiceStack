using Microsoft.AspNetCore.Components;

namespace ServiceStack.Blazor.Components;

/// <summary>
/// The Base class for all ResponseStatus aware ServiceStack.Blazor Components
/// </summary>
public abstract class ApiComponentBase : UiComponentBase
{

    /// <summary>
    /// Directly assing a Response Status to this component
    /// </summary>
    [Parameter] public ResponseStatus? Status { get; set; }

    /// <summary>
    /// The ResponseStatus injected by CascadingValue
    /// </summary>
    [CascadingParameter] public ResponseStatus? CascadingStatus { get; set; }

    /// <summary>
    /// The ResponseStatus assinged to this compontent
    /// </summary>
    protected ResponseStatus? UseStatus => Status ?? (!IgnoreCascadingStatus ? CascadingStatus : null);

    /// <summary>
    /// Assign ResponseStatus to component and ignore CascadingStatus injected by CascadingValue
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

    /// <summary>
    /// Whether to ignore CascadingStatus
    /// </summary>
    protected bool IgnoreCascadingStatus { get; set; }

    /// <summary>
    /// If the ResponseStatus assigned to this component is in an Error State
    /// </summary>
    protected bool IsError => UseStatus.IsError();

    /// <summary>
    /// Helper to return classes for when component is in a `valid` or `invalid` state
    /// </summary>
    /// <param name="valid">css classes to include when valid</param>
    /// <param name="invalid">css classes to include when invalid</param>
    /// <returns></returns>
    protected virtual string StateClasses(string? valid = null, string? invalid = null) => !IsError
        ? valid ?? ""
        : invalid ?? "";

    /// <inheritdoc/>
    protected override string CssClass(string? valid = null, string? invalid = null) =>
        CssUtils.ClassNames(StateClasses(valid, invalid), @class);
}