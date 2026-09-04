using System;
using System.IO;
using System.Runtime.Serialization;
using ServiceStack.Web;

namespace ServiceStack.ProtoBuf
{
    public class ProtoBufServiceClient : ServiceClientBase
    {
        public override string Format => "x-protobuf";

        public ProtoBufServiceClient(string baseUri)
        {
            SetBaseUri(baseUri);
        }

        public ProtoBufServiceClient(string syncReplyBaseUri, string asyncOneWayBaseUri)
            : base(syncReplyBaseUri, asyncOneWayBaseUri) { }

        public override void SerializeToStream(IRequest req, object request, Stream stream)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));
            if (request == null) return;
            try
            {
                ProtoBufFormat.Serialize(req, request, stream);
            }
            catch (Exception ex)
            {
                throw new SerializationException("ProtoBufServiceClient: Error serializing: " + ex.Message, ex);
            }
        }

        public override T DeserializeFromStream<T>(Stream stream)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));
            try
            {
                return ProtoBufFormat.Deserialize<T>(stream);
            }
            catch (Exception ex)
            {
                throw new SerializationException("ProtoBufServiceClient: Error deserializing: " + ex.Message, ex);
            }
        }

        public override string ContentType => MimeTypes.ProtoBuf;

        public override StreamDeserializerDelegate StreamDeserializer => Deserialize;

        private static object Deserialize(Type type, Stream source)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));
            if (source == null) throw new ArgumentNullException(nameof(source));
            try
            {
                return ProtoBufFormat.Deserialize(type, source);
            }
            catch (Exception ex)
            {
                throw new SerializationException("ProtoBufServiceClient: Error deserializing: " + ex.Message, ex);
            }
        }
    }
}