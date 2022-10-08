using Microsoft.AspNetCore.Components;

namespace ServiceStack.Blazor.Components.Tailwind;

/// <summary>
/// Require explicit confirmation before deleting
/// ![](https://raw.githubusercontent.com/ServiceStack/docs/master/docs/images/blazor/components/ConfirmDelete.png)
/// </summary>
public partial class ConfirmDelete : UiComponentBase
{
    bool confirmDelete;
    [Parameter, EditorRequired] public EventCallback OnDelete { get; set; }
    [Parameter] public string ConfirmLabel { get; set; } = "confirm";
    [Parameter] public RenderFragment? ChildContent { get; set; }
}
