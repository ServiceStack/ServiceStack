using System;

namespace ServiceStack.WebHost.Endpoints.Support
{
	public class ContentType
	{
		public const string HeaderContentType = "Content-Type";

		public const string Html = "text/html";

		public const string Xml = "application/xml";

		public const string XmlText = "text/xml";

		public const string Soap12 = " application/soap+xml";

		public const string JsonText = "text/json";

		public const string Jsv = "application/jsv";

		public const string JsvText = "text/jsv";

		public const string Json = "application/json";

		public const string PlainText = "text/plain";

		public const string ProtoBuf = "application/x-protobuf";

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