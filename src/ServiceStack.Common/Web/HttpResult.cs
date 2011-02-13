using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using ServiceStack.Service;
using ServiceStack.ServiceHost;
using ServiceStack.Text;

namespace ServiceStack.Common.Web
{
	public class HttpResult
		: IHttpResult, IStreamWriter
	{
		public HttpResult()
			: this (null, null)
		{
		}

		public HttpResult(object response)
			: this(response, null)
		{
		}

		public HttpResult(object response, string contentType)
			: this(response, contentType, HttpStatusCode.OK)
		{
		}

		public HttpResult(object response, string contentType, HttpStatusCode statusCode)
		{
			this.Headers = new Dictionary<string, string>();
			this.ResponseFilter = HttpResponseFilter.Instance;

			this.Response = response;
			this.ContentType = contentType;
			this.StatusCode = statusCode;
		}

		public HttpResult(FileInfo fileResponse, bool asAttachment)
			: this(fileResponse, asAttachment, MimeTypes.GetMimeType(fileResponse.Name)) { }

		public HttpResult(FileInfo fileResponse, bool asAttachment, string contentType)
		{
			this.StatusCode = HttpStatusCode.OK;
			this.FileInfo = fileResponse;

			if (!asAttachment)
			{
				this.Headers = new Dictionary<string, string> {
					{ Web.HttpHeaders.ContentType, contentType },
				};
				return;
			}

			var headerValue =
				"attachment; " +
				"filename=\"" + fileResponse.Name + "\"; " +
				"size=" + fileResponse.Length + "; " +
				"creation-date=" + fileResponse.CreationTimeUtc.ToString("R") + "; " +
				"modification-date=" + fileResponse.LastWriteTimeUtc.ToString("R") + "; " +
				"read-date=" + fileResponse.LastAccessTimeUtc.ToString("R");

			this.Headers = new Dictionary<string, string> {
				{ HttpHeaders.ContentType, contentType },
				{ HttpHeaders.ContentDisposition, headerValue },
			};
		}

		public FileInfo FileInfo { get; private set; }

		public string ContentType { get; set; }

		public Dictionary<string, string> Headers { get; private set; }
		
		public IDictionary<string, string> Options
		{
			get { return this.Headers; }
		}

		public HttpStatusCode StatusCode { get; set; }

		public object Response { get; set; }

		public IContentTypeWriter ResponseFilter { get; set; }

		public IRequestContext RequestContext { get; set; }

		public void WriteTo(Stream responseStream)
		{
			if (this.FileInfo != null)
			{
				using (var fs = this.FileInfo.OpenRead())
				{
					fs.WriteTo(responseStream);
					responseStream.Flush();
				}
				return;
			}

			if (this.ResponseFilter == null)
				throw new ArgumentNullException("ResponseFilter");
			if (this.RequestContext == null)
				throw new ArgumentNullException("RequestContext");

			ResponseFilter.SerializeToStream(this.RequestContext, this.Response, responseStream);
		}
	}
}