using System;
using System.IO;
using ServiceStack.Common.Web;
using ServiceStack.ServiceClient.Web;
using ServiceStack.ServiceHost;

namespace ServiceStack.WebHost.Endpoints.Tests.ProtoBufTests
{
    public static class ProtoBufFormat
    {
        public static void Register(IAppHost appHost)
        {
            appHost.ContentTypeFilters.Register(ContentType.ProtoBuf, SerializeProtoBuf, DeserializeProtoBuf);
        }

        //AppHost
        
        public static void SerializeProtoBuf(IRequestContext requestContext, object response, Stream stream)
        {
            ProtoBuf.Serializer.NonGeneric.Serialize(stream, response);
        }

        public static object DeserializeProtoBuf(Type type, Stream stream)
        {
            return ProtoBuf.Serializer.NonGeneric.Deserialize(type, stream);
        }

        //ServiceClient

        public static void SerializeToStream(IRequestContext requestContext, object request, Stream stream)
        {
            ProtoBuf.Serializer.NonGeneric.Serialize(stream, request);
        }

        public static T DeserializeFromStream<T>(Stream stream)
        {
            return ProtoBuf.Serializer.Deserialize<T>(stream);
        }

        public static StreamDeserializerDelegate StreamDeserializer
        {
            get { return ProtoBuf.Serializer.NonGeneric.Deserialize; }
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
            ProtoBufFormat.SerializeToStream(requestContext, request, stream);
        }

        public override T DeserializeFromStream<T>(Stream stream)
        {
            return ProtoBufFormat.DeserializeFromStream<T>(stream);
        }

        public override string ContentType
        {
            get { return ServiceStack.Common.Web.ContentType.ProtoBuf; }
        }

        public override StreamDeserializerDelegate StreamDeserializer
        {
            get { return ProtoBufFormat.StreamDeserializer; }
        }
    }
}