using System;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Text;
using ServiceStack.Common.Web;
using ServiceStack.ServiceHost;

namespace ServiceStack.WebHost.Endpoints.Tests.Mocks
{
	public class HttpResponseMock
		: IHttpResponse
	{
		public HttpResponseMock()
		{
			this.Headers = new NameValueCollection();
			this.OutputStream = new MemoryStream();
			this.TextWritten = new StringBuilder();
			this.Cookies = new Cookies(this);
		}

		public object OriginalResponse
		{
			get { return null; }
		}

		public string GetOutputStreamAsString()
		{
			this.OutputStream.Seek(0, SeekOrigin.Begin);
			using (var reader = new StreamReader(this.OutputStream))
			{
				return reader.ReadToEnd();
			}
		}

		public byte[] GetOutputStreamAsBytes()
		{
			var ms = (MemoryStream)this.OutputStream;
			return ms.ToArray();
		}

		public StringBuilder TextWritten
		{
			get;
			set;
		}

		public int StatusCode { get; set; }

		private string statusDescription = string.Empty;
		public string StatusDescription
		{
			get
			{
				return statusDescription;
			}
			set
			{
				statusDescription = value;
			}
		}

		public string ContentType
		{
			get;
			set;
		}

		public NameValueCollection Headers
		{
			get;
			private set;
		}

		public ICookies Cookies { get; set; }

		public void AddHeader(string name, string value)
		{
			this.Headers.Add(name, value);
		}

		public void Redirect(string url)
		{
			this.Headers.Add(HttpHeaders.Location, url.MapServerPath());
		}

		public Stream OutputStream
		{
			get;
			private set;
		}

		public void Write(string text)
		{
			this.TextWritten.Append(text);
		}

		public void Close()
		{
			this.IsClosed = true;
			OutputStream.Position = 0;
		}

		public void End()
		{
			Close();
		}

		public void Flush()
		{
			OutputStream.Flush();
		}

		public bool IsClosed
		{
			get;
			private set;
		}

	    public void SetContentLength(long contentLength)
	    {
	        Headers[HttpHeaders.ContentLength] = contentLength.ToString(CultureInfo.InvariantCulture);
	    }
	}
}