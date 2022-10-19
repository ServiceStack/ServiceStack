using Microsoft.AspNetCore.Components;

namespace ServiceStack.Blazor.Components.Tailwind;

public partial class NavList : UiComponentBase
{
    [Parameter] public string? Title { get; set; }
    [Parameter] public RenderFragment? ChildContent { get; set; }

    readonly List<NavListItem> items = new();

    internal void AddItem(NavListItem item)
    {
        items.Add(item);
    }
}
