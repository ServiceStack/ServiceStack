using System.IO;
using System.IO.Compression;
using System.Text;
using ServiceStack.Caching;
using ServiceStack.Text;

namespace ServiceStack.Support
{
    public class NetGZipProvider : IGZipProvider
    {
        public byte[] GZip(string text)
        {
            return GZip(Encoding.UTF8.GetBytes(text));
        }

        public byte[] GZip(byte[] buffer)
        {
            using (var ms = new MemoryStream())
            using (var zipStream = new GZipStream(ms, CompressionMode.Compress))
            {
                zipStream.Write(buffer, 0, buffer.Length);
                zipStream.Close();

                return ms.ToArray();
            }
        }

        public string GUnzip(byte[] gzBuffer)
        {
            using (var uncompressedStream = MemoryStreamFactory.GetStream())
            using (var compressedStream = MemoryStreamFactory.GetStream(gzBuffer))
            using (var zipStream = new GZipStream(compressedStream, CompressionMode.Decompress))
            {
                zipStream.CopyTo(uncompressedStream);
                return uncompressedStream.ReadToEnd();
            }
        }

        public byte[] GUnzipBytes(byte[] gzBuffer)
        {
            using (var compressedStream = gzBuffer.InMemoryStream())
            using (var zipStream = new GZipStream(compressedStream, CompressionMode.Decompress))
            {
                return zipStream.ReadFully();
            }
        }

        public Stream GZipStream(Stream outputStream)
        {
            return new GZipStream(outputStream, CompressionMode.Compress);
        }

        public Stream GUnzipStream(Stream gzStream)
        {
            return new GZipStream(gzStream, CompressionMode.Decompress);
        }
    }
}
