using System;
using ServiceStack.ServiceHost;

namespace ServiceStack.WebHost.Endpoints.Support
{
	public class ContentType
	{
		public const string HeaderContentType = "Content-Type";

		public const string FormUrlEncoded = "application/x-www-form-urlencoded";

		public const string MultiPartFormData = "multipart/form-data";

		public const string Html = "text/html";

		public const string Xml = "application/xml";

		public const string XmlText = "text/xml";

		public const string Soap11 = " text/xml; charset=utf-8";

		public const string Soap12 = " application/soap+xml";

		public const string Json = "application/json";

		public const string JsonText = "text/json";

		public const string Jsv = "application/jsv";

		public const string JsvText = "text/jsv";

		public const string Csv = "text/csv";

		public const string Yaml = "application/yaml";

		public const string YamlText = "text/yaml";

		public const string PlainText = "text/plain";

		public const string ProtoBuf = "application/x-protobuf";

		public static EndpointAttributes GetEndpointAttributes(string contentType)
		{
			switch (contentType)
			{
				case Json:
				case JsonText:
					return EndpointAttributes.Json;

				case Xml:
				case XmlText:
					return EndpointAttributes.Xml;

				case Html:
					return EndpointAttributes.Html;

				case Jsv:
				case JsvText:
					return EndpointAttributes.Jsv;

				case Yaml:
				case YamlText:
					return EndpointAttributes.Yaml;

				case Csv:
					return EndpointAttributes.Csv;
			}

			throw new NotSupportedException(contentType);
		}

		public static string GetContentType(EndpointType endpointType)
		{
			switch (endpointType)
			{
				case EndpointType.Soap11:
				case EndpointType.Soap12:
				case EndpointType.Xml:
					return Xml;

				case EndpointType.Json:
					return Json;

				case EndpointType.Jsv:
					return JsvText;

				case EndpointType.ProtoBuf:
					return ProtoBuf;

				default:
					throw new NotSupportedException(endpointType.ToString());
			}
		}
	}
}