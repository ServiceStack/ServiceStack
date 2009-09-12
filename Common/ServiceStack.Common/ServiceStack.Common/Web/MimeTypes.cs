using System;

namespace ServiceStack.Common.Web
{
	public static class MimeTypes
	{
		public const string Html = "text/html";
		public const string Xml = "text/xml";
		public const string Json = "text/json";

		public static string GetExtension(string mimeType)
		{
			switch (mimeType)
			{
				case Html:
					return ".html";
				case Xml:
					return ".xml";
				case Json:
					return ".js";
				default:
					throw new NotSupportedException("Unknown mimeType: " + mimeType);
			}
		}
	}
}