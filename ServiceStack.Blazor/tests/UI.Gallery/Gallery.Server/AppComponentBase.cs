using Microsoft.AspNetCore.Components;
using MyApp.Client.Shared;

namespace MyApp.Client;

/// <summary>
/// For Pages and Components that make use of ServiceStack functionality, e.g. Client
/// </summary>
public abstract class AppComponentBase : ServiceStack.Blazor.BlazorComponentBase, IHasJsonApiClient
{
}

/// <summary>
/// For Pages and Components requiring Authentication
/// </summary>
public abstract class AppAuthComponentBase : AuthBlazorComponentBase
{
}

public static class AppLayouts
{
    static Dictionary<string, Type> Layouts = new()
    {
        [nameof(EmptyLayout)] = typeof(EmptyLayout),
        [nameof(ExampleLayout)] = typeof(ExampleLayout),
        [nameof(MainLayout)] = typeof(MainLayout),
    };
    public static Type GetPageLayout(this NavigationManager NavigationManager, Microsoft.AspNetCore.Components.RouteData route) =>
        X.Map(NavigationManager.QueryString("layout"), name => Layouts.TryGetValue(name, out var layout) ? layout : null)
            ?? route.PageType?.FirstAttribute<LayoutAttribute>()?.LayoutType ?? typeof(MainLayout);
}