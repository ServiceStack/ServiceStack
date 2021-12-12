using Microsoft.AspNetCore.Components;

namespace ServiceStack.Blazor.Components.Bootstrap;

public partial class AlertSuccess
{
    [Parameter, EditorRequired]
    public string Message { get; set; } = "";
}
