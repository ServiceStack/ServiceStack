using System.IO;
using ServiceStack.Serialization;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack
{
    public class JsonServiceClient
        : ServiceClientBase, IJsonServiceClient
    {
        public override string Format => "json";

        public JsonServiceClient() {}

        public JsonServiceClient(string baseUri) 
        {
            SetBaseUri(baseUri);
        }

        public JsonServiceClient(string syncReplyBaseUri, string asyncOneWayBaseUri) 
            : base(syncReplyBaseUri, asyncOneWayBaseUri) {}

        public override string ContentType => $"application/{Format}";

        public override void SerializeToStream(IRequest requestContext, object request, Stream stream) => 
            JsonDataContractSerializer.Instance.SerializeToStream(request, stream);

        public override T DeserializeFromStream<T>(Stream stream) => 
            JsonDataContractSerializer.Instance.DeserializeFromStream<T>(stream);

        public override StreamDeserializerDelegate StreamDeserializer => JsonSerializer.DeserializeFromStream;

        internal static JsonObject ParseObject(string json) => 
            JsonObject.Parse(json);
    }
}