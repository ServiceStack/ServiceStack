using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ServiceStack.Web;

#if NETFX_CORE
using System.Net.Http.Headers;
#endif

namespace ServiceStack
{
    public class CompressedFileResult
        : IStreamWriterAsync, IHasOptions
    {
        public const int Adler32ChecksumLength = 4;

        public const string DefaultContentType = MimeTypes.Xml;

        public string FilePath { get; private set; }

        public Dictionary<string, string> Headers { get; private set; }

        public IDictionary<string, string> Options => this.Headers;

        public CompressedFileResult(string filePath)
            : this(filePath, CompressionTypes.Deflate)
        { }

        public CompressedFileResult(string filePath, string compressionType)
            : this(filePath, compressionType, DefaultContentType)
        { }

        public CompressedFileResult(string filePath, string compressionType, string contentMimeType)
        {
            if (!CompressionTypes.IsValid(compressionType))
                throw new ArgumentException("Must be either 'deflate' or 'gzip'", compressionType);

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
        public async Task WriteToAsync(Stream responseStream, CancellationToken token = new CancellationToken())
        {
            using (var fs = new FileStream(this.FilePath, FileMode.Open, FileAccess.Read))
            {
                fs.Position = Adler32ChecksumLength;

                await fs.CopyToAsync(responseStream, token);
                await responseStream.FlushAsync(token);
            }
        }
#endif

    }
}