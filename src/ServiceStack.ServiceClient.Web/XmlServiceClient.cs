using System.IO;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceModel.Serialization;
using ServiceStack.Text;

namespace ServiceStack.ServiceClient.Web
{
	public class XmlServiceClient
		: ServiceClientBase
	{
		public XmlServiceClient()
		{
		}

		public XmlServiceClient(string baseUri) 
		{
			SetBaseUri(baseUri, "xml");
		}

		public XmlServiceClient(string syncReplyBaseUri, string asyncOneWayBaseUri) 
			: base(syncReplyBaseUri, asyncOneWayBaseUri) {}

		public override string ContentType
		{
			get { return "application/xml"; }
		}

		public override void SerializeToStream(object request, Stream stream)
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