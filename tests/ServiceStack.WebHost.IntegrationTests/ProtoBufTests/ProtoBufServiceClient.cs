using System;
using System.IO;
using ServiceStack.Common.Web;
using ServiceStack.ServiceClient.Web;
using ServiceStack.ServiceHost;

namespace ServiceStack.WebHost.Endpoints.Tests.ProtoBufTests
{
	public class ProtoBufFormat : IPlugin
	{
		public void Register(IAppHost appHost)
		{
			appHost.ContentTypeFilters.Register(ContentType.ProtoBuf,
				(reqCtx, res, stream) => ProtoBuf.Serializer.NonGeneric.Serialize(stream, res),
				ProtoBuf.Serializer.NonGeneric.Deserialize);
		}
	}

	public class ProtoBufServiceClient : ServiceClientBase
	{
		public ProtoBufServiceClient(string baseUri)
		{
			SetBaseUri(baseUri, "x-protobuf");
		}

		public ProtoBufServiceClient(string syncReplyBaseUri, string asyncOneWayBaseUri)
			: base(syncReplyBaseUri, asyncOneWayBaseUri)
		{
		}

		public override void SerializeToStream(IRequestContext requestContext, object request, Stream stream)
		{
			ProtoBuf.Serializer.NonGeneric.Serialize(stream, request);
		}

		public override T DeserializeFromStream<T>(Stream stream)
		{
			return ProtoBuf.Serializer.Deserialize<T>(stream);
		}

		public override string ContentType
		{
			get { return Common.Web.ContentType.ProtoBuf; }
		}

		public override StreamDeserializerDelegate StreamDeserializer
		{
			get { return ProtoBuf.Serializer.NonGeneric.Deserialize; }
		}
	}
}