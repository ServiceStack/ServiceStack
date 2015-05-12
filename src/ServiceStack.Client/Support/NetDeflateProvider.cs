#if !(SL5 || XBOX || ANDROID || __IOS__ || PCL)
using System.IO;
using System.IO.Compression;
using System.Text;
using ServiceStack.Caching;
using ServiceStack.Text;

namespace ServiceStack.Support
{
    public class NetDeflateProvider : IDeflateProvider
    {
        public byte[] Deflate(string text)
        {
            var buffer = Encoding.UTF8.GetBytes(text);
            // In .NET FX incompat-ville, you can't access compressed bytes without closing DeflateStream
            // Which means we must use MemoryStream since you have to use ToArray() on a closed Stream
            using (var ms = new MemoryStream())
            using (var zipStream = new DeflateStream(ms, CompressionMode.Compress))
            {
                zipStream.Write(buffer, 0, buffer.Length);
                zipStream.Close();

                return ms.ToArray();
            }
        }

        public string Inflate(byte[] gzBuffer)
        {
            using (var compressedStream = new MemoryStream(gzBuffer))
            using (var zipStream = new DeflateStream(compressedStream, CompressionMode.Decompress))
            {
                var utf8Bytes = zipStream.ReadFully();
                return Encoding.UTF8.GetString(utf8Bytes, 0, utf8Bytes.Length);
            }
        }

    }
}
#endif