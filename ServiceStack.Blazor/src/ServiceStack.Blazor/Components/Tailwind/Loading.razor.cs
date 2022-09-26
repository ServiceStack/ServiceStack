using System;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace ServiceStack.Blazor.Components.Tailwind;

public partial class Loading : UiComponentBase
{
    [Parameter] public string Message { get; set; } = "Loading...";
    [Inject] IJSRuntime JS { get; set; }
    [Inject] NavigationManager NavigationManager { get; set; }

    string prerenderedHtml { get; set; } = "";

    [Parameter]
    public string ImageClass { get; set; } = "w-6 h-6";

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        if (firstRender)
        {
            var html = await JS.InvokeAsync<string>("JS.prerenderedPage") ?? "";
            var currentPath = new Uri(NavigationManager.Uri).AbsolutePath;
            var prerenderedContentIsForPath = html.IndexOf($"data-prerender=\"{currentPath}\"") >= 0;
            if (prerenderedContentIsForPath)
            {
                prerenderedHtml = html;
            }
        }
    }
}
