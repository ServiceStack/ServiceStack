namespace ServiceStack
{
    /// <summary>
    /// Callback for Plugins to register necessary handlers with ServiceStack
    /// </summary>
    public interface IPlugin
    {
        void Register(IAppHost appHost);
    }

    /// <summary>
    /// Callback to pre-configure any logic before IPlugin.Register() is fired
    /// </summary>
    public interface IPreInitPlugin
    {
        void Configure(IAppHost appHost);
    }

    /// <summary>
    /// Callback to post-configure any logic after IPlugin.Register() is fired
    /// </summary>
    public interface IPostInitPlugin
    {
        void AfterPluginsLoaded(IAppHost appHost);
    }

    /// <summary>
    /// Callback for AuthProviders to register callbacks with AuthFeature
    /// </summary>
    public interface IAuthPlugin
    {
        void Register(IAppHost appHost, AuthFeature feature);
    }

    public interface IProtoBufPlugin { }        //Marker for ProtoBuf plugin
    public interface IMsgPackPlugin { }         //Marker for MsgPack plugin
    public interface IWirePlugin { }            //Marker for Wire plugin
    public interface INetSerializerPlugin { }   //Marker for NetSerialize plugin
    public interface IRazorPlugin { }           //Marker for MVC Razor plugin
}