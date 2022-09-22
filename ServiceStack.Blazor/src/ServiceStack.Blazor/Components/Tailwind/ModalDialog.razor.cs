using Microsoft.AspNetCore.Components;

namespace ServiceStack.Blazor.Components.Tailwind;

public partial class ModalDialog : UiComponentBase
{
    [Parameter, EditorRequired] public string Id { get; set; }
    [Parameter, EditorRequired] public bool Show { get; set; }
    [Parameter, EditorRequired] public RenderFragment ChildContent { get; set; }
    [Parameter] public EventCallback Close { get; set; }

    DataTransition DialogTransition = new DataTransition(
        entering: new(@class: "ease-out duration-300", from: "opacity-0", to: "opacity-100"),
        leaving: new(@class: "ease-out duration-200", from: "opacity-100", to: "opacity-0"),
        visible: false);

    DataTransition ContentTransition = new DataTransition(
        entering: new(@class: "ease-out duration-300", from: "opacity-0 translate-y-4 sm:translate-y-0 sm:scale-95", to: "opacity-100 translate-y-0 sm:scale-100"),
        leaving: new(@class: "ease-in duration-200", from: "opacity-100 translate-y-0 sm:scale-100", to: "opacity-0 translate-y-4 sm:translate-y-0 sm:scale-95"),
        visible: false);

    protected override void OnInitialized()
    {
        base.OnInitialized();
        DialogTransition.Show(Show);
        ContentTransition.Show(Show);
    }

    protected override async Task OnParametersSetAsync()
    {
        await base.OnParametersSetAsync();
        await DataTransition.TransitionAllAsync(
            show: Show,
            onChange: StateHasChanged,
            DialogTransition,
            ContentTransition
        );
    }
}
