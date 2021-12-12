using Microsoft.AspNetCore.Components;

namespace ServiceStack.Blazor.Components.Tailwind;

public partial class AlertSuccess
{
    [Parameter, EditorRequired]
    public string Message { get; set; } = "";
}
