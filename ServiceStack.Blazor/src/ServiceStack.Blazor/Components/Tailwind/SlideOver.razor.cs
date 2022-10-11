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
    [Parameter] public string PanelClass { get; set; } = "pointer-events-auto w-screen xl:max-w-3xl md:max-w-xl max-w-lg";
    [Parameter] public string FormClass { get; set; } = "flex h-full flex-col divide-y divide-gray-200 bg-white shadow-xl";
    [Parameter] public string TitlebarClass { get; set; } = "bg-gray-50 px-4 py-6 sm:px-6";
    [Parameter] public string HeadingClass { get; set; } = "text-lg font-medium text-gray-900";
    [Parameter] public string? Title { get; set; }
    [Parameter] public string? SubHeading { get; set; }

    [Parameter] public EventCallback Done { get; set; }

    protected DataTransition SlideOverTransition = new DataTransition(
        entering: new(@class: "transform transition ease-in-out duration-500 sm:duration-700", from: "translate-x-full", to: "translate-x-0"),
        leaving: new(@class: "transform transition ease-in-out duration-500 sm:duration-700", from: "translate-x-0", to: "translate-x-full"),
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
