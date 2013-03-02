using System;
using System.Collections.Generic;
using System.IO;
using ServiceStack.Common.Extensions;
using ServiceStack.Configuration;
using ServiceStack.Service;
using ServiceStack.ServiceHost;
using ServiceStack.Text;
#if NETFX_CORE
using System.Net.Http.Headers;
#endif

namespace ServiceStack.Common.Web
{
    public class CompressedFileResult
        : IStreamWriter, IHasOptions
    {
        public const int Adler32ChecksumLength = 4;

        public const string DefaultContentType = MimeTypes.Xml;

        public string FilePath { get; private set; }

        public Dictionary<string, string> Headers { get; private set; }

        public IDictionary<string, string> Options
        {
            get { return this.Headers; }
        }

        public CompressedFileResult(string filePath)
            : this(filePath, CompressionTypes.Deflate) { }

        public CompressedFileResult(string filePath, string compressionType)
            : this(filePath, compressionType, DefaultContentType) { }

        public CompressedFileResult(string filePath, string compressionType, string contentMimeType)
        {
            if (!CompressionTypes.IsValid(compressionType))
            {
                throw new ArgumentException("Must be either 'deflate' or 'gzip'", compressionType);
            }

            this.FilePath = filePath;
            this.Headers = new Dictionary<string, string> {
                { HttpHeaders.ContentType, contentMimeType },
                { HttpHeaders.ContentEncoding, compressionType },
            };
        }

#if NETFX_CORE
        public async void WriteTo(Stream responseStream)
        {
            var file = await Windows.Storage.StorageFile.GetFileFromPathAsync(this.FilePath);
            using (var fs = await file.OpenStreamForWriteAsync())
            {
                fs.Position = Adler32ChecksumLength;

                fs.WriteTo(responseStream);
                responseStream.Flush();
            }
        }
#else
        public void WriteTo(Stream responseStream)
        {
            using (var fs = new FileStream(this.FilePath, FileMode.Open, FileAccess.Read))
            {
                fs.Position = Adler32ChecksumLength;

                fs.WriteTo(responseStream);
                responseStream.Flush();
            }
        }
#endif

    }
}