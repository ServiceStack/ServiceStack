using ServiceStack.Common.Web;
using ServiceStack.ServiceHost;

namespace ServiceStack.WebHost.Endpoints
{
	public class JsonAsyncOneWayHandler : GenericHandler
	{
		public JsonAsyncOneWayHandler()
			: base(ContentType.Json, EndpointAttributes.AsyncOneWay | EndpointAttributes.Json, Feature.Json)
		{
		}
	}

	public class JsonSyncReplyHandler : GenericHandler
	{
		public JsonSyncReplyHandler()
			: base(ContentType.Json, EndpointAttributes.SyncReply | EndpointAttributes.Json, Feature.Json)
		{
		}
	}
}