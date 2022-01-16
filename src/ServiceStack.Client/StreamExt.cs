#nullable enable
// Copyright (c) ServiceStack, Inc. All Rights Reserved.
// License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt

using System;
using System.IO;
using System.Text;

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
        public static byte[] Compress(this string text, string compressionType, Encoding? encoding=null) =>
            StreamCompressors.GetRequired(compressionType).Compress(text, encoding);

        public static Stream CompressStream(this Stream stream, string compressionType) =>
            StreamCompressors.GetRequired(compressionType).Compress(stream);

        /// <summary>
        /// Compresses the specified text using the default compression method: Deflate
        /// </summary>
        public static byte[] CompressBytes(this byte[] bytes, string compressionType) =>
            StreamCompressors.GetRequired(compressionType).Compress(bytes);

        /// <summary>
        /// Decompresses the specified gz buffer using the default compression method: Inflate
        /// </summary>
        /// <param name="gzBuffer">The gz buffer.</param>
        /// <param name="compressionType">Type of the compression.</param>
        /// <returns></returns>
        public static string Decompress(this byte[] gzBuffer, string compressionType) =>
            StreamCompressors.GetRequired(compressionType).Decompress(gzBuffer);

        /// <summary>
        /// Decompresses the specified gz buffer using inflate or gzip method
        /// </summary>
        /// <param name="gzStream">Compressed stream</param>
        /// <param name="compressionType">Type of the compression. Can be "gzip" or "deflate"</param>
        /// <returns>Decompressed stream</returns>
        public static Stream Decompress(this Stream gzStream, string compressionType) =>
            !string.IsNullOrEmpty(compressionType) 
                ? StreamCompressors.GetRequired(compressionType).Decompress(gzStream) 
                : gzStream;

        /// <summary>
        /// Decompresses the specified gz buffer using the default compression method: Inflate
        /// </summary>
        public static byte[] DecompressBytes(this byte[] gzBuffer, string compressionType) =>
            StreamCompressors.GetRequired(compressionType).DecompressBytes(gzBuffer);

        public static byte[] Deflate(this string text) => DeflateCompressor.Instance.Compress(text);

        public static string Inflate(this byte[] gzBuffer) => DeflateCompressor.Instance.Decompress(gzBuffer);

        public static byte[] GZip(this string text) => GZipCompressor.Instance.Compress(text);

        public static string GUnzip(this byte[] gzBuffer) => GZipCompressor.Instance.Decompress(gzBuffer);

        public static string ToUtf8String(this Stream stream)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            return stream.ReadToEnd();
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
