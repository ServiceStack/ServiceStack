using System.IO;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack
{
    public class JsvServiceClient
        : ServiceClientBase
    {
        public override string Format => "jsv";

        public JsvServiceClient() {}

        public JsvServiceClient(string baseUri) 
        {
            SetBaseUri(baseUri);
        }

        public JsvServiceClient(string syncReplyBaseUri, string asyncOneWayBaseUri) 
            : base(syncReplyBaseUri, asyncOneWayBaseUri) {}

        public override string ContentType => $"application/{Format}";

        public override void SerializeToStream(IRequest req, object request, Stream stream)
        {
            TypeSerializer.SerializeToStream(request, stream);
        }

        public override T DeserializeFromStream<T>(Stream stream)
        {
            return TypeSerializer.DeserializeFromStream<T>(stream);
        }

        public override StreamDeserializerDelegate StreamDeserializer => TypeSerializer.DeserializeFromStream;
    }
}