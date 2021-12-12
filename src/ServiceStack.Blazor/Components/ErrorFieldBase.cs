using Microsoft.AspNetCore.Components;

namespace ServiceStack.Blazor.Components;

/// <summary>
/// Base class for rendering a Field Error adjacent to its Input Component
/// </summary>
public class ErrorFieldBase : ApiComponentBase
{
    [Parameter, EditorRequired]
    public string? Id { get; set; }
}
