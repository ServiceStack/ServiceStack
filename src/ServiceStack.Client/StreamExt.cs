// Copyright (c) ServiceStack, Inc. All Rights Reserved.
// License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt

using System;
using System.IO;
using System.Text;
using ServiceStack.Caching;
using ServiceStack.Text;
using System.Security.Cryptography;

namespace ServiceStack
{
    public static class StreamExt
    {
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

        public static Stream CompressStream(this Stream stream, string compressionType)
        {
            if (compressionType == CompressionTypes.Deflate)
                return DeflateProvider.DeflateStream(stream);

            if (compressionType == CompressionTypes.GZip)
                return GZipProvider.GZipStream(stream);

            throw new NotSupportedException(compressionType);
        }

        /// <summary>
        /// Compresses the specified text using the default compression method: Deflate
        /// </summary>
        public static byte[] CompressBytes(this byte[] bytes, string compressionType)
        {
            if (compressionType == CompressionTypes.Deflate)
                return DeflateProvider.Deflate(bytes);

            if (compressionType == CompressionTypes.GZip)
                return GZipProvider.GZip(bytes);

            throw new NotSupportedException(compressionType);
        }

        public static IDeflateProvider DeflateProvider = new Support.NetDeflateProvider();

        public static IGZipProvider GZipProvider = new Support.NetGZipProvider();

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

        /// <summary>
        /// Decompresses the specified gz buffer using inflate or gzip method
        /// </summary>
        /// <param name="gzStream">Compressed stream</param>
        /// <param name="compressionType">Type of the compression. Can be "gzip" or "deflate"</param>
        /// <returns>Decompressed stream</returns>
        public static Stream Decompress(this Stream gzStream, string compressionType)
        {
            if (String.IsNullOrEmpty(compressionType))
                return gzStream;

            if (compressionType == CompressionTypes.Deflate)
                return DeflateProvider.InflateStream(gzStream);

            if (compressionType == CompressionTypes.GZip)
                return GZipProvider.GUnzipStream(gzStream);

            throw new NotSupportedException(compressionType);
        }

        /// <summary>
        /// Decompresses the specified gz buffer using the default compression method: Inflate
        /// </summary>
        public static byte[] DecompressBytes(this byte[] gzBuffer, string compressionType)
        {
            if (compressionType == CompressionTypes.Deflate)
                return DeflateProvider.InflateBytes(gzBuffer);

            if (compressionType == CompressionTypes.GZip)
                return GZipProvider.GUnzipBytes(gzBuffer);

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

        public static string ToUtf8String(this Stream stream)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            using (var reader = new StreamReader(stream, Encoding.UTF8))
            {
                return reader.ReadToEnd();
            }
        }

        public static byte[] ToBytes(this Stream stream)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            return stream.ReadFully();
        }

        public static void Write(this Stream stream, string text)
        {
            var bytes = Encoding.UTF8.GetBytes(text);
            stream.Write(bytes, 0, bytes.Length);
        }
    }
}
