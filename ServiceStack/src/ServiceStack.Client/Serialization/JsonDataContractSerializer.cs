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
                return TextSerializer.SerializeToString(obj);

            if (!UseBcl)
                return JsonSerializer.SerializeToString(obj);

            return BclSerializeToString(obj);
        }

        public static string BclSerializeToString<T>(T obj)
        {
            if (obj == null) 
                return null;

            var type = obj.GetType();
            try
            {
                using (var ms = MemoryStreamFactory.GetStream())
                {
                    var serializer = new System.Runtime.Serialization.Json.DataContractJsonSerializer(type);
                    serializer.WriteObject(ms, obj);
                    return ms.ReadToEnd();
                }
            }
            catch (Exception ex)
            {
                throw new SerializationException("JsonDataContractSerializer: Error converting type: " + ex.Message, ex);
            }
        }

        public void SerializeToStream<T>(T obj, Stream stream)
        {
            if (obj == null || stream == null || stream == Stream.Null) return;

            if (TextSerializer is IStringStreamSerializer streamSerializer)
            {
                streamSerializer.SerializeToStream(obj, stream);
            }
            else if (TextSerializer != null)
            {
                var str = TextSerializer.SerializeToString(obj);
                if (str != null)
                {
                    var bytes = System.Text.Encoding.UTF8.GetBytes(str);
                    stream.Write(bytes, 0, bytes.Length);
                }
            }
            else if (UseBcl)
            {
                BclSerializeToStream(obj, stream);
            }
            else
            {
                JsonSerializer.SerializeToStream(obj, stream);
            }
        }

        public static void BclSerializeToStream<T>(T obj, Stream stream)
        {
            if (obj == null || stream == null || stream == Stream.Null) return;
            var serializer = new System.Runtime.Serialization.Json.DataContractJsonSerializer(obj.GetType());
            serializer.WriteObject(stream, obj);
        }
    }
}
