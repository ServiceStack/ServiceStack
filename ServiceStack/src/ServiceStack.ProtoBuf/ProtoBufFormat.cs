using System;
using System.IO;
using ProtoBuf.Meta;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack.ProtoBuf
{
    public class ProtoBufFormat : IPlugin, IProtoBufPlugin, Model.IHasStringId
    {
        public string Id { get; set; } = Plugins.ProtoBuf;

        public void Register(IAppHost appHost)
        {
            appHost.ContentTypes.Register(MimeTypes.ProtoBuf, Serialize, Deserialize);
        }

        private static RuntimeTypeModel model;
        private static readonly object modelLock = new();
        public static RuntimeTypeModel Model
        {
            get
            {
                if (model == null)
                {
                    lock (modelLock)
                    {
                        model ??= RuntimeTypeModel.Create();
                    }
                }
                return model;
            }
            set
            {
                lock (modelLock)
                {
                    model = value;
                }
            }
        }

        public static void Serialize(IRequest requestContext, object dto, Stream outputStream)
        {
            Serialize(dto, outputStream);
        }

        public static void Serialize(object dto, Stream outputStream)
        {
            if (dto == null || outputStream == null) return;
            Model.Serialize(outputStream, dto);
        }

        public static T Deserialize<T>(Stream fromStream) => (T) Deserialize(typeof(T), fromStream);

        public static object Deserialize(Type type, Stream fromStream)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));
            if (fromStream == null) return null;
            var obj = Model.Deserialize(fromStream, null, type);
            return obj;
        }

        public string GetProto(Type type)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));
            return Model.GetSchema(type);
        }
    }
}