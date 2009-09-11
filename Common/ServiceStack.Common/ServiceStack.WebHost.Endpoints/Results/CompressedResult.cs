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

		public const string Deflate = "deflate";
		public const string GZip = "gzip";

		public const string ContentTypeXml = "text/xml";
		public const string ContentTypeJson = "text/json";
		public const string DefaultContentType = ContentTypeXml;

		public byte[] Contents { get; private set; }

		public Dictionary<string, string> HttpHeaders { get; private set; }

		public IDictionary<string, string> Options
		{
			get { return this.HttpHeaders; }
		}

		public CompressedResult(byte[] contents)
			: this(contents, Deflate) { }

		public CompressedResult(byte[] contents, string compressionType)
			: this(contents, compressionType, DefaultContentType) { }

		public CompressedResult(byte[] contents, string compressionType, string contentType)
		{
			if (compressionType != Deflate && compressionType != GZip)
			{
				throw new ArgumentException("Must be either 'deflate' or 'gzip'", compressionType);
			}

			this.Contents = contents;
			this.HttpHeaders = new Dictionary<string, string> {
           		{ "Content-Type", contentType },
				{ "Content-Encoding", compressionType },
           	};
		}

		public void WriteTo(Stream stream)
		{
			stream.Write(this.Contents, Adler32ChecksumLength, this.Contents.Length);
			stream.Flush();
		}

	}
}