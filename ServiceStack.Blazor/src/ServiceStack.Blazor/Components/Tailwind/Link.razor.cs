using Microsoft.AspNetCore.Components;

namespace ServiceStack.Blazor.Components.Tailwind;

public partial class Link : UiComponentBase
{
    [Parameter] public string href { get; set; } = "";
    [Parameter] public RenderFragment? ChildContent { get; set; }
    [Parameter] public string? title { get; set; }
    [Parameter] public string? target { get; set; }
}
