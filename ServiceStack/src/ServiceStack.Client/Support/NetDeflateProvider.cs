using System;
using System.IO;
using System.IO.Compression;
using ServiceStack.Caching;

namespace ServiceStack.Support;

[Obsolete("Use DeflateProvider")]
public class NetDeflateProvider : IDeflateProvider
{
    public static NetDeflateProvider Instance { get; } = new();

    public byte[] Deflate(string text) => DeflateCompressor.Instance.Compress(text);
    public byte[] Deflate(byte[] bytes) => DeflateCompressor.Instance.Compress(bytes);
    public Stream DeflateStream(Stream outputStream) => DeflateCompressor.Instance.Compress(outputStream);
    public string Inflate(byte[] gzBuffer) => DeflateCompressor.Instance.Decompress(gzBuffer);
    public byte[] InflateBytes(byte[] gzBuffer) => DeflateCompressor.Instance.DecompressBytes(gzBuffer);
    public Stream InflateStream(Stream inputStream) => new DeflateStream(inputStream, CompressionMode.Decompress);
}