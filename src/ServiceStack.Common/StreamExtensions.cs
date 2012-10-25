#if !SILVERLIGHT && !XBOX
using System;
using System.IO;
using System.Text;
using ServiceStack.CacheAccess;
using ServiceStack.Common.Support;
using ServiceStack.Common.Web;
using ServiceStack.Text;

namespace ServiceStack.Common
{
    public static class StreamExtensions
    {
#if !MONOTOUCH
        /// <summary>
        /// Compresses the specified text using the default compression method: Deflate
        /// </summary>
        /// <param name="text">The text.</param>
        /// <param name="compressionType">Type of the compression.</param>
        /// <returns></returns>
        public static byte[] Compress(this string text, string compressionType)
        {
            if (compressionType == CompressionTypes.Deflate)
                return Deflate(text);

            if (compressionType == CompressionTypes.GZip)
                return GZip(text);

            throw new NotSupportedException(compressionType);
        }

        public static IDeflateProvider DeflateProvider = new NetDeflateProvider();

        public static IGZipProvider GZipProvider = new NetGZipProvider();

        /// <summary>
        /// Decompresses the specified gz buffer using the default compression method: Inflate
        /// </summary>
        /// <param name="gzBuffer">The gz buffer.</param>
        /// <param name="compressionType">Type of the compression.</param>
        /// <returns></returns>
        public static string Decompress(this byte[] gzBuffer, string compressionType)
        {
            if (compressionType == CompressionTypes.Deflate)
                return Inflate(gzBuffer);

            if (compressionType == CompressionTypes.GZip)
                return GUnzip(gzBuffer);

            throw new NotSupportedException(compressionType);
        }

        public static byte[] Deflate(this string text)
        {
            return DeflateProvider.Deflate(text);
        }

        public static string Inflate(this byte[] gzBuffer)
        {
            return DeflateProvider.Inflate(gzBuffer);
        }

        public static byte[] GZip(this string text)
        {
            return GZipProvider.GZip(text);
        }

        public static string GUnzip(this byte[] gzBuffer)
        {
            return GZipProvider.GUnzip(gzBuffer);
        }
#endif

        public static string ToUtf8String(this Stream stream)
        {
            if (stream == null) throw new ArgumentNullException("stream");

            using (var reader = new StreamReader(stream, Encoding.UTF8))
            {
                return reader.ReadToEnd();
            }
        }

        public static byte[] ToBytes(this Stream stream)
        {
            if (stream == null) throw new ArgumentNullException("stream");

            return stream.ReadFully();
        }

        public static void Write(this Stream stream, string text)
        {
            var bytes = Encoding.ASCII.GetBytes(text);
            stream.Write(bytes, 0, bytes.Length);
        }

    }

}
#endif
