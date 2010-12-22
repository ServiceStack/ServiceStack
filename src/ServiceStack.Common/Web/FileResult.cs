using System.Collections.Generic;
using System.IO;
using ServiceStack.Service;
using ServiceStack.ServiceHost;
using ServiceStack.Text;

namespace ServiceStack.Common.Web
{
	public class FileResult
		: IStreamWriter, IHasOptions
	{
		public FileInfo FileInfo { get; private set; }

		public Dictionary<string, string> HttpHeaders { get; private set; }

		public IDictionary<string, string> Options
		{
			get { return this.HttpHeaders; }
		}

		public FileResult(string filePath)
			: this(new FileInfo(filePath)) { }

		public FileResult(FileInfo fileInfo)
			: this(fileInfo, MimeTypes.GetMimeType(fileInfo.Name)) { }

		public FileResult(FileInfo fileInfo, string contentType)
		{
			this.FileInfo = fileInfo;

			var headerValue =
				"attachment; " +
				"filename=\"" + fileInfo.Name + "\"; " +
				"size=" + fileInfo.Length + "; " +
				"creation-date=" + fileInfo.CreationTimeUtc.ToString("R") + "; " +
				"modification-date=" + fileInfo.LastWriteTimeUtc.ToString("R") + "; " +
				"read-date=" + fileInfo.LastAccessTimeUtc.ToString("R");

			this.HttpHeaders = new Dictionary<string, string> {
				{ Web.HttpHeaders.ContentType, contentType },
				{ Web.HttpHeaders.ContentDisposition, headerValue },
			};
		}

		public void WriteTo(Stream stream)
		{
			using (var fs = this.FileInfo.OpenRead())
			{
				fs.WriteTo(stream);
				stream.Flush();
			}
		}

	}
}