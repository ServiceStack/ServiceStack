using Microsoft.AspNetCore.Components;

namespace ServiceStack.Blazor.Components.Tailwind;

public partial class ConfirmDelete : UiComponentBase
{
    bool confirmDelete;
    [Parameter, EditorRequired] public EventCallback OnDelete { get; set; }
    [Parameter] public string ConfirmLabel { get; set; } = "confirm";
    [Parameter] public RenderFragment? ChildContent { get; set; }
}
