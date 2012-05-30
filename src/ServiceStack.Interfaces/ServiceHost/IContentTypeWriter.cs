using System.IO;

namespace ServiceStack.ServiceHost
{
    public interface IContentTypeWriter
    {
        string SerializeToString(IRequestContext requestContext, object response);

        void SerializeToStream(IRequestContext requestContext, object response, Stream toStream);

        void SerializeToResponse(IRequestContext requestContext, object response, IHttpResponse httpRes);

        ResponseSerializerDelegate GetResponseSerializer(string contentType);
    }

    public delegate string TextSerializerDelegate(object dto);

    public delegate void StreamSerializerDelegate(IRequestContext requestContext, object dto, Stream outputStream);

    public delegate void ResponseSerializerDelegate(IRequestContext requestContext, object dto, IHttpResponse httpRes);
}