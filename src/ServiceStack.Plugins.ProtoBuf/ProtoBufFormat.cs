using System;
using System.IO;
using ProtoBuf;
using ProtoBuf.Meta;
using ServiceStack.Common.Web;
using ServiceStack.ServiceHost;
using ServiceStack.WebHost.Endpoints;

namespace ServiceStack.Plugins.ProtoBuf
{
	public class ProtoBufFormat : IPlugin, IProtoBufPlugin
	{
		public void Register(IAppHost appHost)
		{
		    appHost.ContentTypeFilters.Register(ContentType.ProtoBuf, Serialize, Deserialize);
		}

	    private static RuntimeTypeModel model;

        public static RuntimeTypeModel Model
        {
            get { return model ?? (model = TypeModel.Create()); }
        }

        public static void Serialize(IRequestContext requestContext, object dto, Stream outputStream)
        {
            Model.Serialize(outputStream, dto);
        }

	    public static object Deserialize(Type type, Stream fromStream)
	    {
	        var obj = Model.Deserialize(fromStream, null, type);
	        return obj;
	    }
	}
}
