using System;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using ServiceStack.Logging;

namespace ServiceStack.WebHost.Endpoints.Extensions
{
	internal class HttpListenerResponseWrapper 
		: IHttpResponse
	{
		private static readonly ILog Log = LogManager.GetLogger(typeof (HttpListenerResponseWrapper));

		private readonly HttpListenerResponse response;

		public HttpListenerResponseWrapper(HttpListenerResponse response)
		{
			this.response = response;
		}

		public int StatusCode
		{
			set { this.response.StatusCode = value; }
		}

		public string ContentType
		{
			get { return response.ContentType; }
			set { response.ContentType = value; }
		}

		public void AddHeader(string name, string value)
		{
			response.AddHeader(name, value);
		}

		public Stream OutputStream
		{
			get { return response.OutputStream; }
		}

		public void Write(string text)
		{
			try
			{
				var bOutput = System.Text.Encoding.UTF8.GetBytes(text);
				response.ContentLength64 = bOutput.Length;

				var outputStream = response.OutputStream;
				outputStream.Write(bOutput, 0, bOutput.Length);
				outputStream.Close();
			}
			catch (Exception ex)
			{
				Log.Error("Could not WriteTextToResponse: " + ex.Message, ex);
				throw;
			}
		}
	}
}