using Microsoft.AspNetCore.Components;

namespace ServiceStack.Blazor.Components.Bootstrap;

public partial class ErrorField
{
    [Parameter, EditorRequired]
    public string? Id { get; set; }
}
