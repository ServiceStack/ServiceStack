#if !SILVERLIGHT && !MONOTOUCH && !XBOX
using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using ServiceStack.CacheAccess;
using ServiceStack.Text;

namespace ServiceStack.Common.Support
{
    public class NetGZipProvider : IGZipProvider
    {
        public byte[] GZip(string text)
        {
            var buffer = Encoding.UTF8.GetBytes(text);
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
            using (var compressedStream = new MemoryStream(gzBuffer))
            using (var zipStream = new GZipStream(compressedStream, CompressionMode.Decompress))
            {
                var utf8Bytes = zipStream.ReadFully();
                return Encoding.UTF8.GetString(utf8Bytes);
            }
        }
    }
}
#endif