using System;
using System.IO;
using System.Runtime.Serialization;
using ServiceStack.Text;

namespace ServiceStack.Serialization
{
    public partial class JsonDataContractSerializer : IStringSerializer
    {
        public static JsonDataContractSerializer Instance = new JsonDataContractSerializer();

        public IStringSerializer TextSerializer { get; set; }

        public static void UseSerializer(IStringSerializer textSerializer)
        {
            Instance.TextSerializer = textSerializer;
        }

        public bool UseBcl { get; set; }

        public string SerializeToString<T>(T obj)
        {
            if (TextSerializer != null)
            {
                return TextSerializer.SerializeToString(obj);
            }

#if !(SL5 || __IOS__ || XBOX || ANDROID || PCL)
            if (!UseBcl)
                return JsonSerializer.SerializeToString(obj);

            if (obj == null) return null;
            var type = obj.GetType();
            try
            {
                using (var ms = MemoryStreamFactory.GetStream())
                {
                    var serializer = new System.Runtime.Serialization.Json.DataContractJsonSerializer(type);
                    serializer.WriteObject(ms, obj);
                    ms.Position = 0;
                    using (var sr = new StreamReader(ms))
                    {
                        return sr.ReadToEnd();
                    }
                }
            }
            catch (Exception ex)
            {
                throw new SerializationException("JsonDataContractSerializer: Error converting type: " + ex.Message, ex);
            }
#else
            return JsonSerializer.SerializeToString(obj);
#endif
        }

        public void SerializeToStream<T>(T obj, Stream stream)
        {
            if (TextSerializer != null)
            {
                var streamSerializer = TextSerializer as IStringStreamSerializer;
                if (streamSerializer != null)
                {
                    streamSerializer.SerializeToStream(obj, stream);
                }
            }
#if !(SL5 || __IOS__ || XBOX || ANDROID || PCL)
            else if (UseBcl)
            {
                var serializer = new System.Runtime.Serialization.Json.DataContractJsonSerializer(obj.GetType());
                serializer.WriteObject(stream, obj);
            }
#endif
            else
            {
                JsonSerializer.SerializeToStream(obj, stream);
            }
        }
    }
}
