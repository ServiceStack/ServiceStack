#if !(SL5 || XBOX || ANDROID || __IOS__ || __MAC__ || PCL)
using System.IO;
using System.IO.Compression;
using System.Text;
using ServiceStack.Caching;

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
            var utf8Bytes = GUnzipBytes(gzBuffer);
            return Encoding.UTF8.GetString(utf8Bytes, 0, utf8Bytes.Length);
        }

        public byte[] GUnzipBytes(byte[] gzBuffer)
        {
            using (var compressedStream = new MemoryStream(gzBuffer))
            using (var zipStream = new GZipStream(compressedStream, CompressionMode.Decompress))
            {
                return zipStream.ReadFully();
            }
        }

        public Stream GZipStream(Stream outputStream)
        {
            return new GZipStream(outputStream, CompressionMode.Compress);
        }
    }
}
#endif