using System;
using System.IO;
using System.Runtime.Serialization;
using ServiceStack.DesignPatterns.Serialization;
using ServiceStack.Text;

namespace ServiceStack.ServiceModel.Serialization
{
    public class JsonDataContractSerializer 
    {
        public static JsonDataContractSerializer Instance = new JsonDataContractSerializer();

        public ITextSerializer TextSerializer { get; set; }

        public static void UseSerializer(ITextSerializer textSerializer)
        {
            Instance.TextSerializer = textSerializer;
            JsonDataContractDeserializer.Instance.TextSerializer = textSerializer;
        }

        public bool UseBcl { get; set; }

        public string SerializeToString<T>(T obj)
        {
            if (TextSerializer != null)
                return TextSerializer.SerializeToString(obj);

#if !SILVERLIGHT && !MONOTOUCH && !XBOX && !ANDROIDINDIE
            if (!UseBcl)
                return JsonSerializer.SerializeToString(obj);

            if (obj == null) return null;
            var type = obj.GetType();
            try
            {
                using (var ms = new MemoryStream())
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
            if (obj == null) return;

            if (TextSerializer != null)
            {
                TextSerializer.SerializeToStream(obj, stream);
            }
#if !SILVERLIGHT && !MONOTOUCH && !XBOX && !ANDROIDINDIE
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
