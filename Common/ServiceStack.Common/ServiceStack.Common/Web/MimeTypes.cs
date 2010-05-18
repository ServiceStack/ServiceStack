using System;
using ServiceStack.Common.Extensions;

namespace ServiceStack.Common.Web
{
	public static class MimeTypes
	{
		public const string Html = "text/html";
		public const string Xml = "text/xml";
		public const string Json = "text/json";
		public const string Jsv = "text/jsv";
		public const string Csv = "text/csv";
		public const string ProtoBuf = "application/x-protobuf";

		public const string JavaScript = "text/javascript";

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
				case Jsv:
					return ".jsv";
				case Csv:
					return ".csv";
				case ProtoBuf:
					return ".pbuf";
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
				case "jpeg":
				case "gif":
				case "png":
				case "tiff":
				case "bmp":
					return "image/" + fileExt;

				case "jpg":
					return "image/jpeg";

				case "tif":
					return "image/tiff";

				case "htm":
				case "html":
				case "shtml":
					return "text/html";

				case "js":
					return "text/javascript";

				case "csv":
				case "css":
				case "sgml":
					return "text/" + fileExt;

				case "txt":
					return "text/plain";

				case "wav":
					return "audio/wav";

				case "mp3":
					return "audio/mpeg3";

				case "mid":
					return "audio/midi";

				case "qt":
				case "mov":
					return "video/quicktime";

				case "mpg":
					return "video/mpeg";

				case "avi":
					return "video/" + fileExt;

				case "rtf":
					return "application/" + fileExt;

				case "xls":
					return "application/x-excel";

				case "doc":
					return "application/msword";

				case "ppt":
					return "application/powerpoint";

				case "gz":
				case "tgz":
					return "application/x-compressed";

				default:
					throw new NotSupportedException("Unknown fileExt: " + fileExt);
			}
		}
	}
}