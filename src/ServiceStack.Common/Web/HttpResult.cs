#if !SILVERLIGHT
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
            : this((object)null, null)
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

        public HttpResult(HttpStatusCode statusCode, string statusDescription)
            : this()
        {
            StatusCode = statusCode;
            StatusDescription = statusDescription;
        }

        public HttpResult(object response, HttpStatusCode statusCode)
            : this(response, null, statusCode) { }

        public HttpResult(object response, string contentType, HttpStatusCode statusCode)
        {
            this.Headers = new Dictionary<string, string>();
            this.ResponseFilter = HttpResponseFilter.Instance;

            this.Response = response;
            this.ContentType = contentType;
            this.StatusCode = statusCode;
        }

        public HttpResult(FileInfo fileResponse)
            : this(fileResponse, false, MimeTypes.GetMimeType(fileResponse.Name)) { }

        public HttpResult(FileInfo fileResponse, bool asAttachment)
            : this(fileResponse, asAttachment, MimeTypes.GetMimeType(fileResponse.Name)) { }

        public HttpResult(FileInfo fileResponse, bool asAttachment, string contentType)
            : this(null, contentType, HttpStatusCode.OK)
        {
            this.FileInfo = fileResponse;

            if (!asAttachment) return;

            var headerValue =
                "attachment; " +
                "filename=\"" + fileResponse.Name + "\"; " +
                "size=" + fileResponse.Length + "; " +
                "creation-date=" + fileResponse.CreationTimeUtc.ToString("R").Replace(",", "") + "; " +
                "modification-date=" + fileResponse.LastWriteTimeUtc.ToString("R").Replace(",", "") + "; " +
                "read-date=" + fileResponse.LastAccessTimeUtc.ToString("R").Replace(",", "");

            this.Headers = new Dictionary<string, string> {
                { HttpHeaders.ContentDisposition, headerValue },
            };
        }

        public HttpResult(Stream responseStream, string contentType)
            : this(null, contentType, HttpStatusCode.OK)
        {
            this.ResponseStream = responseStream;
        }

        public HttpResult(string responseText, string contentType)
            : this(null, contentType, HttpStatusCode.OK)
        {
            this.ResponseText = responseText;
        }

        public string ResponseText { get; private set; }

        public Stream ResponseStream { get; private set; }

        public FileInfo FileInfo { get; private set; }

        public string ContentType { get; set; }

        public Dictionary<string, string> Headers { get; private set; }

        public DateTime LastModified
        {
            set
            {
                this.Headers[HttpHeaders.LastModified] = value.ToUniversalTime().ToString("r");
            }
        }

        public string Location
        {
            set
            {
                if (StatusCode == HttpStatusCode.OK)
                    StatusCode = HttpStatusCode.Redirect;

                this.Headers[HttpHeaders.Location] = value;
            }
        }

        public void SetPermanentCookie(string name, string value)
        {
            SetCookie(name, value, DateTime.UtcNow.AddYears(20), null);
        }

        public void SetPermanentCookie(string name, string value, string path)
        {
            SetCookie(name, value, DateTime.UtcNow.AddYears(20), path);
        }

        public void SetSessionCookie(string name, string value)
        {
            SetSessionCookie(name, value, null);
        }

        public void SetSessionCookie(string name, string value, string path)
        {
            path = path ?? "/";
            this.Headers[HttpHeaders.SetCookie] = string.Format("{0}={1};path=" + path, name, value);
        }

        public void SetCookie(string name, string value, TimeSpan expiresIn, string path)
        {
            var expiresAt = DateTime.UtcNow.Add(expiresIn);
            SetCookie(name, value, expiresAt, path);
        }

        public void SetCookie(string name, string value, DateTime expiresAt, string path)
        {
            path = path ?? "/";
            var cookie = string.Format("{0}={1};expires={2};path={3}", name, value, expiresAt.ToString("R"), path);
            this.Headers[HttpHeaders.SetCookie] = cookie;
        }

        public void DeleteCookie(string name)
        {
            var cookie = string.Format("{0}=;expires={1};path=/", name, DateTime.UtcNow.AddDays(-1).ToString("R"));
            this.Headers[HttpHeaders.SetCookie] = cookie;
        }

        public IDictionary<string, string> Options
        {
            get { return this.Headers; }
        }

        public int Status { get; set; }

        public HttpStatusCode StatusCode
        {
            get { return (HttpStatusCode) Status; }
            set { Status = (int) value; }
        }

        public string StatusDescription { get; set; }

        public object Response { get; set; }

        public IContentTypeWriter ResponseFilter { get; set; }

        public IRequestContext RequestContext { get; set; }

        public string View { get; set; }

        public string Template { get; set; }

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

            if (this.ResponseStream != null)
            {
                this.ResponseStream.WriteTo(responseStream);
                responseStream.Flush();
                try
                {
                    this.ResponseStream.Dispose();
                }
                catch { /*ignore*/ }

                return;
            }

            if (this.ResponseText != null)
            {
                var bytes = System.Text.Encoding.UTF8.GetBytes(this.ResponseText);
                responseStream.Write(bytes, 0, bytes.Length);
                responseStream.Flush();
                return;
            }

            if (this.ResponseFilter == null)
                throw new ArgumentNullException("ResponseFilter");
            if (this.RequestContext == null)
                throw new ArgumentNullException("RequestContext");

            var bytesResponse = this.Response as byte[];
            if (bytesResponse != null)
            {
                responseStream.Write(bytesResponse, 0, bytesResponse.Length);
                return;
            }

            if (View != null)
                RequestContext.SetItem("View", View);
            if (Template != null)
                RequestContext.SetItem("Template", Template);

            ResponseFilter.SerializeToStream(this.RequestContext, this.Response, responseStream);
        }

        public static HttpResult Status201Created(object response, string newLocationUri)
        {
            return new HttpResult(response) {
                StatusCode = HttpStatusCode.Created,
                Headers =
                {
                    { HttpHeaders.Location, newLocationUri },
                }
            };
        }
    }
}
#endif