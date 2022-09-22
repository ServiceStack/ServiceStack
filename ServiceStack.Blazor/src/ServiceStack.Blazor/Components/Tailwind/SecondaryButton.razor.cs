using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace ServiceStack.Blazor.Components.Tailwind;

public partial class SecondaryButton : UiComponentBase
{
    [Parameter] public string type { get; set; } = "button";
    [Parameter, EditorRequired] public RenderFragment? ChildContent { get; set; }
    [Parameter] public EventCallback<MouseEventArgs> onclick { get; set; }
}
