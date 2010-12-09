using System;
using System.Collections.Generic;
using System.IO;
using ServiceStack.ServiceHost;
using ServiceStack.Text;

namespace ServiceStack.Common.Web
{
	public class HttpResponseFilter : IContentTypeWriter
	{
		public static HttpResponseFilter Instance = new HttpResponseFilter();

		public Dictionary<string, Action<object, Stream>> ContentTypeFilters 
			= new Dictionary<string, Action<object, Stream>>();

		public void AddContentTypeFilter(string contentType, Action<object, Stream> responseWriter)
		{
			this.ContentTypeFilters.Add(contentType, responseWriter);
		}

		public void WriteToResponse(string contentType, object response, Stream responseStream)
		{
			Action<object, Stream> responseWriter;
			if (this.ContentTypeFilters.TryGetValue(contentType, out responseWriter))
			{
				responseWriter(response, responseStream);
				return;
			}

			switch (contentType)
			{
				case ContentType.Xml:
					XmlSerializer.SerializeToStream(response, responseStream);
					break;
				case ContentType.Json:
					JsonSerializer.SerializeToStream(response, responseStream);
					break;
				case ContentType.Jsv:
					TypeSerializer.SerializeToStream(response, responseStream);
					break;
				default:
					throw new NotSupportedException("ContentType not supported: " + contentType);
			}
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
	}

}