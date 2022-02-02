using Microsoft.AspNetCore.Components;

namespace ServiceStack.Blazor.Components;

/// <summary>
/// Base class for rendering a Summary Error
/// </summary>
public class ErrorSummaryBase : ApiComponentBase
{
    /// <summary>
    /// Do not show Field Error Messages for specified visible fields which will display them
    /// </summary>
    [Parameter]
    public string[]? Except { get; set; }

    protected string[] UseExcept => Except ?? Array.Empty<string>();
}
