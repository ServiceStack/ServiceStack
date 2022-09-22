using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace ServiceStack.Blazor.Components.Tailwind;

public partial class TextLink : UiComponentBase
{
    [Parameter] public string href { get; set; } = "";
    [Parameter] public RenderFragment? ChildContent { get; set; }
    [Parameter] public string? title { get; set; }
    [Parameter] public string? target { get; set; }
    [Parameter] public EventCallback<MouseEventArgs> onclick { get; set; }
}
