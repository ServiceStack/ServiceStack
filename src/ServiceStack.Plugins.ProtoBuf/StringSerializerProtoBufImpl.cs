using System;
using System.IO;
using ProtoBuf;
using ServiceStack.DesignPatterns.Serialization;

namespace ServiceStack.Plugins.ProtoBuf
{
    public class StringSerializerProtoBufImpl : ITextSerializer
    {
        public object DeserializeFromString(string stringBase64, Type returnType)
        {
            byte[] byteAfter64 = Convert.FromBase64String(stringBase64);
            var afterStream = new MemoryStream(byteAfter64);
            return Serializer.NonGeneric.Deserialize(returnType, afterStream);
        }

        public T DeserializeFromString<T>(string stringBase64)
        {
            byte[] byteAfter64 = Convert.FromBase64String(stringBase64);
            var afterStream = new MemoryStream(byteAfter64);
            return Serializer.Deserialize<T>(afterStream);
        }

        public T DeserializeFromStream<T>(Stream stream)
        {
            return Serializer.Deserialize<T>(stream);
        }

        public object DeserializeFromStream(Type type, Stream stream)
        {
            return Serializer.NonGeneric.Deserialize(type, stream);
        }

        public string SerializeToString<T>(T obj)
        {
            var ms = new MemoryStream();
            Serializer.Serialize(ms, obj);
            string stringBase64 = Convert.ToBase64String(ms.ToArray());
            return stringBase64;
        }

        public void SerializeToStream<T>(T obj, Stream stream)
        {
            Serializer.Serialize<T>(stream, obj);
        }
    }
}
