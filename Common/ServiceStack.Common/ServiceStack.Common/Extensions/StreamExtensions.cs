using System;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace ServiceStack.Common.Extensions
{
	public static class StreamExtensions
	{
		public static void WriteTo(this Stream inStream, Stream outStream)
		{
			var memoryStream = inStream as MemoryStream;
			if (memoryStream != null)
			{
				memoryStream.WriteTo(outStream);
				return;
			}

			var data = new byte[4096];
			int bytesRead;

			while ((bytesRead = inStream.Read(data, 0, data.Length)) > 0)
			{
				outStream.Write(data, 0, bytesRead);
			}
		}

		/// <summary>
		/// Compresses the specified text using the default compression method: Deflate
		/// </summary>
		/// <param name="text">The text.</param>
		/// <returns></returns>
		public static byte[] Compress(this string text)
		{
			return Deflate(text);
		}

		/// <summary>
		/// Decompresses the specified gz buffer using the default compression method: Inflate
		/// </summary>
		/// <param name="gzBuffer">The gz buffer.</param>
		/// <returns></returns>
		public static string Decompress(this byte[] gzBuffer)
		{
			return Inflate(gzBuffer);
		}

		public static byte[] Deflate(this string text)
		{
			var buffer = Encoding.UTF8.GetBytes(text);
			var ms = new MemoryStream();
			using (var zipStream = new DeflateStream(ms, CompressionMode.Compress, true))
			{
				zipStream.Write(buffer, 0, buffer.Length);
			}

			ms.Position = 0;

			var compressed = new byte[ms.Length];
			ms.Read(compressed, 0, compressed.Length);

			var gzBuffer = new byte[compressed.Length + 4];
			Buffer.BlockCopy(compressed, 0, gzBuffer, 4, compressed.Length);
			Buffer.BlockCopy(BitConverter.GetBytes(buffer.Length), 0, gzBuffer, 0, 4);

			return gzBuffer;
		}

		public static string Inflate(this byte[] gzBuffer)
		{
			using (var ms = new MemoryStream())
			{
				var msgLength = BitConverter.ToInt32(gzBuffer, 0);
				ms.Write(gzBuffer, 4, gzBuffer.Length - 4);

				var buffer = new byte[msgLength];

				ms.Position = 0;
				using (var zipStream = new DeflateStream(ms, CompressionMode.Decompress))
				{
					zipStream.Read(buffer, 0, buffer.Length);
				}

				return Encoding.UTF8.GetString(buffer);
			}
		}

		public static byte[] Gzip(this string text)
		{
			var buffer = Encoding.UTF8.GetBytes(text);
			var ms = new MemoryStream();
			using (var zipStream = new GZipStream(ms, CompressionMode.Compress, true))
			{
				zipStream.Write(buffer, 0, buffer.Length);
			}

			ms.Position = 0;

			var compressed = new byte[ms.Length];
			ms.Read(compressed, 0, compressed.Length);

			var gzBuffer = new byte[compressed.Length + 4];
			Buffer.BlockCopy(compressed, 0, gzBuffer, 4, compressed.Length);
			Buffer.BlockCopy(BitConverter.GetBytes(buffer.Length), 0, gzBuffer, 0, 4);

			return gzBuffer;
		}

		public static string Gunzip(this byte[] gzBuffer)
		{
			using (var ms = new MemoryStream())
			{
				var msgLength = BitConverter.ToInt32(gzBuffer, 0);
				ms.Write(gzBuffer, 4, gzBuffer.Length - 4);

				var buffer = new byte[msgLength];

				ms.Position = 0;
				using (var zipStream = new GZipStream(ms, CompressionMode.Decompress))
				{
					zipStream.Read(buffer, 0, buffer.Length);
				}

				return Encoding.UTF8.GetString(buffer);
			}
		}


	}

}