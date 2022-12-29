namespace ServiceStack.Host.Handlers
{
    public class JsonOneWayHandler : GenericHandler
    {
        public JsonOneWayHandler()
            : base(MimeTypes.Json, RequestAttributes.OneWay | RequestAttributes.Json, Feature.Json)
        {
        }
    }

    public class JsonReplyHandler : GenericHandler
    {
        public JsonReplyHandler()
            : base(MimeTypes.Json, RequestAttributes.Reply | RequestAttributes.Json, Feature.Json)
        {
        }
    }
}