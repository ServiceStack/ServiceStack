using System.Collections.Generic;
using ServiceStack.DesignPatterns.Serialization;

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

	    void Register(string contentType, ITextSerializer stringSerializer);

		void ClearCustomFilters();
	}

}