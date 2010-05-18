using System.Collections.Generic;
using System.IO;
using System.Text;
using ServiceStack.Configuration;
using ServiceStack.Service;

namespace ServiceStack.Common.Web
{
	public class TextResult : IStreamWriter,  IHasOptions
	{
		public TextResult()
			: this(null, MimeTypes.GetMimeType(".txt"))
		{
		}

		public TextResult(StringBuilder body)
			: this(body, MimeTypes.GetMimeType(".txt"))
		{
		}

		public TextResult(StringBuilder body, string mimeType)
		{
			this.Headers = new Dictionary<string, string> { { HttpHeaders.ContentType, mimeType } };
			this.Body = body ?? new StringBuilder();
		}
        
		public StringBuilder Body { get; set; }

		public Dictionary<string, string> Headers { get; set; }

		public IDictionary<string, string> Options
		{
			get { return this.Headers; }
		}

		public void WriteTo(Stream stream)
		{
			var writer = new StreamWriter(stream);
			writer.Write(this.Body);
			writer.Flush();
		}
	}
}