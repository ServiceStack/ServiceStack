using Microsoft.AspNetCore.Components;

namespace ServiceStack.Blazor.Components.Tailwind;

/// <summary>
/// Create a Tailwind Breadcrumb component
/// </summary>
/// <remarks>
/// ![](https://raw.githubusercontent.com/ServiceStack/docs/master/docs/images/blazor/components/Breadcrumbs.png)
/// </remarks>
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
