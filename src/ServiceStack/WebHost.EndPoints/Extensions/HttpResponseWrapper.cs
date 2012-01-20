using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Web;
using ServiceStack.Common.Web;
using ServiceStack.Logging;
using ServiceStack.Net30;
using ServiceStack.ServiceHost;
using ServiceStack.Text;

namespace ServiceStack.WebHost.Endpoints.Extensions
{
	public class HttpResponseWrapper
		: IHttpResponse
	{
		//private static readonly ILog Log = LogManager.GetLogger(typeof(HttpResponseWrapper));

		private readonly HttpResponse response;

		public HttpResponseWrapper(HttpResponse response)
		{
			this.response = response;
			this.Cookies = new Cookies(this);
		}

		public HttpResponse Response
		{
			get { return response; }
		}

		public object OriginalResponse
		{
			get { return response; }
		}

		public int StatusCode
		{
			set { this.response.StatusCode = value; }
		}

		public string StatusDescription
		{
			set { this.response.StatusDescription = value; }
		}

		public string ContentType
		{
			get { return response.ContentType; }
			set { response.ContentType = value; }
		}

		public ICookies Cookies { get; set; }

		public void AddHeader(string name, string value)
		{
			response.AddHeader(name, value);
		}

		public void Redirect(string url)
		{
			response.Redirect(url);
		}

		public Stream OutputStream
		{
			get { return response.OutputStream; }
		}

		public void Write(string text)
		{
			response.Write(text);
		}

		public void Close()
		{
			this.IsClosed = true;
			response.CloseOutputStream();
		}

		public void End()
		{
			this.IsClosed = true;
			try
			{
				response.ClearContent();
				response.End();
			}
			catch {}
		}

		public void Flush()
		{
			response.Flush();
		}

		public bool IsClosed
		{
			get;
			private set;
		}
	}
}