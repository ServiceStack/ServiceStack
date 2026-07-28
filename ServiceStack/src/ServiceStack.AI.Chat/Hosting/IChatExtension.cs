namespace ServiceStack.AI;

/// <summary>
/// A built-in chat extension — the C# port of llms-py's extension modules
/// (llms/extensions/&lt;name&gt;/__init__.py with __install__/__load__ hooks).
/// Unlike Python there is no dynamic loading: built-ins are instantiated by ChatFeature
/// and hosts can add their own via ChatFeature.Extensions.
/// </summary>
public interface IChatExtension
{
    /// <summary>Extension id, e.g. "app", "gallery" — becomes the /ext/&lt;name&gt; route prefix</summary>
    string Name { get; }

    /// <summary>Register routes, filters, tools and providers (port of __install__(ctx))</summary>
    void Install(ExtensionContext ctx);

    /// <summary>Async post-install hook, run concurrently for all extensions (port of __load__(ctx))</summary>
    Task LoadAsync(ExtensionContext ctx, CancellationToken token = default) => Task.CompletedTask;
}
