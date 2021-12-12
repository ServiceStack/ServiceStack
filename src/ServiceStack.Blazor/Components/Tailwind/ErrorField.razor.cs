using Microsoft.AspNetCore.Components;

namespace ServiceStack.Blazor.Components.Tailwind;

public partial class ErrorField
{
    [Parameter, EditorRequired]
    public string? Id { get; set; }
}
