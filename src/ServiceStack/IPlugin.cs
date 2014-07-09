namespace ServiceStack
{
    public interface IPlugin
    {
        void Register(IAppHost appHost);
    }

    public interface IPreInitPlugin
    {
        void Configure(IAppHost appHost);
    }

    public interface IPostInitPlugin
    {
        void AfterPluginsLoaded(IAppHost appHost);
    }

    public interface IProtoBufPlugin { }        //Marker for ProtoBuf plugin
    public interface IMsgPackPlugin { }         //Marker for MsgPack plugin
    public interface INetSerializerPlugin { }   //Marker for NetSerialize plugin
    public interface IRazorPlugin { }           //Marker for MVC Razor plugin
}