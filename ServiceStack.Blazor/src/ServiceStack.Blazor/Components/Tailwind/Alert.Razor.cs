using Microsoft.AspNetCore.Components;

namespace ServiceStack.Blazor.Components.Tailwind;

/// <summary>
/// Show Alert Message
/// ![](https://raw.githubusercontent.com/ServiceStack/docs/master/docs/images/blazor/components/Alert.png)
/// </summary>
public partial class Alert : UiComponentBase
{
    [Parameter] public string HtmlMessage { get; set; } = "";
    [Parameter] public RenderFragment? ChildContent { get; set; }
    [Parameter] public AlertType Type { get; set; } = AlertType.Warning;
}
