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

        public override void SerializeToStream(IRequest requestContext, object request, Stream stream)
        {
            using (var writer = new StreamWriter(stream))
            {
                TypeSerializer.SerializeToWriter(request, writer);
            }
        }

        public override T DeserializeFromStream<T>(Stream stream)
        {
            using (var reader = new StreamReader(stream))
            {
                return TypeSerializer.DeserializeFromReader<T>(reader);
            }
        }

        public override StreamDeserializerDelegate StreamDeserializer => TypeSerializer.DeserializeFromStream;
    }
}