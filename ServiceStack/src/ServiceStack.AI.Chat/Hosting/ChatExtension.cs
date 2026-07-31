using Microsoft.Extensions.Logging;

namespace ServiceStack.AI;

/// <summary>
/// A built-in chat extension — the C# port of llms-py's extension modules
/// (llms/extensions/&lt;name&gt;/__init__.py with __install__/__load__ hooks).
/// Unlike Python there is no dynamic loading: built-ins are instantiated by ChatFeature
/// and hosts can add their own via ChatFeature.Extensions.
/// </summary>
public abstract class ChatExtension(string name)
{
    /// <summary>Extension id, e.g. "app", "gallery" — becomes the /ext/&lt;name&gt; route prefix</summary>
    public string Name { get; } = name;

    /// <summary>Indicates whether the extension is disabled</summary>
    public bool Disabled { get; set; }

    /// <summary>Indicates whether the extension is enabled</summary>
    public bool Enabled
    {
        get => !Disabled;
        set => Disabled = !value;
    }

    /// <summary>Register routes, filters, tools and providers (port of __install__(ctx))</summary>
    public abstract void Install(ExtensionContext ctx);

    /// <summary>Async post-install hook, run concurrently for all extensions (port of __load__(ctx))</summary>
    public virtual Task LoadAsync(ExtensionContext ctx, CancellationToken token = default) => Task.CompletedTask;
    
    public ExtensionContext Ctx { get; set; } = null!;
    public ILogger Log => Ctx.Log;
    public ChatFeature Feature => Ctx.Feature;
}
