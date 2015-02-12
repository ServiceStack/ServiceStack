using System;
using System.IO;
using ServiceStack.Serialization;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack
{
    public class JsonServiceClient
        : ServiceClientBase
    {
        public override string Format
        {
            get { return "json"; }
        }

        public JsonServiceClient()
        {
        }

        public JsonServiceClient(string baseUri) 
        {
            SetBaseUri(baseUri);
        }

        public JsonServiceClient(string syncReplyBaseUri, string asyncOneWayBaseUri) 
            : base(syncReplyBaseUri, asyncOneWayBaseUri)
        {
        }

        public override string ContentType
        {
            get { return String.Format("application/{0}", Format); }
        }

        public override void SerializeToStream(IRequest requestContext, object request, Stream stream)
        {
            JsonDataContractSerializer.Instance.SerializeToStream(request, stream);
        }

        public override T DeserializeFromStream<T>(Stream stream)
        {
            return JsonDataContractSerializer.Instance.DeserializeFromStream<T>(stream);
        }

        public override StreamDeserializerDelegate StreamDeserializer
        {
            get { return JsonSerializer.DeserializeFromStream; }
        }

        internal static JsonObject ParseObject(string json)
        {
            using (__requestAccess())
            {
                return JsonObject.Parse(json);
            }
        }
    }
}