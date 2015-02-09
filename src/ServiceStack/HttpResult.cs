#if !SL5
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using ServiceStack.Host;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack
{
    public class HttpResult
        : IHttpResult, IStreamWriter, IPartialWriter
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
            this.ResponseFilter = ContentTypes.Instance;

            this.Response = response;
            this.ContentType = contentType;
            this.StatusCode = statusCode;
        }

        public HttpResult(FileInfo fileResponse, bool asAttachment)
            : this(fileResponse, MimeTypes.GetMimeType(fileResponse.Name), asAttachment) { }

        public HttpResult(FileInfo fileResponse, string contentType=null, bool asAttachment=false)
            : this(null, contentType ?? MimeTypes.GetMimeType(fileResponse.Name), HttpStatusCode.OK)
        {
            this.FileInfo = fileResponse;
            this.AllowsPartialResponse = true;

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
            this.AllowsPartialResponse = true;
            this.ResponseStream = responseStream;
        }

        public HttpResult(string responseText, string contentType)
            : this(null, contentType, HttpStatusCode.OK)
        {
            this.AllowsPartialResponse = true;
            this.ResponseText = responseText;
        }

        public HttpResult(byte[] responseBytes, string contentType)
            : this(null, contentType, HttpStatusCode.OK)
        {
            this.AllowsPartialResponse = true;
            this.ResponseStream = new MemoryStream(responseBytes);
        }

        public string ResponseText { get; private set; }

        public Stream ResponseStream { get; private set; }

        public FileInfo FileInfo { get; private set; }

        public string ContentType { get; set; }

        public Dictionary<string, string> Headers { get; private set; }

        private bool allowsPartialResponse;
        public bool AllowsPartialResponse
        {
            set
            {
                allowsPartialResponse = value;
                if (allowsPartialResponse)
                    this.Headers.Add(HttpHeaders.AcceptRanges, "bytes");
                else
                    this.Headers.Remove(HttpHeaders.AcceptRanges);
            }
            get { return allowsPartialResponse; }
        }

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
            get { return (HttpStatusCode)Status; }
            set { Status = (int)value; }
        }

        public string StatusDescription { get; set; }

        public object Response { get; set; }

        public IContentTypeWriter ResponseFilter { get; set; }

        public IRequest RequestContext { get; set; }

        public string View { get; set; }

        public string Template { get; set; }

        public int PaddingLength { get; set; }

        public void WriteTo(Stream responseStream)
        {
            try
            {
                WriteTo(responseStream, PaddingLength);
                responseStream.Flush();
            }
            finally
            {
                DisposeStream();
            }
        }

        private void WriteTo(Stream responseStream, int paddingLength)
        {
            var response = RequestContext != null ? RequestContext.Response : null;
            if (this.FileInfo != null)
            {
                if (response != null)
                    response.SetContentLength(FileInfo.Length + paddingLength);

                using (var fs = this.FileInfo.OpenRead())
                {
                    fs.WriteTo(responseStream);
                    return;
                }
            }

            if (this.ResponseStream != null)
            {
                if (response != null)
                {
                    var ms = ResponseStream as MemoryStream;
                    if (ms != null)
                    {
                        var bytes = ms.ToArray();
                        response.SetContentLength(bytes.LongLength + paddingLength);

                        responseStream.Write(bytes, 0, bytes.Length);
                        return;
                    }
                }

                this.ResponseStream.WriteTo(responseStream);
                return;
            }

            if (this.ResponseText != null)
            {
                var bytes = Encoding.UTF8.GetBytes(this.ResponseText);
                if (response != null)
                    response.SetContentLength(bytes.LongLength + paddingLength);

                responseStream.Write(bytes, 0, bytes.Length);
                return;
            }

            if (this.ResponseFilter == null)
                throw new ArgumentNullException("ResponseFilter");
            if (this.RequestContext == null)
                throw new ArgumentNullException("RequestContext");

            var bytesResponse = this.Response as byte[];
            if (bytesResponse != null)
            {
                if (response != null)
                    response.SetContentLength(bytesResponse.LongLength + paddingLength);

                responseStream.Write(bytesResponse, 0, bytesResponse.Length);
                return;
            }

            if (View != null)
                RequestContext.SetItem("View", View);
            if (Template != null)
                RequestContext.SetItem("Template", Template);

            RequestContext.SetItem("HttpResult", this);

            ResponseFilter.SerializeToStream(this.RequestContext, this.Response, responseStream);
        }

        public bool IsPartialRequest
        {
            get { return AllowsPartialResponse && RequestContext.GetHeader(HttpHeaders.Range) != null && GetContentLength() != null; }
        }

        public void WritePartialTo(IResponse response)
        {
            var contentLength = GetContentLength().GetValueOrDefault(int.MaxValue); //Safe as guarded by IsPartialRequest
            var rangeHeader = RequestContext.GetHeader(HttpHeaders.Range);

            long rangeStart, rangeEnd;
            rangeHeader.ExtractHttpRanges(contentLength, out rangeStart, out rangeEnd);

            if (rangeEnd > contentLength - 1)
                rangeEnd = contentLength - 1;

            response.AddHttpRangeResponseHeaders(rangeStart, rangeEnd, contentLength);

            var outputStream = response.OutputStream;
            if (FileInfo != null)
            {
                using (var fs = FileInfo.OpenRead())
                {
                    fs.WritePartialTo(outputStream, rangeStart, rangeEnd);
                }
            }
            else if (ResponseStream != null)
            {
                try
                {
                    ResponseStream.WritePartialTo(outputStream, rangeStart, rangeEnd);
                }
                finally 
                {
                    DisposeStream();
                }
            }
            else if (ResponseText != null)
            {
                using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(ResponseText)))
                {
                    ms.WritePartialTo(outputStream, rangeStart, rangeEnd);
                }
            }
            else
                throw new InvalidOperationException("Neither file, stream nor text were set when attempting to write to the Response Stream.");
        }

        public long? GetContentLength()
        {
            if (FileInfo != null)
                return FileInfo.Length;
            if (ResponseStream != null)
                return ResponseStream.Length;
            if (ResponseText != null)
                return ResponseText.Length;
            return null;
        }

        public static HttpResult Status201Created(object response, string newLocationUri)
        {
            return new HttpResult(response)
            {
                StatusCode = HttpStatusCode.Created,
                Headers =
                {
                    { HttpHeaders.Location, newLocationUri },
                }
            };
        }

        public static HttpResult Redirect(string newLocationUri, HttpStatusCode redirectStatus = HttpStatusCode.Found)
        {
            return new HttpResult
            {
                StatusCode = redirectStatus,
                Headers =
                {
                    { HttpHeaders.Location, newLocationUri },
                }
            };
        }

        /// <summary>
        /// Respond with a 'Soft redirect' so smart clients (e.g. ajax) have access to the response and 
        /// can decide whether or not they should redirect
        /// </summary>
        public static HttpResult SoftRedirect(string newLocationUri, object response = null)
        {
            return new HttpResult(response)
            {
                Headers =
                {
                    { HttpHeaders.XLocation, newLocationUri },
                }
            };
        }

        /// <summary>
        /// Decorate the response with an additional client-side event to instruct participating 
        /// smart clients (e.g. ajax) with hints to transparently invoke client-side functionality
        /// </summary>
        public static HttpResult TriggerEvent(object response, string eventName, string value=null)
        {
            return new HttpResult(response)
            {
                Headers =
                {
                    { HttpHeaders.XTrigger, eventName + (value != null ? ":" + value : "") },
                }
            };
        }

        private void DisposeStream()
        {
            try
            {
                if (ResponseStream != null)
                {
                    this.ResponseStream.Dispose();
                }
            }
            catch { /*ignore*/ }
        }
    }
}
#endif