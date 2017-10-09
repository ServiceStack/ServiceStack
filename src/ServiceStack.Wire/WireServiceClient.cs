namespace ServiceStack.Wire
{
    using global::Wire;
    using System;
    using System.IO;
    using System.Runtime.Serialization;
    using ServiceStack.Web;
    using ServiceStack.Text;

    public class WireServiceClient : ServiceClientBase
    {
        public override string Format => "x-wire";

        public WireServiceClient(string baseUri)
        {
            SetBaseUri(baseUri);
        }

        public WireServiceClient(string syncReplyBaseUri, string asyncOneWayBaseUri)
            : base(syncReplyBaseUri, asyncOneWayBaseUri) { }

        public override void SerializeToStream(IRequest requestContext, object request, Stream stream)
        {
            if (request == null) return;
            try
            {
                WireFormat.Serialize(requestContext, request, stream);
            }
            catch (Exception ex)
            {
                WireFormat.HandleException(ex, request.GetType());
            }
        }

        public override T DeserializeFromStream<T>(Stream stream)
        {
            try
            {
                return (T)WireFormat.Deserialize(typeof(T), stream);

            }
            catch (Exception ex)
            {
                return (T)WireFormat.HandleException(ex, typeof(T));
            }
        }

        public override string ContentType => MimeTypes.Wire;

        public override StreamDeserializerDelegate StreamDeserializer => WireFormat.Deserialize;
    }
    
    public class WireFormat : IPlugin, IWirePlugin
    {
        public static Serializer WireSerializer = new Serializer(new SerializerOptions(
            versionTolerance:true, 
            preserveObjectReferences:false, 
            surrogates:null, 
            serializerFactories:null, 
            knownTypes:null));

        public void Register(IAppHost appHost)
        {
            appHost.ContentTypes.Register(MimeTypes.Wire,
                Serialize,
                Deserialize);
        }

        public static void Serialize(IRequest requestContext, object dto, Stream outputStream)
        {
            Serialize(dto, outputStream);
        }

        public static void Serialize(object dto, Stream outputStream)
        {
            if (dto == null) return;
            try
            {
                WireSerializer.Serialize(dto, outputStream);
            }
            catch (Exception ex)
            {
                HandleException(ex, dto.GetType());
            }
        }

        public static object Deserialize(Type type, Stream fromStream)
        {
            try
            {
                return WireSerializer.Deserialize(fromStream);
            }
            catch (Exception ex)
            {
                return HandleException(ex, type);
            }
        }

        /// <summary>
        /// MsgPack throws an exception for empty DTO's - normalizing the behavior to 
        /// follow other types and return an empty instance.
        /// </summary>
        /// <param name="ex"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public static object HandleException(Exception ex, Type type)
        {
            if (ex is SerializationException
                && ex.Message.Contains("does not have any serializable fields nor properties"))
                return type.CreateInstance();

            throw ex;
        }
    }
    
    public static class WireExtensions
    {
        private static readonly Serializer serializer = new Serializer();

        public static byte[] ToWire<T>(this T obj)
        {
            using (var ms = MemoryStreamFactory.GetStream())
            {
                serializer.Serialize(obj, ms);
                var bytes = ms.ToArray();
                return bytes;
            }
        }

        public static T FromWire<T>(this byte[] bytes)
        {
            using (var ms = MemoryStreamFactory.GetStream(bytes))
            {
                return serializer.Deserialize<T>(ms);
            }
        }
    }

}