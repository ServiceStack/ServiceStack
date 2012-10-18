using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using ServiceStack.Service;
using ServiceStack.ServiceHost;

namespace ServiceStack.Common.Web
{
    public class CompressedResult
        : IStreamWriter, IHttpResult
    {
        public const int Adler32ChecksumLength = 4;

        public const string DefaultContentType = MimeTypes.Xml;

        public byte[] Contents { get; private set; }

        public string ContentType { get; set; }

        public Dictionary<string, string> Headers { get; private set; }

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

        public IRequestContext RequestContext { get; set; }

        public IDictionary<string, string> Options
        {
            get { return this.Headers; }
        }

        public CompressedResult(byte[] contents)
            : this(contents, CompressionTypes.Deflate) { }

        public CompressedResult(byte[] contents, string compressionType)
            : this(contents, compressionType, DefaultContentType) { }

        public CompressedResult(byte[] contents, string compressionType, string contentMimeType)
        {
            if (!CompressionTypes.IsValid(compressionType))
            {
                throw new ArgumentException("Must be either 'deflate' or 'gzip'", compressionType);
            }

            this.StatusCode = HttpStatusCode.OK;
            this.ContentType = contentMimeType;

            this.Contents = contents;
            this.Headers = new Dictionary<string, string> {
                { HttpHeaders.ContentEncoding, compressionType },
            };
        }

        public void WriteTo(Stream responseStream)
        {
            responseStream.Write(this.Contents, 0, this.Contents.Length);
            //stream.Write(this.Contents, Adler32ChecksumLength, this.Contents.Length - Adler32ChecksumLength);
        }

    }
}