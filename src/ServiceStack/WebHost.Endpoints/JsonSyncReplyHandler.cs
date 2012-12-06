using ServiceStack.Common.Web;
using ServiceStack.ServiceHost;

namespace ServiceStack.WebHost.Endpoints
{
    public class JsonAsyncOneWayHandler : GenericHandler
    {
        public JsonAsyncOneWayHandler()
            : base(ContentType.Json, EndpointAttributes.OneWay | EndpointAttributes.Json, Feature.Json)
        {
        }
    }

    public class JsonSyncReplyHandler : GenericHandler
    {
        public JsonSyncReplyHandler()
            : base(ContentType.Json, EndpointAttributes.Reply | EndpointAttributes.Json, Feature.Json)
        {
        }
    }
}