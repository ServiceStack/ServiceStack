using ProtoBuf;
using ServiceStack.Common.Web;
using ServiceStack.WebHost.Endpoints;

namespace ServiceStack.Plugins.ProtoBuf
{
	public class ProtoBufFormat : IPlugin, IProtoBufPlugin
	{
		public void Register(IAppHost appHost)
		{
			appHost.ContentTypeFilters.Register(ContentType.ProtoBuf,
				(reqCtx, res, stream) => Serializer.NonGeneric.Serialize(stream, res),
				Serializer.NonGeneric.Deserialize);
		}
	}
}
