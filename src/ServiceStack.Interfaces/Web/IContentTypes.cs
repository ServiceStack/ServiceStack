using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace ServiceStack.Web
{
    public interface IContentTypes
        : IContentTypeWriter, IContentTypeReader
    {
        string GetFormatContentType(string format);

        Dictionary<string, string> ContentTypeFormats { get; }

        void Register(string contentType,
            StreamSerializerDelegate streamSerializer, StreamDeserializerDelegate streamDeserializer);

        void RegisterAsync(string contentType,
            StreamSerializerDelegateAsync responseSerializer, StreamDeserializerDelegateAsync streamDeserializer);

        void Remove(string contentType);
    }

    public interface IContentTypeReader
    {
        object DeserializeFromString(string contentType, Type type, string request);

        object DeserializeFromStream(string contentType, Type type, Stream requestStream);

        StreamDeserializerDelegate GetStreamDeserializer(string contentType);

        StreamDeserializerDelegateAsync GetStreamDeserializerAsync(string contentType);
    }
    
    public interface IContentTypeWriter
    {
        byte[] SerializeToBytes(IRequest req, object response);

        string SerializeToString(IRequest req, object response);

        Task SerializeToStreamAsync(IRequest requestContext, object response, Stream toStream);

        StreamSerializerDelegateAsync GetStreamSerializerAsync(string contentType);
    }

    public delegate string TextSerializerDelegate(object dto);

    public delegate void StreamSerializerDelegate(IRequest req, object dto, Stream outputStream);

    public delegate Task StreamSerializerDelegateAsync(IRequest req, object dto, Stream outputStream);

    public delegate object TextDeserializerDelegate(Type type, string dto);

    public delegate object StreamDeserializerDelegate(Type type, Stream fromStream);

    public delegate Task<object> StreamDeserializerDelegateAsync(Type type, Stream fromStream);

    public delegate string StringSerializerDelegate(IRequest req, object dto);
    
    public delegate object StringDeserializerDelegate(string contents, Type type);
}
