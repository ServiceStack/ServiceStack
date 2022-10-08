using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace ServiceStack.Blazor.Components.Tailwind;

/// <summary>
/// Render a tailwind hyper link
/// ![](https://raw.githubusercontent.com/ServiceStack/docs/master/docs/images/blazor/components/TextLink.png)
/// </summary>
public partial class TextLink : UiComponentBase
{
    [Parameter] public string href { get; set; } = "";
    [Parameter] public RenderFragment? ChildContent { get; set; }
    [Parameter] public string? title { get; set; }
    [Parameter] public string? target { get; set; }
    [Parameter] public EventCallback<MouseEventArgs> onclick { get; set; }
}
