#pragma warning disable IDE1006 // Naming Styles

using System.Collections.Concurrent;
using Microsoft.JSInterop;
using Microsoft.AspNetCore.Components;

namespace ServiceStack.Blazor.Components;

/// <summary>
/// The Base class for all ServiceStack.Blazor Components
/// </summary>
public abstract class UiComponentBase : ComponentBase
{
    [Inject] public IJSRuntime JS { get; set; }

    /// <summary>
    /// Optional user defined classes for this component
    /// </summary>
    [Parameter] public string? @class { get; set; }
    public string? Class => @class;

    /// <summary>
    /// Return any user-defined classes along with optional classes for when component is in a `valid` or `invalid` state
    /// </summary>
    /// <param name="valid">css classes to include when valid</param>
    /// <param name="invalid">css classes to include when invalid</param>
    /// <returns></returns>
    protected virtual string CssClass(string? valid = null, string? invalid = null) =>
        CssUtils.ClassNames(@class);

    /// <summary>
    /// Helper to combine multiple css classes. Strings can contain multiple classes, empty strings are ignored.
    /// </summary>
    /// <param name="classes"></param>
    /// <returns></returns>
    protected virtual string ClassNames(params string?[] classes) => CssUtils.ClassNames(classes);

    public static bool SanitizeAttribute(string name) => name == "@bind" || name.StartsWith("@bind:");
    public static IReadOnlyDictionary<string, object>? SanitizeAttributes(IReadOnlyDictionary<string, object>? attrs)
    {
        if (attrs == null) return null;
        var safeAttrs = new Dictionary<string, object>();
        foreach (var attr in attrs)
        {
            if (SanitizeAttribute(attr.Key))
                continue;
            safeAttrs[attr.Key] = attr.Value;
        }
        return safeAttrs;
    }

    static long renderIndex = 0;
    static ConcurrentDictionary<long, Func<IJSRuntime, Task>> RenderActions = new();
    protected virtual void QueueRenderAction(Func<IJSRuntime, Task> action) =>
        RenderActions[Interlocked.Increment(ref renderIndex)] = action;

    /// <summary>
    /// Set the document.title
    /// </summary>
    protected virtual void SetTitle(string title)
    {
        if (JS is IJSInProcessRuntime jsWasm)
            jsWasm.SetTitle(title);
        else
            QueueRenderAction(JS => JS.SetTitleAsync(title));
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        var orderedKeys = RenderActions.Keys.OrderBy(x => x).ToList();
        foreach (var key in orderedKeys)
        {
            if (RenderActions.TryRemove(key, out var action))
            {
                try
                {
                    await action(JS);
                }
                catch (Exception e)
                {
                    BlazorUtils.LogError(e, "RenderAction in {0} failed: {1}", GetType().Name, e.Message);
                }
            }
        }
        await base.OnAfterRenderAsync(firstRender);
    }

}
