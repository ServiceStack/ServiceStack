using System;
using System.Collections.Generic;
using System.IO;
using ServiceStack.Common.Extensions;
using ServiceStack.Configuration;
using ServiceStack.Service;
using ServiceStack.Text;

namespace ServiceStack.Common.Web
{
	public class FileResult
		: IStreamWriter, IHasOptions
	{
		public const string DefaultContentType = MimeTypes.Xml;

		public string FilePath { get; private set; }

		public Dictionary<string, string> HttpHeaders { get; private set; }

		public IDictionary<string, string> Options
		{
			get { return this.HttpHeaders; }
		}

		public FileResult(string filePath)
			: this(filePath, DefaultContentType) { }

		public FileResult(string filePath, string contentType)
		{
			this.FilePath = filePath;
			this.HttpHeaders = new Dictionary<string, string> {
				{ Web.HttpHeaders.ContentType, contentType },
			};
		}

		public void WriteTo(Stream stream)
		{
			using (var fs = new FileStream(this.FilePath, FileMode.Open, FileAccess.Read))
			{
				fs.WriteTo(stream);
				stream.Flush();
			}
		}

	}
}