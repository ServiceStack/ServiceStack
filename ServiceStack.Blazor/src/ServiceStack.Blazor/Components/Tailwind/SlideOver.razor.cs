using Microsoft.AspNetCore.Components;

namespace ServiceStack.Blazor.Components.Tailwind;

/// <summary>
/// Display ChildContent in a Slide Over Dialog
/// </summary>
/// <remarks>
/// ![](https://raw.githubusercontent.com/ServiceStack/docs/master/docs/images/blazor/components/SlideOver.png)
/// </remarks>
public partial class SlideOver : BlazorComponentBase
{
    [Parameter, EditorRequired] public RenderFragment ChildContent { get; set; }
    [Parameter] public string SlideOverClass { get; set; } = CssDefaults.SlideOver.SlideOverClass;
    [Parameter] public string DialogClass { get; set; } = CssDefaults.SlideOver.DialogClass;
    [Parameter] public string PanelClass { get; set; } = CssDefaults.SlideOver.PanelClass;
    [Parameter] public string FormClass { get; set; } = CssDefaults.SlideOver.FormClass;
    [Parameter] public string TitlebarClass { get; set; } = CssDefaults.SlideOver.TitlebarClass;
    [Parameter] public string HeadingClass { get; set; } = CssDefaults.SlideOver.HeadingClass;
    [Parameter] public string CloseButtonClass { get; set; } = CssDefaults.Form.CloseButtonClass;
    [Parameter] public string? Title { get; set; }
    [Parameter] public string? SubHeading { get; set; }
    [Parameter] public bool ShowTitlebar { get; set; } = true;
    [Parameter] public bool ShowCloseButton { get; set; } = true;
    [Parameter] public bool ShowFooter { get; set; } = true;

    [Parameter] public EventCallback Done { get; set; }

    protected DataTransition SlideOverTransition = new DataTransition(
        entering: new(@class: CssDefaults.SlideOver.TransitionClass, from: "translate-x-full", to: "translate-x-0"),
        leaving:  new(@class: CssDefaults.SlideOver.TransitionClass, from: "translate-x-0", to: "translate-x-full"),
        visible: false);

    public async Task CloseAsync()
    {
        await DataTransition.TransitionAllAsync(
            show: false,
            onChange: StateHasChanged,
            SlideOverTransition
        );
        await Task.Delay(500 - SlideOverTransition.DelayMs);
        await Done.InvokeAsync();
    }

    protected override async Task OnParametersSetAsync()
    {
        await base.OnParametersSetAsync();
        await DataTransition.TransitionAllAsync(
            show: true,
            onChange: StateHasChanged,
            SlideOverTransition
        );
    }
}
