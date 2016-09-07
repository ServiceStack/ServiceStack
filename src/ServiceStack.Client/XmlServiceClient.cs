#if !LITE
using System.IO;
using System.Xml;
using ServiceStack.Serialization;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack
{
    public class XmlServiceClient
        : ServiceClientBase
    {
        public override string Format => "xml";

        public XmlServiceClient() {}

        public XmlServiceClient(string baseUri) 
        {
            SetBaseUri(baseUri);
        }

        public XmlServiceClient(string syncReplyBaseUri, string asyncOneWayBaseUri) 
            : base(syncReplyBaseUri, asyncOneWayBaseUri) {}

        public override string ContentType => $"application/{Format}";

        public override void SerializeToStream(IRequest requestContext, object request, Stream stream)
        {
            if (request == null) return;
            DataContractSerializer.Instance.SerializeToStream(request, stream);
        }

        public override T DeserializeFromStream<T>(Stream stream)
        {
            try
            {
                return DataContractSerializer.Instance.DeserializeFromStream<T>(stream);
            }
            catch (XmlException ex)
            {
                if (ex.Message == "Unexpected end of file.") //Empty responses
                    return default(T);

                throw;
            }
        }

        public override StreamDeserializerDelegate StreamDeserializer => XmlSerializer.DeserializeFromStream;
    }
}
#endif