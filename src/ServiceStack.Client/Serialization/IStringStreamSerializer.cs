using System;
using System.IO;

namespace ServiceStack.Serialization
{
    public interface IStringStreamSerializer
    {
        T DeserializeFromStream<T>(Stream stream);
        object DeserializeFromStream(Type type, Stream stream);

        void SerializeToStream<T>(T obj, Stream stream);
    }
}