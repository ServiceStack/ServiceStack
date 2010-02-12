using System;
using ServiceStack.Common.Extensions;

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

		public static string GetMimeType(string fileExt)
		{
			fileExt.ThrowIfNullOrEmpty();
			fileExt = fileExt.TrimStart('.');

			switch (fileExt)
			{
				case "jpg":
				case "jpeg":
				case "gif":
				case "png":
					return "image/" + fileExt;

				case "htm":
				case "html":
					return "text/html";

				case "js":
					return "text/javascript";

				case "css":
					return "text/" + fileExt;

				default:
					throw new NotSupportedException("Unknown fileExt: " + fileExt);
			}
		}
	}
}