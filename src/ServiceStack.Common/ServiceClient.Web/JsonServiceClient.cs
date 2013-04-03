using System;
using System.IO;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceModel.Serialization;
using ServiceStack.Text;

namespace ServiceStack.ServiceClient.Web
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

        public override void SerializeToStream(IRequestContext requestContext, object request, Stream stream)
        {
            JsonDataContractSerializer.Instance.SerializeToStream(request, stream);
        }

        public override T DeserializeFromStream<T>(Stream stream)
        {
            return JsonDataContractDeserializer.Instance.DeserializeFromStream<T>(stream);
        }

        public override StreamDeserializerDelegate StreamDeserializer
        {
            get { return JsonSerializer.DeserializeFromStream; }
        }
    }
}