using System.IO;

namespace ServiceStack.Web
{
    public interface IContentTypeWriter
    {
        byte[] SerializeToBytes(IRequest req, object response);

        string SerializeToString(IRequest req, object response);

        void SerializeToStream(IRequest requestContext, object response, Stream toStream);

        void SerializeToResponse(IRequest requestContext, object response, IResponse httpRes);

        ResponseSerializerDelegate GetResponseSerializer(string contentType);
    }

    public delegate string TextSerializerDelegate(object dto);

    public delegate void StreamSerializerDelegate(IRequest requestContext, object dto, Stream outputStream);

    public delegate void ResponseSerializerDelegate(IRequest requestContext, object dto, IResponse httpRes);
}