using Microsoft.AspNetCore.Components;

namespace ServiceStack.Blazor.Components.Tailwind;

/// <summary>
/// Redirect to Sign In page if not authenticated or show access denied message if unauthorized
/// </summary>
/// <remarks>
/// ![](https://raw.githubusercontent.com/ServiceStack/docs/master/docs/images/blazor/components/EnsureAccess.png)
/// </remarks>
public partial class EnsureAccess : AuthBlazorComponentBase
{
    [Inject] NavigationManager NavigationManager { get; set; }
    [Parameter, EditorRequired] public string HtmlMessage { get; set; } = "";
    [Parameter] public EventCallback Done { get; set; }
    [Parameter] public string? Title { get; set; }
    [Parameter] public string? SubHeading { get; set; }

    async Task signIn()
    {
        NavigationManager.NavigateTo(NavigationManager.GetLoginUrl());
    }
}
