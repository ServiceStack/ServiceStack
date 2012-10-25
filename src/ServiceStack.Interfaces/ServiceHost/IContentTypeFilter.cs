using System.Collections.Generic;

namespace ServiceStack.ServiceHost
{
	public interface IContentTypeFilter
		: IContentTypeWriter, IContentTypeReader
	{
		Dictionary<string, string> ContentTypeFormats { get; }

		void Register(string contentType,
			StreamSerializerDelegate streamSerializer, StreamDeserializerDelegate streamDeserializer);

		void Register(string contentType,
			ResponseSerializerDelegate responseSerializer, StreamDeserializerDelegate streamDeserializer);

		void ClearCustomFilters();
	}

}