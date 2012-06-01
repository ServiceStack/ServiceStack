using System.IO;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceModel.Serialization;
using ServiceStack.Text;
using System;

namespace ServiceStack.ServiceClient.Web
{
    public class XmlServiceClient
        : ServiceClientBase
    {
        public override string Format
        {
            get { return "xml"; }
        }

        public XmlServiceClient()
        {
        }

        public XmlServiceClient(string baseUri) 
        {
            SetBaseUri(baseUri);
        }

        public XmlServiceClient(string syncReplyBaseUri, string asyncOneWayBaseUri) 
            : base(syncReplyBaseUri, asyncOneWayBaseUri) {}

        public override string ContentType
        {
            get { return String.Format("application/{0}", Format); }
        }

        public override void SerializeToStream(IRequestContext requestContext, object request, Stream stream)
        {
            DataContractSerializer.Instance.SerializeToStream(request, stream);
        }

        public override T DeserializeFromStream<T>(Stream stream)
        {
            return DataContractDeserializer.Instance.DeserializeFromStream<T>(stream);
        }

        public override StreamDeserializerDelegate StreamDeserializer
        {
            get { return XmlSerializer.DeserializeFromStream; }
        }
    }
}