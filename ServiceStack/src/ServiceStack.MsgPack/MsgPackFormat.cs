using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.ExceptionServices;
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
        private static readonly bool isGenericCollection;
        private static readonly Func<object, Type, object> collectionConvertFn;

        static MsgPackType()
        {
            var genericType = typeof(T).FirstGenericType();

            isGenericCollection = genericType != null
                && typeof(T).IsOrHasGenericInterfaceTypeOf(typeof(ICollection<>));

            if (isGenericCollection)
            {
                var elType = genericType.GetGenericArguments()[0];
                var genericMi = typeof(CollectionExtensions).GetStaticMethod("Convert");
                if (genericMi != null)
                {
                    var mi = genericMi.MakeGenericMethod(elType);
                    collectionConvertFn = (Func<object, Type, object>)
                        mi.CreateDelegate(typeof(Func<object, Type, object>));
                }
            }

            type = isGenericCollection ? genericType : typeof(T);
        }

        public Type Type => type;

        public object Convert(object instance)
        {
            if (!isGenericCollection || instance == null || collectionConvertFn == null)
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

    public class MsgPackFormat : IPlugin, IMsgPackPlugin, Model.IHasStringId
    {
        public string Id { get; set; } = Plugins.MsgPack;
        public static SerializationContext Context { get; set; } = SerializationContext.Default;

        public void Register(IAppHost appHost)
        {
            appHost.ContentTypes.Register(MimeTypes.MsgPack,
                Serialize,
                Deserialize);
        }

        private static Dictionary<Type, IMsgPackType> msgPackTypeCache = new Dictionary<Type, IMsgPackType>();

        internal static IMsgPackType GetMsgPackType(Type type)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));

            if (msgPackTypeCache.TryGetValue(type, out var msgPackType))
                return msgPackType;

            var genericType = typeof(MsgPackType<>).MakeGenericType(type);
            msgPackType = (IMsgPackType)genericType.CreateInstance();

            Dictionary<Type, IMsgPackType> snapshot, newCache;
            do
            {
                snapshot = msgPackTypeCache;
                newCache = new Dictionary<Type, IMsgPackType>(snapshot) { [type] = msgPackType };

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
            if (dto == null || outputStream == null) return;
            var dtoType = dto.GetType();
            try
            {
                var msgPackType = GetMsgPackType(dtoType);
                dtoType = msgPackType.Type;

                var serializer = MessagePackSerializer.Get(dtoType, Context ?? SerializationContext.Default);
                using var packer = Packer.Create(outputStream, ownsStream: false);
                serializer.PackTo(packer, dto);
            }
            catch (Exception ex)
            {
                HandleException(ex, dtoType);
            }
        }

        public static T Deserialize<T>(Stream fromStream) => (T)Deserialize(typeof(T), fromStream);

        public static object Deserialize(Type type, Stream fromStream)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));
            if (fromStream == null) return null;

            try
            {
                var msgPackType = GetMsgPackType(type);
                type = msgPackType.Type;

                var serializer = MessagePackSerializer.Get(type, Context ?? SerializationContext.Default);
                using var unpacker = Unpacker.Create(fromStream, ownsStream: false);
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
            if (ex == null) throw new ArgumentNullException(nameof(ex));
            if (ex is SerializationException
                && (ex.Message?.Contains("does not have any serializable fields nor properties") == true
                    || ex.InnerException?.Message?.Contains("does not have any serializable fields nor properties") == true))
            {
                return type.CreateInstance();
            }

            ExceptionDispatchInfo.Capture(ex).Throw();
            return null;
        }
    }
}
