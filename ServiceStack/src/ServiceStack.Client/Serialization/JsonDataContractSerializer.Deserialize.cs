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

            if (!UseBcl)
                return JsonSerializer.DeserializeFromString(json, returnType);

            return BclDeserializeFromString(json, returnType);
        }

        public static object BclDeserializeFromString(string json, Type returnType)
        {
            if (json == null) return null;
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
            if (stream == null || stream == Stream.Null)
                return default;

            if (TextSerializer is IStringStreamSerializer streamSerializer)
                return streamSerializer.DeserializeFromStream<T>(stream);

            if (TextSerializer != null)
            {
                var json = stream.ReadToEnd();
                return json == null ? default : TextSerializer.DeserializeFromString<T>(json);
            }

            if (UseBcl)
                return (T)BclDeserializeFromStream(typeof(T), stream);

            return JsonSerializer.DeserializeFromStream<T>(stream);
        }

        public object DeserializeFromStream(Type type, Stream stream)
        {
            if (type == null || stream == null || stream == Stream.Null)
                return null;

            if (TextSerializer is IStringStreamSerializer streamSerializer)
                return streamSerializer.DeserializeFromStream(type, stream);

            if (TextSerializer != null)
            {
                var json = stream.ReadToEnd();
                return json == null ? null : TextSerializer.DeserializeFromString(json, type);
            }

            if (UseBcl)
                return BclDeserializeFromStream(type, stream);

            return JsonSerializer.DeserializeFromStream(type, stream);
        }

        public static object BclDeserializeFromStream(Type type, Stream stream)
        {
            if (type == null || stream == null || stream == Stream.Null)
                return null;

            var serializer = new System.Runtime.Serialization.Json.DataContractJsonSerializer(type);
            return serializer.ReadObject(stream);
        }
    }
}
