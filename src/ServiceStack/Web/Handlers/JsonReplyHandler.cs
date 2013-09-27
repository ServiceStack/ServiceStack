using ServiceStack.Text;

namespace ServiceStack.Web.Handlers
{
    public class JsonOneWayHandler : GenericHandler
    {
        public JsonOneWayHandler()
            : base(MimeTypes.Json, EndpointAttributes.OneWay | EndpointAttributes.Json, Feature.Json)
        {
        }
    }

    public class JsonReplyHandler : GenericHandler
    {
        public JsonReplyHandler()
            : base(MimeTypes.Json, EndpointAttributes.Reply | EndpointAttributes.Json, Feature.Json)
        {
        }
    }
}