namespace ServiceStack.Wire
{
    using System;
    using System.IO;
    using ServiceStack.Web;

    public class WireServiceClient : ServiceClientBase
    {
        public override string Format => "x-wire";

        public WireServiceClient(string baseUri)
        {
            SetBaseUri(baseUri);
        }

        public WireServiceClient(string syncReplyBaseUri, string asyncOneWayBaseUri)
            : base(syncReplyBaseUri, asyncOneWayBaseUri) { }

        public override void SerializeToStream(IRequest requestContext, object request, Stream stream)
        {
            if (request == null) return;
            try
            {
                WireFormat.Serialize(requestContext, request, stream);
            }
            catch (Exception ex)
            {
                WireFormat.HandleException(ex, request.GetType());
            }
        }

        public override T DeserializeFromStream<T>(Stream stream)
        {
            try
            {
                return (T)WireFormat.Deserialize(typeof(T), stream);

            }
            catch (Exception ex)
            {
                return (T)WireFormat.HandleException(ex, typeof(T));
            }
        }

        public override string ContentType => MimeTypes.Wire;

        public override StreamDeserializerDelegate StreamDeserializer => WireFormat.Deserialize;
    }
}