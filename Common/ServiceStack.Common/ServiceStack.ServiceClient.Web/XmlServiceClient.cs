using System.IO;
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

		/// <summary>
		/// Base Url of Service Stack's Web Service endpoints, i.e. http://localhost/Public/
		/// </summary>
		/// <param name="baseUri"></param>
		public XmlServiceClient(string baseUri) 
		{
			this.BaseUri = baseUri.WithTrailingSlash() + "Xml/";
		}

		public XmlServiceClient(string syncReplyBaseUri, string asyncOneWayBaseUri) 
			: base(syncReplyBaseUri, asyncOneWayBaseUri)
		{
		}

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
	}
}