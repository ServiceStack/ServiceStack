using Microsoft.AspNetCore.Components;

namespace ServiceStack.Blazor.Components.Tailwind;

public partial class Breadcrumb : UiComponentBase
{
    [CascadingParameter]
    public Breadcrumbs? Breadcrumbs { get; set; }
    [Parameter] public string href { get; set; } = "";
    [Parameter] public RenderFragment? ChildContent { get; set; }
    [Parameter] public string? title { get; set; }

    protected override Task OnInitializedAsync()
    {
        Breadcrumbs!.AddBreadcrumb(this);
        return base.OnInitializedAsync();
    }
}
