using System.Collections.Generic;
using ServiceStack.ServiceHost;

namespace ServiceStack.Server
{
	public interface IContentTypes
		: IContentTypeWriter, IContentTypeReader
	{
        string GetFormatContentType(string format);

		Dictionary<string, string> ContentTypeFormats { get; }

		void Register(string contentType,
			StreamSerializerDelegate streamSerializer, StreamDeserializerDelegate streamDeserializer);

		void Register(string contentType,
			ResponseSerializerDelegate responseSerializer, StreamDeserializerDelegate streamDeserializer);

		void ClearCustomFilters();
	}

}