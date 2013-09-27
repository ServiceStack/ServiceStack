using ServiceStack.Text;

namespace ServiceStack.WebHost.Handlers
{
    public class JsonAsyncOneWayHandler : GenericHandler
    {
        public JsonAsyncOneWayHandler()
            : base(MimeTypes.Json, EndpointAttributes.OneWay | EndpointAttributes.Json, Feature.Json)
        {
        }
    }

    public class JsonSyncReplyHandler : GenericHandler
    {
        public JsonSyncReplyHandler()
            : base(MimeTypes.Json, EndpointAttributes.Reply | EndpointAttributes.Json, Feature.Json)
        {
        }
    }
}