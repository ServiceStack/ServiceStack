using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using ServiceStack.Web;

namespace ServiceStack
{
    public class CompressedResult
        : IStreamWriter, IHttpResult
    {
        public const int Adler32ChecksumLength = 4;

        public const string DefaultContentType = MimeTypes.Xml;

        public byte[] Contents { get; }

        public string ContentType { get; set; }

        public Dictionary<string, string> Headers { get; }
        public List<Cookie> Cookies { get; }

        public int Status { get; set; }

        public HttpStatusCode StatusCode
        {
            get { return (HttpStatusCode)Status; }
            set { Status = (int)value; }
        }

        public string StatusDescription { get; set; }

        public object Response
        {
            get { return this.Contents; }
            set { throw new NotImplementedException(); }
        }

        public IContentTypeWriter ResponseFilter { get; set; }

        public IRequest RequestContext { get; set; }

        public int PaddingLength { get; set; }

        public Func<IDisposable> ResultScope { get; set; }

        public IDictionary<string, string> Options => this.Headers;

        public DateTime? LastModified
        {
            set
            {
                if (value == null)
                    return;

                this.Headers[HttpHeaders.LastModified] = value.Value.ToUniversalTime().ToString("r");

                var feature = HostContext.GetPlugin<HttpCacheFeature>();
                if (feature?.CacheControlForOptimizedResults != null)
                    this.Headers[HttpHeaders.CacheControl] = feature.CacheControlForOptimizedResults;
            }
        }

        public CompressedResult(byte[] contents)
            : this(contents, CompressionTypes.Deflate)
        { }

        public CompressedResult(byte[] contents, string compressionType)
            : this(contents, compressionType, DefaultContentType)
        { }

        public CompressedResult(byte[] contents, string compressionType, string contentMimeType)
        {
            if (!CompressionTypes.IsValid(compressionType))
                throw new ArgumentException("Must be either 'deflate' or 'gzip'", compressionType);

            this.StatusCode = HttpStatusCode.OK;
            this.ContentType = contentMimeType;

            this.Contents = contents;
            this.Headers = new Dictionary<string, string> {
                { HttpHeaders.ContentEncoding, compressionType },
            };
            this.Cookies = new List<Cookie>();
        }

        public void WriteTo(Stream responseStream)
        {
            var response = RequestContext?.Response;
            response?.SetContentLength(this.Contents.Length + PaddingLength);

            responseStream.Write(this.Contents, 0, this.Contents.Length);
            //stream.Write(this.Contents, Adler32ChecksumLength, this.Contents.Length - Adler32ChecksumLength);

            responseStream.Flush();
        }

    }
}