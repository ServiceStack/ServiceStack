using System;
using System.Collections.Generic;
using System.IO;
using ServiceStack.Common.Extensions;
using ServiceStack.ServiceHost;
using ServiceStack.Text;

namespace ServiceStack.Common.Web
{
	public class HttpResponseFilter : IContentTypeFilter
	{
		public static HttpResponseFilter Instance = new HttpResponseFilter();

		public Dictionary<string, StreamSerializerDelegate> ContentTypeSerializers
			= new Dictionary<string, StreamSerializerDelegate>();

		public Dictionary<string, StreamDeserializerDelegate> ContentTypeDeserializers
			= new Dictionary<string, StreamDeserializerDelegate>();

		public HttpResponseFilter()
		{
			this.ContentTypeFormats = new Dictionary<string, string>();
		}

		public void ClearCustomFilters()
		{
			this.ContentTypeFormats = new Dictionary<string, string>();
			this.ContentTypeSerializers = new Dictionary<string, StreamSerializerDelegate>();
			this.ContentTypeDeserializers = new Dictionary<string, StreamDeserializerDelegate>();
		}

		public Dictionary<string, string> ContentTypeFormats { get; set; }

		public void Register(string contentType, StreamSerializerDelegate streamSerializer, StreamDeserializerDelegate streamDeserializer)
		{
			if (contentType.IsNullOrEmpty())
				throw new ArgumentNullException("contentType");

			var parts = contentType.Split('/');
			var format = parts[parts.Length - 1];
			this.ContentTypeFormats[format] = contentType;

			SetContentTypeSerializer(contentType, streamSerializer);
			SetContentTypeDeserializer(contentType, streamDeserializer);
		}

		public void SetContentTypeSerializer(string contentType, StreamSerializerDelegate streamSerializer)
		{
			this.ContentTypeSerializers[contentType] = streamSerializer;
		}

		public void SetContentTypeDeserializer(string contentType, StreamDeserializerDelegate streamDeserializer)
		{
			this.ContentTypeDeserializers[contentType] = streamDeserializer;
		}

		public string Serialize(string contentType, object response)
		{
			switch (contentType)
			{
				case ContentType.Xml:
					return XmlSerializer.SerializeToString(response);

				case ContentType.Json:
					return JsonSerializer.SerializeToString(response);

				case ContentType.Jsv:
					return TypeSerializer.SerializeToString(response);

				default:
					throw new NotSupportedException("ContentType not supported: " + contentType);
			}
		}

		public string SerializeToString(string contentType, object response)
		{
			var contentTypeAttr = ContentType.GetEndpointAttributes(contentType);
			switch (contentTypeAttr)
			{
				case EndpointAttributes.Xml:
					return XmlSerializer.SerializeToString(response);

				case EndpointAttributes.Json:
					return JsonSerializer.SerializeToString(response);

				case EndpointAttributes.Jsv:
					return TypeSerializer.SerializeToString(response);

				default:
					throw new NotSupportedException("ContentType not supported: " + contentType);
			}
		}

		public void SerializeToStream(string contentType, object response, Stream responseStream)
		{
			var serializer = GetStreamSerializer(contentType);
			if (serializer == null)
				throw new NotSupportedException("ContentType not supported: " + contentType);

			serializer(response, responseStream);
		}

		public StreamSerializerDelegate GetStreamSerializer(string contentType)
		{
			StreamSerializerDelegate responseWriter;
			if (this.ContentTypeSerializers.TryGetValue(contentType, out responseWriter))
			{
				return responseWriter;
			}

			var contentTypeAttr = ContentType.GetEndpointAttributes(contentType);
			switch (contentTypeAttr)
			{
				case EndpointAttributes.Xml:
					return XmlSerializer.SerializeToStream;

				case EndpointAttributes.Json:
					return JsonSerializer.SerializeToStream;

				case EndpointAttributes.Jsv:
					return TypeSerializer.SerializeToStream;
			}

			return null;
		}

		public object DeserializeFromString(string contentType, Type type, string request)
		{
			var contentTypeAttr = ContentType.GetEndpointAttributes(contentType);
			switch (contentTypeAttr)
			{
				case EndpointAttributes.Xml:
					return XmlSerializer.DeserializeFromString(request, type);

				case EndpointAttributes.Json:
					return JsonSerializer.DeserializeFromString(request, type);

				case EndpointAttributes.Jsv:
					return TypeSerializer.DeserializeFromString(request, type);
				
				default:
					throw new NotSupportedException("ContentType not supported: " + contentType);
			}
		}

		public object DeserializeFromStream(string contentType, Type type, Stream fromStream)
		{
			var deserializer = GetStreamDeserializer(contentType);
			if (deserializer == null)
				throw new NotSupportedException("ContentType not supported: " + contentType);

			return deserializer(type, fromStream);
		}

		public StreamDeserializerDelegate GetStreamDeserializer(string contentType)
		{
			StreamDeserializerDelegate streamReader;
			if (this.ContentTypeDeserializers.TryGetValue(contentType, out streamReader))
			{
				return streamReader;
			}

			var contentTypeAttr = ContentType.GetEndpointAttributes(contentType);
			switch (contentTypeAttr)
			{
				case EndpointAttributes.Xml:
					return XmlSerializer.DeserializeFromStream;

				case EndpointAttributes.Json:
					return JsonSerializer.DeserializeFromStream;

				case EndpointAttributes.Jsv:
					return TypeSerializer.DeserializeFromStream;
			}

			return null;
		}
	}

}