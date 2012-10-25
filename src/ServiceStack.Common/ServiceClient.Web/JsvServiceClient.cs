using System;
using System.IO;
using ServiceStack.ServiceHost;
using ServiceStack.Text;

namespace ServiceStack.ServiceClient.Web
{
    public class JsvServiceClient
        : ServiceClientBase
    {
        public override string Format
        {
            get { return "jsv"; }
        }

        public JsvServiceClient()
        {
        }

        public JsvServiceClient(string baseUri) 
        {
            SetBaseUri(baseUri);
        }

        public JsvServiceClient(string syncReplyBaseUri, string asyncOneWayBaseUri) 
            : base(syncReplyBaseUri, asyncOneWayBaseUri)
        {
        }

        public override string ContentType
        {
            get { return String.Format("application/{0}", Format); }
        }

        public override void SerializeToStream(IRequestContext requestContext, object request, Stream stream)
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

        public override StreamDeserializerDelegate StreamDeserializer
        {
            get { return TypeSerializer.DeserializeFromStream; }
        }
    }
}