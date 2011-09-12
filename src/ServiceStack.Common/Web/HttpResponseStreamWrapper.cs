using System.Collections.Generic;
using System.IO;
using System.Text;
using ServiceStack.Common.Utils;
using ServiceStack.ServiceHost;

namespace ServiceStack.Common.Web
{
	public class HttpResponseStreamWrapper : IHttpResponse
	{
		private static readonly UTF8Encoding UTF8EncodingWithoutBom = new UTF8Encoding(false);
        private TextWriter _output; 

		public HttpResponseStreamWrapper(Stream stream)
		{
			this.OutputStream = stream;
            _output = new StreamWriter(stream, UTF8EncodingWithoutBom);
			this.Headers = new Dictionary<string, string>();
		}

		public Dictionary<string, string> Headers { get; set; }
		public int StatusCode { set; private get; }
        public string StatusDescription { set; private get; }
		public string ContentType { get; set; }

		public void AddHeader(string name, string value)
		{
			this.Headers[name] = value;
		}

		public void Redirect(string url)
		{
			this.Headers[HttpHeaders.Location] = url;
		}

		public Stream OutputStream { get; private set; }

		public void Write(string text)
		{
            Output.Write(text);
            Output.Flush();
		}

		public void Close()
		{
			if (IsClosed) return;
            Output.Flush();
			OutputStream.Close();
			IsClosed = true;
		}

		public bool IsClosed { get; private set; }


        public TextWriter Output
        {
            get { throw new System.NotImplementedException(); }
        }
    }
}