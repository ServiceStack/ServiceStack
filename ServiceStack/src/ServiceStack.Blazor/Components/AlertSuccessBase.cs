using Microsoft.AspNetCore.Components;

namespace ServiceStack.Blazor.Components;

public class AlertSuccessBase : ApiComponentBase
{
    [Parameter, EditorRequired]
    public string Message { get; set; } = "";
}
