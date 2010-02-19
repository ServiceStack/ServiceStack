using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using ServiceStack.CacheAccess;
using ServiceStack.Common.Support;
using ServiceStack.Common.Web;

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

		public static IEnumerable<string> ReadLines(this StreamReader reader)
		{
			if (reader == null)
				throw new ArgumentNullException("reader");

			string line;
			while ((line = reader.ReadLine()) != null)
			{
				yield return line;
			}
		}

		/// <summary>
		/// @jonskeet: Collection of utility methods which operate on streams.
		/// r285, February 26th 2009: http://www.yoda.arachsys.com/csharp/miscutil/
		/// </summary>
		const int DefaultBufferSize = 8 * 1024;

		/// <summary>
		/// Reads the given stream up to the end, returning the data as a byte
		/// array.
		/// </summary>
		public static byte[] ReadFully(this Stream input)
		{
			return ReadFully(input, DefaultBufferSize);
		}

		/// <summary>
		/// Reads the given stream up to the end, returning the data as a byte
		/// array, using the given buffer size.
		/// </summary>
		public static byte[] ReadFully(this Stream input, int bufferSize)
		{
			if (bufferSize < 1)
			{
				throw new ArgumentOutOfRangeException("bufferSize");
			}
			return ReadFully(input, new byte[bufferSize]);
		}

		/// <summary>
		/// Reads the given stream up to the end, returning the data as a byte
		/// array, using the given buffer for transferring data. Note that the
		/// current contents of the buffer is ignored, so the buffer needn't
		/// be cleared beforehand.
		/// </summary>
		public static byte[] ReadFully(this Stream input, byte[] buffer)
		{
			if (buffer == null)
			{
				throw new ArgumentNullException("buffer");
			}
			if (input == null)
			{
				throw new ArgumentNullException("input");
			}
			if (buffer.Length == 0)
			{
				throw new ArgumentException("Buffer has length of 0");
			}
			// We could do all our own work here, but using MemoryStream is easier
			// and likely to be just as efficient.
			using (var tempStream = new MemoryStream())
			{
				Copy(input, tempStream, buffer);
				// No need to copy the buffer if it's the right size
				if (tempStream.Length == tempStream.GetBuffer().Length)
				{
					return tempStream.GetBuffer();
				}
				// Okay, make a copy that's the right size
				return tempStream.ToArray();
			}
		}

		/// <summary>
		/// Copies all the data from one stream into another.
		/// </summary>
		public static void Copy(this Stream input, Stream output)
		{
			Copy(input, output, DefaultBufferSize);
		}

		/// <summary>
		/// Copies all the data from one stream into another, using a buffer
		/// of the given size.
		/// </summary>
		public static void Copy(this Stream input, Stream output, int bufferSize)
		{
			if (bufferSize < 1)
			{
				throw new ArgumentOutOfRangeException("bufferSize");
			}
			Copy(input, output, new byte[bufferSize]);
		}

		/// <summary>
		/// Copies all the data from one stream into another, using the given 
		/// buffer for transferring data. Note that the current contents of 
		/// the buffer is ignored, so the buffer needn't be cleared beforehand.
		/// </summary>
		public static void Copy(this Stream input, Stream output, byte[] buffer)
		{
			if (buffer == null)
			{
				throw new ArgumentNullException("buffer");
			}
			if (input == null)
			{
				throw new ArgumentNullException("input");
			}
			if (output == null)
			{
				throw new ArgumentNullException("output");
			}
			if (buffer.Length == 0)
			{
				throw new ArgumentException("Buffer has length of 0");
			}
			int read;
			while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
			{
				output.Write(buffer, 0, read);
			}
		}

		/// <summary>
		/// Reads exactly the given number of bytes from the specified stream.
		/// If the end of the stream is reached before the specified amount
		/// of data is read, an exception is thrown.
		/// </summary>
		public static byte[] ReadExactly(this Stream input, int bytesToRead)
		{
			return ReadExactly(input, new byte[bytesToRead]);
		}

		/// <summary>
		/// Reads into a buffer, filling it completely.
		/// </summary>
		public static byte[] ReadExactly(this Stream input, byte[] buffer)
		{
			return ReadExactly(input, buffer, buffer.Length);
		}

		/// <summary>
		/// Reads exactly the given number of bytes from the specified stream,
		/// into the given buffer, starting at position 0 of the array.
		/// </summary>
		public static byte[] ReadExactly(this Stream input, byte[] buffer, int bytesToRead)
		{
			return ReadExactly(input, buffer, 0, bytesToRead);
		}

		/// <summary>
		/// Reads exactly the given number of bytes from the specified stream,
		/// into the given buffer, starting at position 0 of the array.
		/// </summary>
		public static byte[] ReadExactly(this Stream input, byte[] buffer, int startIndex, int bytesToRead)
		{
			if (input == null)
			{
				throw new ArgumentNullException("input");
			}

			if (buffer == null)
			{
				throw new ArgumentNullException("buffer");
			}

			if (startIndex < 0 || startIndex >= buffer.Length)
			{
				throw new ArgumentOutOfRangeException("startIndex");
			}

			if (bytesToRead < 1 || startIndex + bytesToRead > buffer.Length)
			{
				throw new ArgumentOutOfRangeException("bytesToRead");
			}

			return ReadExactlyFast(input, buffer, startIndex, bytesToRead);
		}

		/// <summary>
		/// Same as ReadExactly, but without the argument checks.
		/// </summary>
		private static byte[] ReadExactlyFast(Stream fromStream, byte[] intoBuffer, int startAtIndex, int bytesToRead)
		{
			var index = 0;
			while (index < bytesToRead)
			{
				var read = fromStream.Read(intoBuffer, startAtIndex + index, bytesToRead - index);
				if (read == 0)
				{
					throw new EndOfStreamException
						(String.Format("End of stream reached with {0} byte{1} left to read.",
									   bytesToRead - index,
									   bytesToRead - index == 1 ? "s" : ""));
				}
				index += read;
			}
			return intoBuffer;
		}

	}

}