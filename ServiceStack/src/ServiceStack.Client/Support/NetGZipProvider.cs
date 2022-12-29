using System;
using System.IO;
using ServiceStack.Caching;

namespace ServiceStack.Support;

[Obsolete("Use GZipProvider")]
public class NetGZipProvider : IGZipProvider
{
    public static NetGZipProvider Instance { get; } = new();

    public byte[] GZip(string text) => GZipCompressor.Instance.Compress(text);

    public byte[] GZip(byte[] bytes) => GZipCompressor.Instance.Compress(bytes);

    public Stream GZipStream(Stream outputStream) => GZipCompressor.Instance.Compress(outputStream);

    public string GUnzip(byte[] gzBuffer) => GZipCompressor.Instance.Decompress(gzBuffer);

    public byte[] GUnzipBytes(byte[] gzBuffer) => GZipCompressor.Instance.DecompressBytes(gzBuffer);

    public Stream GUnzipStream(Stream inputStream) => GZipCompressor.Instance.Decompress(inputStream);
}