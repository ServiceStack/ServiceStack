using Microsoft.AspNetCore.Components;

namespace ServiceStack.Blazor.Components;

public class AlertSuccessBase : ApiComponentBase
{
    [Parameter] public string Message { get; set; } = "";
    [Parameter] public RenderFragment? ChildContent { get; set; }
}
