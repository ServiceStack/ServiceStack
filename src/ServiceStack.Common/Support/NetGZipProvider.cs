using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using ServiceStack.CacheAccess;

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
				ms.Position = 0;

				return ms.ToArray();
			}
		}

		public string GUnzip(byte[] gzBuffer)
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