using System;
using System.IO;

namespace ServiceStack.Web
{
    public interface IContentTypeReader
    {
        object DeserializeFromString(string contentType, Type type, string request);

        object DeserializeFromStream(string contentType, Type type, Stream requestStream);

        StreamDeserializerDelegate GetStreamDeserializer(string contentType);
    }

    public delegate object TextDeserializerDelegate(Type type, string dto);

    public delegate object StreamDeserializerDelegate(Type type, Stream fromStream);
}