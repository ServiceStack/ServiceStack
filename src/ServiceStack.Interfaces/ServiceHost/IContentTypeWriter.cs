using System.IO;

namespace ServiceStack.ServiceHost
{
	public interface IContentTypeWriter
	{
		string SerializeToString(IRequestContext requestContext, object response);

		void SerializeToStream(IRequestContext requestContext, object response, Stream toStream);

		StreamSerializerDelegate GetStreamSerializer(string contentType);
	}

	public delegate string TextSerializerDelegate(object dto);

	public delegate void StreamSerializerDelegate(IRequestContext requestContext, object dto, Stream toStream);

	public delegate bool StreamSerializerResolverDelegate(IRequestContext requestContext, object dto, Stream toStream);
}