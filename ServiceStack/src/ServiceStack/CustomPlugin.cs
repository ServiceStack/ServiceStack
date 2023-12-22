using System;

namespace ServiceStack;

/// <summary>
/// Allow custom AppHost registrations to run at different plugin lifecycle events
/// </summary>
public class CustomPlugin : IPlugin, Model.IHasStringId, IPreInitPlugin, IPostInitPlugin
{
    public string Id { get; set; } = "Custom";

    public Action<IAppHost> OnRegister { get; set; }
    public Action<IAppHost> OnBeforePluginsLoaded { get; set; }
    public Action<IAppHost> OnAfterPluginsLoaded { get; set; }

    public CustomPlugin() { }
    public CustomPlugin(Action<IAppHost> onRegister) : this("Custom", onRegister) {}
    public CustomPlugin(string id, Action<IAppHost> onRegister)
    {
        this.OnRegister = onRegister;
        Id = id;
    }

    public void Register(IAppHost appHost) => OnRegister?.Invoke(appHost);
    public void BeforePluginsLoaded(IAppHost appHost) => OnBeforePluginsLoaded?.Invoke(appHost);
    public void AfterPluginsLoaded(IAppHost appHost) => OnAfterPluginsLoaded?.Invoke(appHost);
}