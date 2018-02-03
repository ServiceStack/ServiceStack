#if !SL5
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using ServiceStack.Host;
using ServiceStack.IO;
using ServiceStack.Logging;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack
{
    public class HttpResult
        : IHttpResult, IStreamWriterAsync, IPartialWriterAsync, IDisposable
    {
        public HttpResult()
            : this((object)null, null) { }

        public HttpResult(object response)
            : this(response, null) { }

        public HttpResult(object response, string contentType)
            : this(response, contentType, HttpStatusCode.OK) { }

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
            this.Cookies = new List<Cookie>();
            this.ResponseFilter = HostContext.AppHost?.ContentTypes ?? ContentTypes.Instance;

            this.Response = response;
            this.ContentType = contentType;
            this.StatusCode = statusCode;
        }

        public HttpResult(FileInfo fileResponse, bool asAttachment)
            : this(fileResponse, MimeTypes.GetMimeType(fileResponse.Name), asAttachment)
        { }

        public HttpResult(FileInfo fileResponse, string contentType = null, bool asAttachment = false)
            : this(null, contentType ?? MimeTypes.GetMimeType(fileResponse.Name), HttpStatusCode.OK)
        {
            this.FileInfo = fileResponse ?? throw new ArgumentNullException(nameof(fileResponse));
            this.LastModified = fileResponse.LastWriteTime;
            this.AllowsPartialResponse = true;
            if (!FileInfo.Exists)
                throw HttpError.NotFound($"{FileInfo.Name} was not found");

            if (!asAttachment) return;

            var headerValue = $"attachment; filename=\"{fileResponse.Name}\"; size={fileResponse.Length}; " +
                              $"creation-date={fileResponse.CreationTimeUtc.ToString("R").Replace(",", "")}; " +
                              $"modification-date={fileResponse.LastWriteTimeUtc.ToString("R").Replace(",", "")}; " +
                              $"read-date={fileResponse.LastAccessTimeUtc.ToString("R").Replace(",", "")}";

            this.Headers = new Dictionary<string, string>
            {
                { HttpHeaders.ContentDisposition, headerValue },
            };
        }

        public HttpResult(IVirtualFile fileResponse, bool asAttachment)
            : this(fileResponse, MimeTypes.GetMimeType(fileResponse.Name), asAttachment)
        { }

        public HttpResult(IVirtualFile fileResponse, string contentType = null, bool asAttachment = false)
            : this(null, contentType ?? MimeTypes.GetMimeType(fileResponse.Name), HttpStatusCode.OK)
        {
            if (fileResponse == null)
                throw new ArgumentNullException(nameof(fileResponse));

            this.AllowsPartialResponse = true;
            this.LastModified = fileResponse.LastModified;
            this.ResponseStream = fileResponse.OpenRead();

            if (!asAttachment) return;

            var headerValue = $"attachment; filename=\"{fileResponse.Name}\"; size={fileResponse.Length}; modification-date={fileResponse.LastModified.ToString("R").Replace(",", "")}";

            this.Headers = new Dictionary<string, string>
            {
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
            this.ResponseStream = MemoryStreamFactory.GetStream(responseBytes);
        }

        public string ResponseText { get; }

        public Stream ResponseStream { get; private set; }

        public FileInfo FileInfo { get; }

        public string ContentType { get; set; }

        public Dictionary<string, string> Headers { get; }

        public List<Cookie> Cookies { get; }

        public string ETag { get; set; }

        public TimeSpan? Age { get; set; }

        public TimeSpan? MaxAge { get; set; }

        public DateTime? Expires { get; set; }

        public DateTime? LastModified { get; set; }

        public CacheControl CacheControl { get; set; }

        public Func<IDisposable> ResultScope { get; set; }

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
            get => allowsPartialResponse;
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
            this.Headers[HttpHeaders.SetCookie] = $"{name}={value};path={path}";
        }

        public void SetCookie(string name, string value, TimeSpan expiresIn, string path)
        {
            var expiresAt = DateTime.UtcNow.Add(expiresIn);
            SetCookie(name, value, expiresAt, path);
        }

        public void SetCookie(string name, string value, DateTime expiresAt, string path, bool secure = false, bool httpOnly = false)
        {
            path = path ?? "/";
            var cookie = $"{name}={value};expires={expiresAt:R};path={path}";
            if (secure)
                cookie += ";Secure";
            if (httpOnly)
                cookie += ";HttpOnly";

            this.Headers[HttpHeaders.SetCookie] = cookie;
        }

        public void DeleteCookie(string name)
        {
            var cookie = $"{name}=;expires={DateTime.UtcNow.AddDays(-1):R};path=/";
            this.Headers[HttpHeaders.SetCookie] = cookie;
        }

        public IDictionary<string, string> Options => this.Headers;

        public int Status { get; set; }

        public HttpStatusCode StatusCode
        {
            get => (HttpStatusCode)Status;
            set => Status = (int)value;
        }

        public string StatusDescription { get; set; }

        public object Response { get; set; }

        public IContentTypeWriter ResponseFilter { get; set; }

        public IRequest RequestContext { get; set; }

        public string View { get; set; }

        public string Template { get; set; }

        public int PaddingLength { get; set; }

        public async Task WriteToAsync(Stream responseStream, CancellationToken token = new CancellationToken())
        {
            try
            {
                await WriteToAsync(responseStream, PaddingLength, token);
                await responseStream.FlushAsync(token);
            }
            finally
            {
                DisposeStream();
            }
        }

        private async Task WriteToAsync(Stream responseStream, int paddingLength, CancellationToken token)
        {
            var response = RequestContext?.Response;
            if (this.FileInfo != null)
            {
                response?.SetContentLength(FileInfo.Length + paddingLength);

                using (var fs = this.FileInfo.OpenRead())
                {
                    await fs.CopyToAsync(responseStream, token);
                    return;
                }
            }

            if (this.ResponseStream != null)
            {
                try
                {
                    if (response != null)
                    {
                        if (ResponseStream is MemoryStream ms)
                        {
                            var bytes = ms.ToArray();
                            response.SetContentLength(bytes.Length + paddingLength);

                            await responseStream.WriteAsync(bytes, 0, bytes.Length, token); //Write Sync to MemoryStream
                            return;
                        }
                    }

                    await ResponseStream.CopyToAsync(responseStream, token);
                }
                finally
                {
                    DisposeStream();
                }
                return;
            }

            if (this.ResponseText != null)
            {
                var bytes = Encoding.UTF8.GetBytes(this.ResponseText);
                response?.SetContentLength(bytes.Length + paddingLength);

                await responseStream.WriteAsync(bytes, token);
                return;
            }

            if (this.ResponseFilter == null)
                throw new ArgumentNullException(nameof(ResponseFilter));
            if (this.RequestContext == null)
                throw new ArgumentNullException(nameof(RequestContext));

            if (this.Response is byte[] bytesResponse)
            {
                response?.SetContentLength(bytesResponse.Length + paddingLength);

                await responseStream.WriteAsync(bytesResponse, token);
                return;
            }

            if (View != null)
                RequestContext.SetItem("View", View);
            if (Template != null)
                RequestContext.SetItem("Template", Template);

            RequestContext.SetItem("HttpResult", this);

            if (Response != null || View != null || Template != null) //allow new HttpResult { View = "/default.cshtml" }
            {
                await ResponseFilter.SerializeToStreamAsync(this.RequestContext, this.Response, responseStream);
            }
        }
        
        public bool IsPartialRequest => 
            AllowsPartialResponse && RequestContext.GetHeader(HttpHeaders.Range) != null && GetContentLength() != null;

        public async Task WritePartialToAsync(IResponse response, CancellationToken token = default(CancellationToken))
        {
            var contentLength = GetContentLength().GetValueOrDefault(int.MaxValue); //Safe as guarded by IsPartialRequest
            var rangeHeader = RequestContext.GetHeader(HttpHeaders.Range);

            rangeHeader.ExtractHttpRanges(contentLength, out var rangeStart, out var rangeEnd);

            if (rangeEnd > contentLength - 1)
                rangeEnd = contentLength - 1;

            response.AddHttpRangeResponseHeaders(rangeStart, rangeEnd, contentLength);

            var outputStream = response.OutputStream;
            if (FileInfo != null)
            {
                using (var fs = FileInfo.OpenRead())
                {
                    await fs.WritePartialToAsync(outputStream, rangeStart, rangeEnd, token);
                }
            }
            else if (ResponseStream != null)
            {
                try
                {
                    await ResponseStream.WritePartialToAsync(outputStream, rangeStart, rangeEnd, token);
                }
                finally
                {
                    DisposeStream();
                }
            }
            else if (ResponseText != null)
            {
                using (var ms = MemoryStreamFactory.GetStream(Encoding.UTF8.GetBytes(ResponseText)))
                {
                    await ms.WritePartialToAsync(outputStream, rangeStart, rangeEnd, token);
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
            return ResponseText?.Length;
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
        public static HttpResult TriggerEvent(object response, string eventName, string value = null)
        {
            return new HttpResult(response)
            {
                Headers =
                {
                    { HttpHeaders.XTrigger, eventName + (value != null ? ":" + value : "") },
                }
            };
        }

        public static HttpResult NotModified(string description = null,
            CacheControl? cacheControl = null,
            TimeSpan? maxAge = null,
            string eTag = null,
            DateTime? lastModified = null)
        {
            return new HttpResult(HttpStatusCode.NotModified,
                description ?? HostContext.ResolveLocalizedString(LocalizedStrings.NotModified))
            {
                ETag = eTag,
                LastModified = lastModified,
                MaxAge = maxAge,
                CacheControl = cacheControl.GetValueOrDefault(CacheControl.None),
            };
        }

        private void DisposeStream()
        {
            try
            {
                if (ResponseStream == null) return;
                this.ResponseStream.Dispose();
                this.ResponseStream = null;
            }
            catch { /*ignore*/ }
        }

        public void Dispose()
        {
            DisposeStream();
            using (Response as IDisposable) {}
        }
    }

    [Flags]
    public enum CacheControl : long
    {
        None = 0,
        Public = 1 << 0,
        Private = 1 << 1,
        MustRevalidate = 1 << 2,
        NoCache = 1 << 3,
        NoStore = 1 << 4,
        NoTransform = 1 << 5,
        ProxyRevalidate = 1 << 6,
    }

#if !NETSTANDARD2_0
    public static class HttpResultExtensions
    {
        public static System.Net.Cookie ToCookie(this HttpCookie httpCookie)
        {
            var to = new System.Net.Cookie(httpCookie.Name, httpCookie.Value, httpCookie.Path)
            {
                Expires = httpCookie.Expires,
                Secure = httpCookie.Secure,
                HttpOnly = httpCookie.HttpOnly,
            };

            if (httpCookie.Domain != null)
                to.Domain = httpCookie.Domain;

            return to;
        }
    }
#endif

}
#endif