using System;
using System.IO;
using System.Runtime.Serialization;
using MsgPack;
using MsgPack.Serialization;
using ServiceStack.Common.Web;
using ServiceStack.ServiceHost;
using ServiceStack.Text;
using ServiceStack.WebHost.Endpoints;

namespace ServiceStack.Plugins.MsgPack
{
    public class MsgPackFormat : IPlugin, IMsgPackPlugin
	{
		public void Register(IAppHost appHost)
		{
            appHost.ContentTypeFilters.Register(ContentType.MsgPack,
                Serialize,
                Deserialize);
		}

        public static void Serialize(IRequestContext requestContext, object dto, Stream outputStream)
        {
            if (dto == null) return;
            try
            {
                var serializer = MessagePackSerializer.Create(dto.GetType());
                serializer.PackTo(Packer.Create(outputStream), dto);
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
                var serializer = MessagePackSerializer.Create(type);
                var unpacker = Unpacker.Create(fromStream);
                unpacker.Read();
                var obj = serializer.UnpackFrom(unpacker);
                return obj;
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
}
