using System;
using MsgPack.Serialization;
using System.IO;
using ServiceStack.Web;

namespace ServiceStack.MsgPack
{
    public class MsgPackServiceClient : ServiceClientBase
    {
        public override string Format => "x-msgpack";

        public MsgPackServiceClient(string baseUri)
        {
            SetBaseUri(baseUri);
        }

        public MsgPackServiceClient(string syncReplyBaseUri, string asyncOneWayBaseUri)
            : base(syncReplyBaseUri, asyncOneWayBaseUri) { }

        public override void SerializeToStream(IRequest req, object request, Stream stream)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));
            if (request == null) return;
            try
            {
                MsgPackFormat.Serialize(req, request, stream);
            }
            catch (Exception ex)
            {
                MsgPackFormat.HandleException(ex, request.GetType());
            }
        }

        public override T DeserializeFromStream<T>(Stream stream)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));
            try
            {
                return MsgPackFormat.Deserialize<T>(stream);
            }
            catch (Exception ex)
            {
                return (T)MsgPackFormat.HandleException(ex, typeof(T));
            }
        }

        public override string ContentType => MimeTypes.MsgPack;

        public override StreamDeserializerDelegate StreamDeserializer => MsgPackFormat.Deserialize;
    }
}