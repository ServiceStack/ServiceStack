using Microsoft.AspNetCore.Components;
namespace ServiceStack.Blazor.Components.Tailwind;

public partial class NavListItem : UiComponentBase
{
    [CascadingParameter] public NavList? NavList { get; set; }

    [Parameter, EditorRequired] public string? Title { get; set; }
    [Parameter, EditorRequired] public string? href { get; set; }
    [Parameter] public RenderFragment? ChildContent { get; set; }

    [Parameter] public ImageInfo? Icon { get; set; }
    [Parameter] public string? IconSvg { get; set; }
    [Parameter] public string? IconSrc { get; set; }
    [Parameter] public string? IconAlt { get; set; }

    protected override void OnInitialized()
    {
        base.OnInitialized();
        NavList!.AddItem(this);
    }
}
