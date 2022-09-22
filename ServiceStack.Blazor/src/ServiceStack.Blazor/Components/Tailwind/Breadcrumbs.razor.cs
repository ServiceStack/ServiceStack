using Microsoft.AspNetCore.Components;

namespace ServiceStack.Blazor.Components.Tailwind;

public partial class Breadcrumbs : UiComponentBase
{
    [Parameter] public string HomeHref { get; set; } = "/";
    [Parameter] public string HomeLabel { get; set; } = "Home";
    [Parameter] public RenderFragment ChildContent { get; set; }

    List<Breadcrumb> Links { get; set; } = new();

    public void AddBreadcrumb(Breadcrumb breadcrumb)
    {
        Links.Add(breadcrumb);
    }

    protected override void OnAfterRender(bool firstRender)
    {
        base.OnAfterRender(firstRender);
        if (firstRender) StateHasChanged();
    }
}
