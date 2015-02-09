using System;
using System.IO;
using ServiceStack.Web;

namespace ServiceStack.NetSerializer
{
    public class NetSerializerServiceClient : ServiceClientBase
    {
        public override string Format
        {
            get { return "x-netserializer"; }
        }

        public NetSerializerServiceClient(string baseUri)
        {
            SetBaseUri(baseUri);
        }

        public NetSerializerServiceClient(string syncReplyBaseUri, string asyncOneWayBaseUri)
            : base(syncReplyBaseUri, asyncOneWayBaseUri) { }

        public override void SerializeToStream(IRequest requestContext, object request, Stream stream)
        {
            if (request == null) return;
            try
            {
                NetSerializerFormat.Serialize(requestContext, request, stream);
            }
            catch (Exception ex)
            {
                NetSerializerFormat.HandleException(ex, request.GetType());
            }
        }

        public override T DeserializeFromStream<T>(Stream stream)
        {
            try
            {
                var obj = (T)NetSerializerFormat.Deserialize(typeof(T), stream);
                return obj;

            }
            catch (Exception ex)
            {
                return (T)NetSerializerFormat.HandleException(ex, typeof(T));
            }
        }

        public override string ContentType
        {
            get { return MimeTypes.NetSerializer; }
        }

        public override StreamDeserializerDelegate StreamDeserializer
        {
            get { return NetSerializerFormat.Deserialize; }
        }
    }
}