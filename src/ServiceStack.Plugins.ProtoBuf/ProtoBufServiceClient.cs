using ProtoBuf;
using System.IO;
using ServiceStack.ServiceClient.Web;
using ServiceStack.ServiceHost;

namespace ServiceStack.Plugins.ProtoBuf
{
	public class ProtoBufServiceClient : ServiceClientBase
	{
        public override string Format
        {
            get { return "x-protobuf"; }
        }

		public ProtoBufServiceClient(string baseUri)
		{
			SetBaseUri(baseUri);
		}

		public ProtoBufServiceClient(string syncReplyBaseUri, string asyncOneWayBaseUri)
			: base(syncReplyBaseUri, asyncOneWayBaseUri) {}

		public override void SerializeToStream(IRequestContext requestContext, object request, Stream stream)
		{
			Serializer.NonGeneric.Serialize(stream, request);
		}

		public override T DeserializeFromStream<T>(Stream stream)
		{
			return Serializer.Deserialize<T>(stream);
		}

		public override string ContentType
		{
			get { return Common.Web.ContentType.ProtoBuf; }
		}

		public override StreamDeserializerDelegate StreamDeserializer
		{
			get { return Serializer.NonGeneric.Deserialize; }
		}
	}
}