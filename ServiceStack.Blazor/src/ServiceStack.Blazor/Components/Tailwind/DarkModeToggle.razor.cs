using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace ServiceStack.Blazor.Components.Tailwind;

/// <summary>
/// Button to Toggle BlazorConfig.HtmlClass to dark mode
/// </summary>
public partial class DarkModeToggle : UiComponentBase
{
    [Inject] public LocalStorage LocalStorage { get; set; }
    [Inject] public IJSRuntime JS { get; set; }

    const string Key = "color-scheme";

    async Task toggleDark()
    {
        if (BlazorConfig.Instance.ToggleDarkMode())
        {
            await LocalStorage.SetStringAsync(Key, "dark");
            await JS.InvokeVoidAsync("JS.addClass", "html", "dark");
        }
        else
        {
            await LocalStorage.RemoveAsync(Key);
            await JS.InvokeVoidAsync("JS.removeClass", "html", "dark");
        }
        StateHasChanged();
    }


    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        if (firstRender)
        {
            var darkMode = await JS.InvokeAsync<bool>("JS.containsClass", "html", "dark");
            BlazorConfig.Instance.ToggleDarkMode(darkMode);
        }
    }
}
