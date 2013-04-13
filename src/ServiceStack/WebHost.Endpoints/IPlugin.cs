namespace ServiceStack.WebHost.Endpoints
{
	public interface IPlugin
	{
		void Register(IAppHost appHost);
	}

    public interface IPreInitPlugin
    {
        void Configure(IAppHost appHost);
    }

	public interface IProtoBufPlugin { } //Marker for ProtoBuf plugin
    public interface IMsgPackPlugin { }  //Marker for MsgPack plugin
    public interface IRazor2Plugin { }    //Marker for MVC Razor plugin
}