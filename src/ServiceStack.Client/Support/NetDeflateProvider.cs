using System.IO;
using System.IO.Compression;
using System.Text;
using ServiceStack.Caching;
using ServiceStack.Text;

namespace ServiceStack.Support
{
    public class NetDeflateProvider : IDeflateProvider
    {
        public static NetDeflateProvider Instance { get; } = new();
        
        public byte[] Deflate(string text)
        {
            return Deflate(Encoding.UTF8.GetBytes(text));
        }

        public byte[] Deflate(byte[] bytes)
        {
            // In .NET FX incompatible, you can't access compressed bytes without closing DeflateStream
            // Which means we must use MemoryStream since you have to use ToArray() on a closed Stream
            using var ms = new MemoryStream();
            using var zipStream = new DeflateStream(ms, CompressionMode.Compress);
            zipStream.Write(bytes, 0, bytes.Length);
            zipStream.Close();

            return ms.ToArray();
        }

        public string Inflate(byte[] gzBuffer)
        {
            using var uncompressedStream = MemoryStreamFactory.GetStream();
            using var compressedStream = MemoryStreamFactory.GetStream(gzBuffer);
            using var zipStream = new DeflateStream(compressedStream, CompressionMode.Decompress);
            zipStream.CopyTo(uncompressedStream);
            return uncompressedStream.ReadToEnd();
        }

        public byte[] InflateBytes(byte[] gzBuffer)
        {
            using var compressedStream = gzBuffer.InMemoryStream();
            using var zipStream = new DeflateStream(compressedStream, CompressionMode.Decompress);
            return zipStream.ReadFully();
        }

        public Stream DeflateStream(Stream outputStream)
        {
            return new DeflateStream(outputStream, CompressionMode.Compress);
        }

        public Stream InflateStream(Stream inputStream)
        {
            return new DeflateStream(inputStream, CompressionMode.Decompress);
        }
    }
}
