using System;
using System.Collections.Generic;
using System.IO;
using ServiceStack.Configuration;
using ServiceStack.Service;

namespace ServiceStack.WebHost.Endpoints.Results
{
	public class CompressedResult
		: IStreamWriter, IHasOptions
	{
		public const int Adler32ChecksumLength = 4;

		public const string DefaultContentType = MimeTypes.Xml;

		public byte[] Contents { get; private set; }

		public Dictionary<string, string> Headers { get; private set; }

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

			this.Contents = contents;
			this.Headers = new Dictionary<string, string> {
           		{ HttpHeaders.ContentType, contentMimeType },
				{ HttpHeaders.ContentEncoding, compressionType },
           	};
		}

		public void WriteTo(Stream stream)
		{
			stream.Write(this.Contents, Adler32ChecksumLength, this.Contents.Length - Adler32ChecksumLength);
			stream.Flush();
		}

	}
}