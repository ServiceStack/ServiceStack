using System.Collections.Generic;
using System.IO;
using System.Text;
using ServiceStack.Configuration;
using ServiceStack.Service;

namespace ServiceStack.WebHost.Endpoints.Results
{
	public class HtmlResult : IStreamWriter,  IHasOptions
	{
		public HtmlResult()
		{
			this.Headers = new Dictionary<string, string> { { HttpHeaders.ContentType, MimeTypes.Html } };
			this.Html = new StringBuilder();
		}

		public StringBuilder Html { get; set; }

		public Dictionary<string, string> Headers { get; set; }

		public IDictionary<string, string> Options
		{
			get { return this.Headers; }
		}

		public void WriteTo(Stream stream)
		{
			var writer = new StreamWriter(stream);
			writer.Write(this.Html);
			writer.Flush();
		}
	}
}