using System;
using MsgPack.Serialization;
using System.IO;
using ServiceStack.Web;

namespace ServiceStack.MsgPack
{
    public class MsgPackServiceClient : ServiceClientBase
    {
        public override string Format
        {
            get { return "x-msgpack"; }
        }

        public MsgPackServiceClient(string baseUri)
        {
            SetBaseUri(baseUri);
        }

        public MsgPackServiceClient(string syncReplyBaseUri, string asyncOneWayBaseUri)
            : base(syncReplyBaseUri, asyncOneWayBaseUri) { }

        public override void SerializeToStream(IRequest requestContext, object request, Stream stream)
        {
            if (request == null) return;
            try
            {
                MsgPackFormat.Serialize(requestContext, request, stream);
            }
            catch (Exception ex)
            {
                MsgPackFormat.HandleException(ex, request.GetType());
            }
        }

        public override T DeserializeFromStream<T>(Stream stream)
        {
            try
            {
                var serializer = MessagePackSerializer.Get<T>();
                var obj = serializer.Unpack(stream);
                return obj;

            }
            catch (Exception ex)
            {
                return (T)MsgPackFormat.HandleException(ex, typeof(T));
            }
        }

        public override string ContentType
        {
            get { return MimeTypes.MsgPack; }
        }

        public override StreamDeserializerDelegate StreamDeserializer
        {
            get { return MsgPackFormat.Deserialize; }
        }
    }
}