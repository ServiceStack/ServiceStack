using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using ServiceStack.Web;

namespace ServiceStack.NetSerializer
{
    public class NetSerializerFormat : IPlugin, INetSerializerPlugin
    {
        public void Register(IAppHost appHost)
        {
            appHost.ContentTypes.Register(MimeTypes.NetSerializer,
                Serialize,
                Deserialize);

            var allTypes = ((ServiceStackHost) appHost).Metadata.GetAllOperationTypes();
            var serializableTypes = allTypes.Where(x => x.HasAttribute<SerializableAttribute>()).ToArray();
            global::NetSerializer.Serializer.Initialize(serializableTypes);
        }

        public static void Serialize(IRequest requestContext, object dto, Stream outputStream)
        {
            Serialize(dto, outputStream);
        }

        public static void Serialize(object dto, Stream outputStream)
        {
            if (dto == null) return;
            var dtoType = dto.GetType();
            try
            {
                global::NetSerializer.Serializer.Serialize(outputStream, dto);
            }
            catch (Exception ex)
            {
                HandleException(ex, dtoType);
            }
        }

        public static object Deserialize(Type type, Stream fromStream)
        {
            try
            {
                var obj = global::NetSerializer.Serializer.Deserialize(fromStream);
                return obj;
            }
            catch (Exception ex)
            {
                return HandleException(ex, type);
            }
        }

        public static object HandleException(Exception ex, Type type)
        {
            if (ex is SerializationException
                && ex.Message.Contains("does not have any serializable fields nor properties"))
                return type.CreateInstance();

            throw ex;
        }
    }
}
