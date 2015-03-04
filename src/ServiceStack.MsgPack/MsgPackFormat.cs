using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading;
using MsgPack;
using MsgPack.Serialization;
using ServiceStack.Web;

namespace ServiceStack.MsgPack
{
    public class MsgPackType<T> : IMsgPackType
    {
        private static readonly Type type;
        private static bool isGenericCollection;
        private static Func<object, Type, object> collectionConvertFn;

        static MsgPackType()
        {
            var genericType = typeof(T).FirstGenericType();

            isGenericCollection = genericType != null
                && typeof(T).IsOrHasGenericInterfaceTypeOf(typeof(ICollection<>));

            if (isGenericCollection)
            {
                var elType = genericType.GetGenericArguments()[0];
                var methods = typeof(CollectionExtensions).GetMethods();
                var genericMi = methods.FirstOrDefault(x => x.Name == "Convert");
                var mi = genericMi.MakeGenericMethod(elType);
                collectionConvertFn = (Func<object, Type, object>)
                    Delegate.CreateDelegate(typeof(Func<object, Type, object>), mi);
            }

            type = isGenericCollection ? genericType : typeof(T);
        }

        public Type Type
        {
            get
            {
                return type;
            }
        }

        public object Convert(object instance)
        {
            if (!isGenericCollection)
                return instance;

            var ret = collectionConvertFn(instance, typeof(T));

            return ret;
        }
    }

    internal interface IMsgPackType
    {
        Type Type { get; }
        object Convert(object instance);
    }

    public class MsgPackFormat : IPlugin, IMsgPackPlugin
    {
        public void Register(IAppHost appHost)
        {
            appHost.ContentTypes.Register(MimeTypes.MsgPack,
                Serialize,
                Deserialize);
        }

        private static Dictionary<Type, IMsgPackType> msgPackTypeCache = new Dictionary<Type, IMsgPackType>();

        internal static IMsgPackType GetMsgPackType(Type type)
        {
            IMsgPackType msgPackType;
            if (msgPackTypeCache.TryGetValue(type, out msgPackType))
                return msgPackType;

            var genericType = typeof(MsgPackType<>).MakeGenericType(type);
            msgPackType = (IMsgPackType)genericType.CreateInstance();

            Dictionary<Type, IMsgPackType> snapshot, newCache;
            do
            {
                snapshot = msgPackTypeCache;
                newCache = new Dictionary<Type, IMsgPackType>(msgPackTypeCache);
                newCache[type] = msgPackType;

            } while (!ReferenceEquals(
                Interlocked.CompareExchange(ref msgPackTypeCache, newCache, snapshot), snapshot));

            return msgPackType;
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
                var msgPackType = GetMsgPackType(dtoType);
                dtoType = msgPackType.Type;

                var serializer = MessagePackSerializer.Get(dtoType);
                serializer.PackTo(Packer.Create(outputStream), dto);
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
                var msgPackType = GetMsgPackType(type);
                type = msgPackType.Type;

                var serializer = MessagePackSerializer.Get(type);
                var unpacker = Unpacker.Create(fromStream);
                unpacker.Read();
                var obj = serializer.UnpackFrom(unpacker);

                obj = msgPackType.Convert(obj);

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
