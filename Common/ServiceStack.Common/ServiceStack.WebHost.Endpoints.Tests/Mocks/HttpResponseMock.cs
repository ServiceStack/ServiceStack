using System.Collections.Specialized;
using System.IO;
using System.Text;
using ServiceStack.WebHost.Endpoints.Extensions;

namespace ServiceStack.WebHost.Endpoints.Tests.Mocks
{
	public class HttpResponseMock : IHttpResponse
	{
		public HttpResponseMock()
		{
			this.Headers = new NameValueCollection();
			this.OutputStream = new MemoryStream();
			this.TextWritten = new StringBuilder();
		}

		public string GetOutputStreamAsString()
		{
			this.OutputStream.Seek(0, SeekOrigin.Begin);
			using (var reader = new StreamReader(this.OutputStream))
			{
				return reader.ReadToEnd();
			}
		}

		public StringBuilder TextWritten
		{
			get;
			set;
		}
        
		public string ContentType
		{
			get; set;
		}

		public NameValueCollection Headers
		{
			get; 
			private set;
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
	}
}