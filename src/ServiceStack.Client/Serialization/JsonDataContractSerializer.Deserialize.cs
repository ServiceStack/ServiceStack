using System;
using System.IO;
using System.Runtime.Serialization;
using System.Text;
using ServiceStack.Text;

namespace ServiceStack.Serialization
{
    public partial class JsonDataContractSerializer 
    {
        public object DeserializeFromString(string json, Type returnType)
        {
            if (TextSerializer != null)
                return TextSerializer.DeserializeFromString(json, returnType);

#if !(SL5 || __IOS__ || XBOX || ANDROID || PCL)
            if (!UseBcl)
                return JsonSerializer.DeserializeFromString(json, returnType);

            try
            {
                using (var ms = MemoryStreamFactory.GetStream())
                {
                    var bytes = Encoding.UTF8.GetBytes(json);
                    ms.Write(bytes, 0, bytes.Length);
                    ms.Position = 0;
                    var serializer = new System.Runtime.Serialization.Json.DataContractJsonSerializer(returnType);
                    return serializer.ReadObject(ms);
                }
            }
            catch (Exception ex)
            {
                throw new SerializationException("JsonDataContractDeserializer: Error converting to type: " + ex.Message, ex);
            }
#else
            return JsonSerializer.DeserializeFromString(json, returnType);
#endif
        }

        public T DeserializeFromString<T>(string json)
        {
            if (TextSerializer != null)
                return TextSerializer.DeserializeFromString<T>(json);

            if (UseBcl)
                return (T)DeserializeFromString(json, typeof(T));

            return JsonSerializer.DeserializeFromString<T>(json);
        }

        public T DeserializeFromStream<T>(Stream stream)
        {
            if (TextSerializer != null)
            {
                var streamSerializer = TextSerializer as IStringStreamSerializer;
                if (streamSerializer != null)
                {
                    return streamSerializer.DeserializeFromStream<T>(stream);
                }
            }

#if !(SL5 || __IOS__ || XBOX || ANDROID || PCL)
            if (UseBcl)
            {
                var serializer = new System.Runtime.Serialization.Json.DataContractJsonSerializer(typeof(T));
                return (T)serializer.ReadObject(stream);				
            }
#endif
            return JsonSerializer.DeserializeFromStream<T>(stream);
        }

        public object DeserializeFromStream(Type type, Stream stream)
        {
            if (TextSerializer != null)
            {
                var streamSerializer = TextSerializer as IStringStreamSerializer;
                if (streamSerializer != null)
                {
                    return streamSerializer.DeserializeFromStream(type, stream);
                }
            }

#if !(SL5 || __IOS__ || XBOX || ANDROID || PCL)
            if (UseBcl)
            {
                var serializer = new System.Runtime.Serialization.Json.DataContractJsonSerializer(type);
                return serializer.ReadObject(stream);
            }
#endif

            return JsonSerializer.DeserializeFromStream(type, stream);
        }
    }
}
